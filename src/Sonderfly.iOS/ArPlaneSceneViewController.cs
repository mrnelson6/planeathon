// Copyright 2020 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Drawing;
using System.Linq;
using System.Timers;
using Sonderfly.iOS.BottomSheet;
using Sonderfly.iOS.Calibration;
using UIKit;

namespace Sonderfly.iOS
{
    public class ArPlaneSceneViewController : UIViewController
    {
        private AirplaneFinder _airplaneFinder;

        // UI objects.
        private ARSceneView _arView;
        private UIToolbar _flyoverToolbar;
        private UIButton _calibrateButton;

        // Location data source for AR and route tracking.
        private readonly AdjustableLocationDataSource _locationSource = new AdjustableLocationDataSource();

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
        private Camera _groundCamera;

        private bool _isCalibrating = true;

        // Timer control enables stopping and starting frame-by-frame animation.
        private Timer _animationTimer;
        private FlightInfoViewController _flightInfoVc;
        private NSLayoutConstraint[] _flightInfoVcHorizontalConstraints;
        private NSLayoutConstraint[] _flightInfoVcVerticalConstraints;

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
                _locationSource.HeadingChanged += LocationHeadingChanged;
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
                GraphicsOverlay identifyOverlay = new GraphicsOverlay();
                
                _arView.GraphicsOverlays.Add(_graphicsOverlay);
                _airplaneFinder = new AirplaneFinder(_graphicsOverlay, identifyOverlay)
                {
                    Center = _locationSource.LastLocation.Position
                };
                _airplaneFinder.SetupScene();
                _flightInfoVc.AssociateAirplaneFinder(_airplaneFinder);
                // Disable scene interaction.
                _arView.InteractionOptions = new SceneViewInteractionOptions() { IsEnabled = false };

                // Get the elevation value.
                _arView.LocationDataSource.LocationChanged += UpdateElevation;

                _panCalibrator = new PanCompassCalibrationGestureRecognizer(_locationSource);
                View.GestureRecognizers = new UIGestureRecognizer[] { _panCalibrator };

                _groundPointsOverlay = new GraphicsOverlay
                {
                    SceneProperties = {SurfacePlacement = SurfacePlacement.DrapedBillboarded}, IsVisible = false
                };
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

        private void LocationHeadingChanged(object sender, double e)
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

            _flyoverToolbar = new UIToolbar {TranslatesAutoresizingMaskIntoConstraints = false, Hidden = true};

            _arView = new ARSceneView {TranslatesAutoresizingMaskIntoConstraints = false};

            _calibrateButton = new UIButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundColor = UIColor.SecondarySystemBackgroundColor
            };
            _calibrateButton.Layer.CornerRadius = 8;
            _calibrateButton.Layer.Opacity = 0.6f;
            _calibrateButton.SetImage(UIImage.GetSystemImage("globe").ApplyTintColor(UIColor.White), UIControlState.Normal);
            
            var backButton = new UIBarButtonItem() { Title = "Back to ground" };
            backButton.Clicked += (s, e) => DisableFromPlaneView();
            _flyoverToolbar.Items = new[]
            {
                backButton
            };

            _flightInfoVc = new FlightInfoViewController();

            AddChildViewController(_flightInfoVc);
            _flightInfoVc.View.TranslatesAutoresizingMaskIntoConstraints = false;

