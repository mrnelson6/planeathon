using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using PlaneARViewer.BottomSheet;
using PlaneARViewer.Calibration;
using SharedAirplaneFinder;
using System;
using System.Drawing;
using System.Linq;
using System.Timers;
using UIKit;

namespace PlaneARViewer
{
    public partial class ViewController : UIViewController
    {
        private SharedAirplaneFinder.AirplaneFinder _airplaneFinder;

        // UI objects.
        private ARSceneView _arView;
        private UILabel _helpLabel;
        private UIToolbar _flyoverToolbar;
        private UIButton _CalibrateButton;

        // Location data source for AR and route tracking.
        private AdjustableLocationDataSource _locationSource = new AdjustableLocationDataSource();

        // Elevation source for AR scene calibration.
        private ArcGISTiledElevationSource _elevationSource;
        private Surface _elevationSurface;

        private PanCompassCalibrationGestureRecognizer _panCalibrator;

        // Overlay for testing plane graphics.
        private GraphicsOverlay _graphicsOverlay;

        // Items on ground, viewable from plane.
        private GraphicsOverlay _groundPointsOverlay;
        private FeatureLayer _airportsLayer1;
        private FeatureLayer _airportsLayer2;
        private FeatureLayer _airportsLayer3;

        // Using the view from an aircraft.
        private bool _fromPlaneView;
        private Camera _groundCamera;

        private bool _IsCalibrating = true;

        // Timer control enables stopping and starting frame-by-frame animation.
        private Timer _animationTimer;
        private FlightInfoViewController _flightInfoVC;
        private NSLayoutConstraint[] _flightInfoVC_HorizontalConstraints;
        private NSLayoutConstraint[] _flightInfoVC_VerticalConstraints;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public ViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Initialize();
        }

        /*
         * This method is used for all of the main setup code for the app.
         * Load feature layers.
         * Set users initial position.
         * ETC...
         */

        private async void Initialize()
        {
            try
            {
                // Create and add the scene.
                _arView.Scene = new Scene(Basemap.CreateImagery());

                // Add the location data source to the AR view.
                _arView.LocationDataSource = _locationSource;
                _locationSource.HeadingChanged += _locationSource_HeadingChanged;
                await _locationSource.StartAsync();

                // Create and add the elevation source.
                _elevationSource = new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer"));
                await _elevationSource.LoadAsync();
                _elevationSurface = new Surface();
                _elevationSurface.ElevationSources.Add(_elevationSource);
                _arView.Scene.BaseSurface = _elevationSurface;

                // Configure the surface for AR: no navigation constraint and hidden by default.
                _elevationSurface.NavigationConstraint = NavigationConstraint.None;
                _elevationSurface.Opacity = 0;

                // Configure scene view display for real-scale AR: no space effect or atmosphere effect.
                _arView.SpaceEffect = SpaceEffect.None;
                _arView.AtmosphereEffect = AtmosphereEffect.None;

                _arView.Scene.BaseSurface.Opacity = 0.5;
                _arView.Scene.BaseSurface.NavigationConstraint = NavigationConstraint.StayAbove;
                _graphicsOverlay = new GraphicsOverlay();
                GraphicsOverlay _identifyOverlay = new GraphicsOverlay();
                
                _arView.GraphicsOverlays.Add(_graphicsOverlay);
                _airplaneFinder = new SharedAirplaneFinder.AirplaneFinder(_graphicsOverlay, _identifyOverlay);
                _airplaneFinder.center = _locationSource.LastLocation.Position;
                _airplaneFinder.setupScene();
                _flightInfoVC.AssociateAirplaneFinder(_airplaneFinder);
                // Disable scene interaction.
                _arView.InteractionOptions = new SceneViewInteractionOptions() { IsEnabled = false };

                // Get the elevation value.
                _arView.LocationDataSource.LocationChanged += UpdateElevation;

                _panCalibrator = new PanCompassCalibrationGestureRecognizer(_locationSource);
                View.GestureRecognizers = new[] { _panCalibrator };

                _groundPointsOverlay = new GraphicsOverlay();
                _groundPointsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.DrapedBillboarded;
                _groundPointsOverlay.IsVisible = false;
                _arView.GraphicsOverlays.Add(_groundPointsOverlay);

                _airportsLayer1 = new FeatureLayer(new Uri("https://services.arcgis.com/P3ePLMYs2RVChkJx/arcgis/rest/services/USA_Airports_by_scale/FeatureServer/1"));
                _arView.Scene.OperationalLayers.Add(_airportsLayer1);
                _airportsLayer1.Renderer = new SimpleRenderer()
                {
                    Symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.Orange, 25)
                };
                _airportsLayer1.IsVisible = false;

                _airportsLayer2 = new FeatureLayer(new Uri("https://services.arcgis.com/P3ePLMYs2RVChkJx/arcgis/rest/services/USA_Airports_by_scale/FeatureServer/1"));
                _arView.Scene.OperationalLayers.Add(_airportsLayer2);
                _airportsLayer2.Renderer = new SimpleRenderer()
                {
                    Symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.Orange, 25)
                };
                _airportsLayer2.IsVisible = false;

