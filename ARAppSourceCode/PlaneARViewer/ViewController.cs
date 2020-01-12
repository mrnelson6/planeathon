using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using UIKit;
using System.Net.Http;
using System.Timers;

using PlaneARViewer.Calibration;
using System.Linq;

namespace PlaneARViewer
{


    public partial class ViewController : UIViewController
    {

        SharedAirplaneFinder.AirplaneFinder sc;
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
            catch(Exception ex)
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
            var res = await _arView.IdentifyGraphicsOverlayAsync(sc._graphicsOverlay, e.Position, 30, false);
            if (res.Graphics.Any())
            {
                Console.WriteLine(res.Graphics.First());

                string callsign = res.Graphics.First().Attributes["CALLSIGN"] as string;

                new UIAlertView(callsign, "identified", null, "ok").Show();
            }
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // Stop ARKit tracking and unsubscribe from events when the view closes.
            await _arView?.StopTrackingAsync();
        }


    }
}