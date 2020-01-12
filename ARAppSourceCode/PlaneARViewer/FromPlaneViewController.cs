using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using PlaneARViewer.Calibration;
using System;
using System.Linq;
using UIKit;

namespace PlaneARViewer
{
    public partial class FromPlaneViewController : UIViewController
    {
        public string CallSign;

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

        public FromPlaneViewController(IntPtr handle) : base(handle)
        {
        }

        public FromPlaneViewController()
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
            // Create the scene with a basemap.
            Scene flyoverScene = new Scene(Basemap.CreateImagery());

            // Create the integrated mesh layer and add it to the scene.
            IntegratedMeshLayer meshLayer =
                new IntegratedMeshLayer(
                    new Uri("https://www.arcgis.com/home/item.html?id=dbc72b3ebb024c848d89a42fe6387a1b"));
            flyoverScene.OperationalLayers.Add(meshLayer);

            try
            {
                // Wait for the layer to load so that extent is available.
                await meshLayer.LoadAsync();

                // Start with the camera at the center of the mesh layer.
                Envelope layerExtent = meshLayer.FullExtent;
                Camera originCamera = new Camera(layerExtent.GetCenter().Y, layerExtent.GetCenter().X, 600, 0, 90, 0);
                _arView.OriginCamera = originCamera;

                // set the translation factor to enable rapid movement through the scene.
                _arView.TranslationFactor = 1000;

                // Enable atmosphere and space effects for a more immersive experience.
                _arView.SpaceEffect = SpaceEffect.Stars;
                _arView.AtmosphereEffect = AtmosphereEffect.Realistic;

                Uri serviceUri = new Uri("https://dev0011356.esri.com/server/rest/services/Hosted/Latest_Flights_1578796112/FeatureServer/0");
                FeatureLayer planesLayer = new FeatureLayer(serviceUri);
                try
                {
                    await planesLayer.LoadAsync();
                }
                catch (Exception ex)
                {
                }

                try
                {
                    // Create a query parameters that will be used to Query the feature table.
                    QueryParameters queryParams = new QueryParameters();

                    // Construct and assign the where clause that will be used to query the feature table.
                    queryParams.WhereClause = "upper(CALLSIGN) LIKE '%" + CallSign + "%'";

                    // Query the feature table.
                    FeatureQueryResult queryResult = await planesLayer.FeatureTable.QueryFeaturesAsync(queryParams);

                    // Cast the QueryResult to a List so the results can be interrogated.
                    var currentFlight = queryResult.ToList().First();

                    _arView.OriginCamera = new Camera(currentFlight.Geometry.Extent.GetCenter(), 0, 0, 0);
                }
                catch (Exception ex)
                {
                }

                // Display the scene.
                await flyoverScene.LoadAsync();
                _arView.Scene = flyoverScene;
                new UIAlertView("Success", CallSign, (IUIAlertViewDelegate)null, "OK", null).Show();
            }
            catch (Exception ex)
            {
                new UIAlertView("Error", "Failed to start AR", (IUIAlertViewDelegate)null, "OK", null).Show();
                System.Diagnostics.Debug.WriteLine(ex);
            }
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
            await _arView.StartTrackingAsync();
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // Stop ARKit tracking and unsubscribe from events when the view closes.
            await _arView?.StopTrackingAsync();
        }
    }
}