                _airportsLayer3 = new FeatureLayer(new Uri("https://services.arcgis.com/P3ePLMYs2RVChkJx/arcgis/rest/services/USA_Airports_by_scale/FeatureServer/1"));
                _arView.Scene.OperationalLayers.Add(_airportsLayer3);
                _airportsLayer3.Renderer = new SimpleRenderer()
                {
                    Symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.Orange, 25)
                };
                _airportsLayer3.IsVisible = false;



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void _locationSource_HeadingChanged(object sender, double e)
        {
            // Get the old camera.
            Camera oldCamera = _arView.OriginCamera;

            // Set the origin camera by rotating the existing camera to the new heading.
            _arView.OriginCamera = oldCamera.RotateTo(e, oldCamera.Pitch, oldCamera.Roll);
        }

        private async void UpdateElevation(object sender, Location e)
        {
            try
            {
                await _elevationSurface.LoadAsync();
                double elevation = await _elevationSurface.GetElevationAsync(e.Position);
                _locationSource.SetKnownElevation(elevation + 5.0);
                _locationSource.LocationChanged -= UpdateElevation;
                _locationSource.IgnoreLocationUpdate = true;

                // Create ground symbol for user location.
                SimpleMarkerSymbol userLocationSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, Color.Green, 25);
                TextSymbol drapedBillboardedText = new TextSymbol("Your Location", Color.FromArgb(255, 255, 255, 255), 10,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Middle);
                drapedBillboardedText.OffsetY += 20;
                _groundPointsOverlay.Graphics.Add(new Graphic(e.Position, drapedBillboardedText));
                _groundPointsOverlay.Graphics.Add(new Graphic(e.Position, userLocationSymbol));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override void LoadView()
        {
            View = new UIView { BackgroundColor = UIColor.White };

            _flyoverToolbar = new UIToolbar();
            _flyoverToolbar.TranslatesAutoresizingMaskIntoConstraints = false;
            _flyoverToolbar.Hidden = true;

            _arView = new ARSceneView();
            _arView.TranslatesAutoresizingMaskIntoConstraints = false;

            _CalibrateButton = new UIButton();
            _CalibrateButton.TranslatesAutoresizingMaskIntoConstraints = false;
            _CalibrateButton.SetImage(UIImage.GetSystemImage("globe").ApplyTintColor(UIColor.White), UIControlState.Normal);
            _CalibrateButton.BackgroundColor = UIColor.SecondarySystemBackgroundColor;
            _CalibrateButton.Layer.CornerRadius = 8;
            _CalibrateButton.Layer.Opacity = 0.6f;

            //_helpLabel = new UILabel();
            //_helpLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            //_helpLabel.TextAlignment = UITextAlignment.Center;
            //_helpLabel.TextColor = UIColor.White;
            //_helpLabel.BackgroundColor = UIColor.FromWhiteAlpha(0, 0.6f);
            //_helpLabel.Text = "Plane Gang 2020";
            var backButton = new UIBarButtonItem() { Title = "Back to ground" };
            backButton.Clicked += (s, e) => DisableFromPlaneView();
            _flyoverToolbar.Items = new[]
            {
                backButton
            };

            _flightInfoVC = new FlightInfoViewController();

            AddChildViewController(_flightInfoVC);
            _flightInfoVC.View.TranslatesAutoresizingMaskIntoConstraints = false;

            View.AddSubviews(_arView, _flyoverToolbar, _CalibrateButton);//, toolbar);//, _helpLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _arView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _arView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _arView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _arView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                _flyoverToolbar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _flyoverToolbar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _flyoverToolbar.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor),
                _CalibrateButton.HeightAnchor.ConstraintEqualTo(48),
                _CalibrateButton.WidthAnchor.ConstraintEqualTo(48),
                _CalibrateButton.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 16),
                _CalibrateButton.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -16)
                //_helpLabel.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                //_helpLabel.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                //_helpLabel.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                //_helpLabel.HeightAnchor.ConstraintEqualTo(40)
            });

            _flightInfoVC_HorizontalConstraints = new NSLayoutConstraint[]
            {
                _flightInfoVC.View.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor),
                _flightInfoVC.View.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 16),
                _flightInfoVC.View.BottomAnchor.ConstraintEqualTo(_arView.SafeAreaLayoutGuide.BottomAnchor, -16),
                _flightInfoVC.View.WidthAnchor.ConstraintEqualTo(320)
            };

            _flightInfoVC_VerticalConstraints = new NSLayoutConstraint[]
            {
                _flightInfoVC.View.BottomAnchor.ConstraintEqualTo(_arView.SafeAreaLayoutGuide.BottomAnchor, -16),
                _flightInfoVC.View.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _flightInfoVC.View.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _flightInfoVC.View.HeightAnchor.ConstraintEqualTo(_flightInfoVC.GetViewHeight())
            };
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (NavigationController != null)
            {
                NavigationController.NavigationBarHidden = true;
            }
            // Start tracking as soon as the view has been shown.
            await _arView.StartTrackingAsync(ARLocationTrackingMode.Continuous);

            _arView.GeoViewTapped += _arView_GeoViewTapped;
            _flightInfoVC.mapButton.TouchUpInside += ShowMapView;
            _flightInfoVC.flyoverButton.TouchUpInside += ShowFlyoverView;
            _CalibrateButton.TouchUpInside += _CalibrateButton_TouchUpInside;
        }

        private void _CalibrateButton_TouchUpInside(object sender, EventArgs e)
        {
            _IsCalibrating = !_IsCalibrating;

            if (_IsCalibrating)
            {
                _arView.Scene.BaseSurface.Opacity = 0.5;
            }
            else
            {
                _arView.Scene.BaseSurface.Opacity = 0;
            }
        }

        private void ShowFlyoverView(object sender, EventArgs e)
        {
            EnableFromPlaneView();
        }

        private void ShowMapView(object sender, EventArgs e)
        {
            NavigationController.PushViewController(new PlanesMapView(_arView.OriginCamera.Location), true);
        }

        private async void _arView_GeoViewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            _airplaneFinder.ShouldUpdateIdentifyOverlay = false;
            _arView.GraphicsOverlays.Add(_airplaneFinder._identifyOverlay);
            var res = await _arView.IdentifyGraphicsOverlayAsync(_airplaneFinder._identifyOverlay, e.Position, 64, false, 1);
            _arView.GraphicsOverlays.Remove(_airplaneFinder._identifyOverlay);
            _airplaneFinder.ShouldUpdateIdentifyOverlay = true;
            if (res.Graphics.Any())
            {
                Console.WriteLine(res.Graphics.First());

                string callsign = res.Graphics.First().Attributes["CALLSIGN"] as string;

                if (_airplaneFinder.planes.ContainsKey(callsign))
                {
                    Plane targetPlane = _airplaneFinder.planes[callsign];
                    _airplaneFinder.SelectedPlane = targetPlane;

                    View.AddSubview(_flightInfoVC.View);

                    if (TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Regular)
                    {
                        NSLayoutConstraint.ActivateConstraints(_flightInfoVC_VerticalConstraints);
                    }
                    else
                    {
                        NSLayoutConstraint.ActivateConstraints(_flightInfoVC_HorizontalConstraints);
                    }
                }
            }
            else
            {
                NSLayoutConstraint.DeactivateConstraints(_flightInfoVC_HorizontalConstraints);
                NSLayoutConstraint.DeactivateConstraints(_flightInfoVC_VerticalConstraints);
                _flightInfoVC.View.RemoveFromSuperview();
            }
        }

        private async void EnableFromPlaneView()
        {
            _CalibrateButton.Hidden = true;
            NSLayoutConstraint.DeactivateConstraints(_flightInfoVC_HorizontalConstraints);
            NSLayoutConstraint.DeactivateConstraints(_flightInfoVC_VerticalConstraints);
            _flightInfoVC.View.RemoveFromSuperview();

            _flyoverToolbar.Hidden = false;

            Graphic selectedPlane = _flightInfoVC.GetPlane().graphic;
            string callSign = _flightInfoVC.GetPlane().callsign;

            _arView.GeoViewTapped -= _arView_GeoViewTapped;

            // Store the ground camera position.
            _groundCamera = _arView.OriginCamera;

            // switch camera to the position of the plane.
            await _arView.StopTrackingAsync();
            _arView.OriginCamera = new Camera(selectedPlane.Geometry.Extent.GetCenter(), _groundCamera.Heading, _groundCamera.Pitch, _groundCamera.Roll);
            await _arView.StartTrackingAsync();

            // Configure scene view display for real-scale AR: no space effect or atmosphere effect.
            _arView.SpaceEffect = SpaceEffect.Stars;
            _arView.AtmosphereEffect = AtmosphereEffect.Realistic;
            _arView.Scene.BaseSurface.Opacity = 1.0;

            // Disable subsurface
            _arView.Scene.BaseSurface.NavigationConstraint = NavigationConstraint.StayAbove;
            _arView.Scene.BaseSurface.Opacity = 1;

            // Disable plane.
            selectedPlane.IsVisible = false;

            // Start animation timer.
            _animationTimer = new Timer(33)
            {
                AutoReset = true
            };
            _animationTimer.Elapsed += (s, e) => AnimationTimerElapsed(callSign);
            _animationTimer.Start();

            _airportsLayer1.IsVisible = _airportsLayer2.IsVisible = _airportsLayer3.IsVisible = _groundPointsOverlay.IsVisible = true;
        }

        private void AnimationTimerElapsed(string callSign)
        {
            _arView.OriginCamera = _arView.OriginCamera.MoveTo(_airplaneFinder.planes[callSign].graphic.Geometry.Extent.GetCenter());

            // Update center point so plane doesnt go out of boundary.
            _airplaneFinder.center = _arView.OriginCamera.Location;
        }

        private void DisableFromPlaneView()
        {
            // Hide flyover toolbar
            _flyoverToolbar.Hidden = true;
            _animationTimer?.Stop();
            _arView.OriginCamera = _groundCamera;
            _airplaneFinder.center = _groundCamera.Location;

            // Re-enable plane graphic
            _flightInfoVC.GetPlane().graphic.IsVisible = true;

            // Configure scene view display for real-scale AR: no space effect or atmosphere effect.
            _arView.SpaceEffect = SpaceEffect.None;
            _arView.AtmosphereEffect = AtmosphereEffect.None;

            // Enable subsurface
            _arView.Scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            if (_IsCalibrating)
            {
                _arView.Scene.BaseSurface.Opacity = 0.5;
            }
            else
            {
                _arView.Scene.BaseSurface.Opacity = 0;
            }

            _arView.GeoViewTapped += _arView_GeoViewTapped;
            _CalibrateButton.Hidden = false;

            _airportsLayer1.IsVisible = _airportsLayer2.IsVisible =_airportsLayer3.IsVisible = _groundPointsOverlay.IsVisible = false;
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // Stop ARKit tracking and unsubscribe from events when the view closes.
            await _arView?.StopTrackingAsync();
            _arView.GeoViewTapped -= _arView_GeoViewTapped;
            _CalibrateButton.TouchUpInside -= _CalibrateButton_TouchUpInside; 
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            if (!View.Subviews.ToList().Contains(_flightInfoVC.View))
            {
                return;
            }

            NSLayoutConstraint.DeactivateConstraints(_flightInfoVC_HorizontalConstraints);
            NSLayoutConstraint.DeactivateConstraints(_flightInfoVC_VerticalConstraints);

            if (TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                NSLayoutConstraint.ActivateConstraints(_flightInfoVC_VerticalConstraints);
            }
            else
            {
                NSLayoutConstraint.ActivateConstraints(_flightInfoVC_HorizontalConstraints);
            }
        }
    }
}