            View.AddSubviews(_arView, _flyoverToolbar, _calibrateButton);//, toolbar);//, _helpLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _arView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _arView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _arView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _arView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                _flyoverToolbar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _flyoverToolbar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _flyoverToolbar.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor),
                _calibrateButton.HeightAnchor.ConstraintEqualTo(48),
                _calibrateButton.WidthAnchor.ConstraintEqualTo(48),
                _calibrateButton.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 16),
                _calibrateButton.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -16)
            });

            _flightInfoVcHorizontalConstraints = new []
            {
                _flightInfoVc.View.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor),
                _flightInfoVc.View.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 16),
                _flightInfoVc.View.BottomAnchor.ConstraintEqualTo(_arView.SafeAreaLayoutGuide.BottomAnchor, -16),
                _flightInfoVc.View.WidthAnchor.ConstraintEqualTo(320)
            };

            _flightInfoVcVerticalConstraints = new []
            {
                _flightInfoVc.View.BottomAnchor.ConstraintEqualTo(_arView.SafeAreaLayoutGuide.BottomAnchor, -16),
                _flightInfoVc.View.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _flightInfoVc.View.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _flightInfoVc.View.HeightAnchor.ConstraintEqualTo(_flightInfoVc.GetViewHeight())
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

            _arView.GeoViewTapped += ArSceneViewTapped;
            _flightInfoVc.MapButton.TouchUpInside += ShowMapView;
            _flightInfoVc.FlyoverButton.TouchUpInside += ShowFlyoverView;
            _calibrateButton.TouchUpInside += CalibrateButtonTapped;
        }

        private void CalibrateButtonTapped(object sender, EventArgs e)
        {
            _isCalibrating = !_isCalibrating;

            if (_isCalibrating)
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

        private async void ArSceneViewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            _airplaneFinder.ShouldUpdateIdentifyOverlay = false;
            _arView.GraphicsOverlays.Add(_airplaneFinder.IdentifyOverlay);
            var res = await _arView.IdentifyGraphicsOverlayAsync(_airplaneFinder.IdentifyOverlay, e.Position, 64, false, 1);
            _arView.GraphicsOverlays.Remove(_airplaneFinder.IdentifyOverlay);
            _airplaneFinder.ShouldUpdateIdentifyOverlay = true;
            if (res.Graphics.Any() && res.Graphics.First().Attributes["CALLSIGN"] is string callSign)
            {
                if (_airplaneFinder.Planes.ContainsKey(callSign))
                {
                    Plane targetPlane = _airplaneFinder.Planes[callSign];
                    _airplaneFinder.SelectedPlane = targetPlane;

                    View.AddSubview(_flightInfoVc.View);

                    if (TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Regular)
                    {
                        NSLayoutConstraint.ActivateConstraints(_flightInfoVcVerticalConstraints);
                    }
                    else
                    {
                        NSLayoutConstraint.ActivateConstraints(_flightInfoVcHorizontalConstraints);
                    }
                }
            }
            else
            {
                NSLayoutConstraint.DeactivateConstraints(_flightInfoVcHorizontalConstraints);
                NSLayoutConstraint.DeactivateConstraints(_flightInfoVcVerticalConstraints);
                _flightInfoVc.View.RemoveFromSuperview();
            }
        }

        private async void EnableFromPlaneView()
        {
            _calibrateButton.Hidden = true;
            NSLayoutConstraint.DeactivateConstraints(_flightInfoVcHorizontalConstraints);
            NSLayoutConstraint.DeactivateConstraints(_flightInfoVcVerticalConstraints);
            _flightInfoVc.View.RemoveFromSuperview();

            _flyoverToolbar.Hidden = false;

            var selectedPlane = _flightInfoVc.GetPlane().Graphic;
            var callSign = _flightInfoVc.GetPlane().CallSign;

            _arView.GeoViewTapped -= ArSceneViewTapped;

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
            _arView.OriginCamera = _arView.OriginCamera.MoveTo(_airplaneFinder.Planes[callSign].Graphic.Geometry.Extent.GetCenter());

            // Update center point so plane doesn't go out of boundary.
            _airplaneFinder.Center = _arView.OriginCamera.Location;
        }

        private void DisableFromPlaneView()
        {
            // Hide flyover toolbar
            _flyoverToolbar.Hidden = true;
            _animationTimer?.Stop();
            _arView.OriginCamera = _groundCamera;
            _airplaneFinder.Center = _groundCamera.Location;

            // Re-enable plane graphic
            _flightInfoVc.GetPlane().Graphic.IsVisible = true;

            // Configure scene view display for real-scale AR: no space effect or atmosphere effect.
            _arView.SpaceEffect = SpaceEffect.None;
            _arView.AtmosphereEffect = AtmosphereEffect.None;

            // Enable subsurface
            _arView.Scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            if (_isCalibrating)
            {
                _arView.Scene.BaseSurface.Opacity = 0.5;
            }
            else
            {
                _arView.Scene.BaseSurface.Opacity = 0;
            }

            _arView.GeoViewTapped += ArSceneViewTapped;
            _calibrateButton.Hidden = false;

            _airportsLayer1.IsVisible = _airportsLayer2.IsVisible =_airportsLayer3.IsVisible = _groundPointsOverlay.IsVisible = false;
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // Stop ARKit tracking and unsubscribe from events when the view closes.
            if (_arView != null)
            {
                await _arView.StopTrackingAsync();
                _arView.GeoViewTapped -= ArSceneViewTapped;
                _calibrateButton.TouchUpInside -= CalibrateButtonTapped;
            }
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            if (!View.Subviews.ToList().Contains(_flightInfoVc.View))
            {
                return;
            }

            NSLayoutConstraint.DeactivateConstraints(_flightInfoVcHorizontalConstraints);
            NSLayoutConstraint.DeactivateConstraints(_flightInfoVcVerticalConstraints);

            if (TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                NSLayoutConstraint.ActivateConstraints(_flightInfoVcVerticalConstraints);
            }
            else
            {
                NSLayoutConstraint.ActivateConstraints(_flightInfoVcHorizontalConstraints);
            }
        }
    }
}