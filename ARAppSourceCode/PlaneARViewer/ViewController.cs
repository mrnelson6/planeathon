﻿using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using PlaneARViewer.Calibration;
using System;
using System.Linq;
using System.Timers;
using UIKit;

namespace PlaneARViewer
{
    public partial class ViewController : UIViewController
    {
        private SharedAirplaneFinder.AirplaneFinder sc;

        // UI objects.
        private ARSceneView _arView;
        private UILabel _helpLabel;

        // Location data source for AR and route tracking.
        private AdjustableLocationDataSource _locationSource = new AdjustableLocationDataSource();

        // Elevation source for AR scene calibration.
        private ArcGISTiledElevationSource _elevationSource;
        private Surface _elevationSurface;

        private PanCompassCalibrationGestureRecognizer _panCalibrator;

        // Overlay for testing plane graphics.
        private GraphicsOverlay _graphicsOverlay;

        // Using the view from an aircraft.
        private bool _fromPlaneView;
        private Camera _groundCamera;

        // Timer control enables stopping and starting frame-by-frame animation.
        private Timer _animationTimer;

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
            //Uri serviceUri = new Uri("https//dev0011356.esri.com/server/rest/services/Hosted/Latest_Flights_1578796112/FeatureServer/0");
            //FeatureLayer planesLayer = new FeatureLayer(serviceUri);
            //try
            //{
            //    await planesLayer.LoadAsync();
            //}
            //catch (Exception ex)
            //{
            //}
            try
            {
                // Create and add the scene.
                _arView.Scene = new Scene(Basemap.CreateImageryWithLabels());

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
                _arView.GraphicsOverlays.Add(_graphicsOverlay);
                sc = new SharedAirplaneFinder.AirplaneFinder(_graphicsOverlay);
                sc.center = _locationSource.LastLocation.Position;
                sc.setupScene();
                // Disable scene interaction.
                _arView.InteractionOptions = new SceneViewInteractionOptions() { IsEnabled = false };

                // Get the elevation value.
                _arView.LocationDataSource.LocationChanged += UpdateElevation;

                _panCalibrator = new PanCompassCalibrationGestureRecognizer(_locationSource);
                View.GestureRecognizers = new[] { _panCalibrator };
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override void LoadView()
        {
            View = new UIView { BackgroundColor = UIColor.White };

            //UIToolbar toolbar = new UIToolbar();
            //toolbar.TranslatesAutoresizingMaskIntoConstraints = false;

            _arView = new ARSceneView();
            _arView.TranslatesAutoresizingMaskIntoConstraints = false;

            //_helpLabel = new UILabel();
            //_helpLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            //_helpLabel.TextAlignment = UITextAlignment.Center;
            //_helpLabel.TextColor = UIColor.White;
            //_helpLabel.BackgroundColor = UIColor.FromWhiteAlpha(0, 0.6f);
            //_helpLabel.Text = "Plane Gang 2020";

            //toolbar.Items = new[]
            //{
            //    new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
            //};

            View.AddSubviews(_arView);//, toolbar);//, _helpLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _arView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _arView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _arView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _arView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                //toolbar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                //toolbar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                //toolbar.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor),
                //_helpLabel.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                //_helpLabel.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                //_helpLabel.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                //_helpLabel.HeightAnchor.ConstraintEqualTo(40)
            });
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            // Start tracking as soon as the view has been shown.
            await _arView.StartTrackingAsync(ARLocationTrackingMode.Continuous);

            _arView.GeoViewTapped += _arView_GeoViewTapped;
        }

        private async void _arView_GeoViewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            var res = await _arView.IdentifyGraphicsOverlayAsync(sc._graphicsOverlay, e.Position, 80, false);
            try
            {
                if (res.Graphics.Any())
                {
                    Console.WriteLine(res.Graphics.First());

                    string callsign = res.Graphics.First().Attributes["CALLSIGN"] as string;

                    await _arView.StopTrackingAsync();
                    //NavigationController.PushViewController(new FromPlaneViewController() { CallSign = callsign }, true);
                    //new UIAlertView(callsign, "identified", null, "ok").Show();
                    EnableFromPlaneView(res.Graphics.First(), callsign);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private async void EnableFromPlaneView(Graphic selectedPlane, string callSign)
        {
            _arView.GeoViewTapped -= _arView_GeoViewTapped;

            // Store the ground camera position.
            _groundCamera = _arView.OriginCamera;

            await _arView.LocationDataSource.StopAsync();

            // switch camera to the position of the plane.
            //_arView.OriginCamera = new Camera(selectedPlane.Geometry.Extent.GetCenter(), _groundCamera.Heading, _groundCamera.Pitch, _groundCamera.Roll);
            await _arView.StopTrackingAsync();
            _arView.OriginCamera = new Camera(selectedPlane.Geometry.Extent.GetCenter(), _groundCamera.Heading, _groundCamera.Pitch, _groundCamera.Roll);
            await _arView.StartTrackingAsync();

            // Configure scene view display for real-scale AR: no space effect or atmosphere effect.
            _arView.SpaceEffect = SpaceEffect.Stars;
            _arView.AtmosphereEffect = AtmosphereEffect.Realistic;
            _arView.Scene.BaseSurface.Opacity = 1.0;

            //disable subsurface
            _arView.Scene.BaseSurface.NavigationConstraint = NavigationConstraint.StayAbove;

            selectedPlane.IsVisible = false;

            //animation
            _animationTimer = new Timer(33)
            {
                AutoReset = true
            };
            _animationTimer.Elapsed += (s, e) => AnimationTimerElapsed(callSign);
            _animationTimer.Start();
        }

        private async void AnimationTimerElapsed(string callSign)
        {
            //_arView.OriginCamera = new Camera(selectedPlane.Geometry.Extent.GetCenter(), _groundCamera.Heading, _groundCamera.Pitch, _groundCamera.Roll);
            _arView.OriginCamera = _arView.OriginCamera.MoveTo(sc.planes[callSign].graphic.Geometry.Extent.GetCenter());
            sc.center = _arView.OriginCamera.Location;
        }

        private void DisableFromPlaneView()
        {
            _animationTimer?.Stop();
            _arView.OriginCamera = _groundCamera;
            // Configure scene view display for real-scale AR: no space effect or atmosphere effect.
            _arView.SpaceEffect = SpaceEffect.None;
            _arView.AtmosphereEffect = AtmosphereEffect.None;

            // Enable subsurface
            _arView.Scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
            _arView.Scene.BaseSurface.Opacity = 0.5;

            _arView.GeoViewTapped += _arView_GeoViewTapped;
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // Stop ARKit tracking and unsubscribe from events when the view closes.
            await _arView?.StopTrackingAsync();
        }
    }
}