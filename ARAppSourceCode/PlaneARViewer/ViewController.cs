﻿using Esri.ArcGISRuntime.ARToolkit;
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

namespace PlaneARViewer
{
    public class Plane
    {
        public Graphic graphic;
        public double velocity;
        public double vert_rate;
        public double heading;

        public Plane(Graphic p_graphic, double p_velocity, double p_vert_rate, double p_heading)
        {
            graphic = p_graphic;
            velocity = p_velocity;
            vert_rate = p_vert_rate;
            heading = p_heading;
        }
    }

    public partial class ViewController : UIViewController
    {
        private Timer _animationTimer;
        private Dictionary<String, Plane> planes = new Dictionary<String, Plane>();
        private ModelSceneSymbol plane3DSymbol;
        private SpatialReference sr;

        private static readonly HttpClient client = new HttpClient();

        // UI objects.
        private ARSceneView _arView;
        private UILabel _helpLabel;

        // Location data source for AR and route tracking.
        private AdjustableLocationDataSource _locationSource = new AdjustableLocationDataSource();

        // Elevation source for AR scene calibration.
        private ArcGISTiledElevationSource _elevationSource;
        private Surface _elevationSurface;

        // Overlay for testing plane graphics.
        private GraphicsOverlay _graphicsOverlay;

        private PanCompassCalibrationGestureRecognizer _panCalibrator;

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

                // Create and add the elevation source.
                _elevationSource = new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer"));
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

                sr = SpatialReferences.Wgs84;

                String modelPath = GetModelPath();
                plane3DSymbol = await ModelSceneSymbol.CreateAsync(new Uri(modelPath), 50.0);

                _graphicsOverlay = new GraphicsOverlay();
                _graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
                _arView.GraphicsOverlays.Add(_graphicsOverlay);
                SimpleRenderer renderer3D = new SimpleRenderer();
                RendererSceneProperties renderProperties = renderer3D.SceneProperties;
                // Use expressions to keep the renderer properties updated as parameters of the rendered object
                renderProperties.HeadingExpression = "[HEADING]";
                renderProperties.PitchExpression = "[PITCH]";
                renderProperties.RollExpression = "[ROLL]";
                // Apply the renderer to the scene view's overlay
                _graphicsOverlay.Renderer = renderer3D;

                queryPlanes();


                _animationTimer = new Timer(1000)
                {
                    Enabled = true,
                    AutoReset = true
                };

                // Move the plane every time the timer expires
                _animationTimer.Elapsed += AnimatePlane;

                _animationTimer.Start();

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

        private async void UpdateElevation(object sender, Location e)
        {
            try
            {
                await _elevationSurface.LoadAsync();
                double elevation = await _elevationSurface.GetElevationAsync(e.Position);
                ((AdjustableLocationDataSource)(_arView.LocationDataSource)).AltitudeOffset = elevation + 5;
                _arView.LocationDataSource.LocationChanged -= UpdateElevation;
                _graphicsOverlay.Graphics.Add(new Graphic(new MapPoint(e.Position.X, e.Position.Y, e.Position.Z + 2800, e.Position.SpatialReference)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override void LoadView()
        {
            View = new UIView { BackgroundColor = UIColor.White };

            UIToolbar toolbar = new UIToolbar();
            toolbar.TranslatesAutoresizingMaskIntoConstraints = false;

            _arView = new ARSceneView();
            _arView.TranslatesAutoresizingMaskIntoConstraints = false;

            _helpLabel = new UILabel();
            _helpLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            _helpLabel.TextAlignment = UITextAlignment.Center;
            _helpLabel.TextColor = UIColor.White;
            _helpLabel.BackgroundColor = UIColor.FromWhiteAlpha(0, 0.6f);
            _helpLabel.Text = "Plane Gang 2020";

            toolbar.Items = new[]
            {
                new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
            };

            View.AddSubviews(_arView, toolbar, _helpLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _arView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _arView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _arView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _arView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                toolbar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                toolbar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                toolbar.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor),
                _helpLabel.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _helpLabel.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _helpLabel.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _helpLabel.HeightAnchor.ConstraintEqualTo(40)
            });
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            // Start tracking as soon as the view has been shown.
            await _arView.StartTrackingAsync(ARLocationTrackingMode.Continuous);
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // Stop ARKit tracking and unsubscribe from events when the view closes.
            await _arView?.StopTrackingAsync();
        }




        private async void queryPlanes()
        {
            try
            {
                int max_planes = 0;
                // var response = await client.GetAsync("https://matt9678:Window430@opensky-network.org/api/states/all");
                //var response = await client.GetAsync("https://matt9678:Window430@opensky-network.org/api/states/all?lamin=31.87845&lomin=-119.81135&lamax=34.98221&lomax=-114.54345");
                var response = await client.GetAsync("https://opensky-network.org/api/states/all?lamin=33.82&lomin=-117.781&lamax=34.616&lomax=-115.712");
                var responseString = await response.Content.ReadAsStringAsync();

                String states = responseString.Substring(30, responseString.Length - 30);
                String[] elements = states.Split('[');

                if (max_planes == 0)
                {
                    max_planes = elements.Length;
                }
                for (int i = 0; i < max_planes; i++)
                {
                    String[] attributes = elements[i].Split(',');
                    if (attributes[5] != "null" || attributes[5] != "null")
                    {
                        String callsign = attributes[1].Substring(1, attributes[1].Length - 2);
                        double lon = Convert.ToDouble(attributes[5]);
                        double lat = Convert.ToDouble(attributes[6]);
                        double alt = 0.0;
                        if (attributes[13] != "null")
                        {
                            alt = Convert.ToDouble(attributes[13]);
                        }
                        else if (attributes[7] != "null")
                        {
                            alt = Convert.ToDouble(attributes[7]);
                        }

                        double velocity = 0.0;
                        double heading = 0.0;
                        double vert_rate = 0.0;
                        if (attributes[9] != "null")
                        {
                            velocity = Convert.ToDouble(attributes[9]);
                        }
                        if (attributes[10] != "null")
                        {
                            heading = Convert.ToDouble(attributes[10]);
                        }
                        if (attributes[11] != "null")
                        {
                            vert_rate = Convert.ToDouble(attributes[11]);
                        }
                        Geometry g = new MapPoint(lon, lat, alt, sr);
                        if (planes.ContainsKey(callsign))
                        {
                            Plane currPlane = planes[callsign];
                            currPlane.graphic.Geometry = g;
                            currPlane.graphic.Attributes["HEADING"] = heading + 180;
                            currPlane.velocity = velocity;
                            currPlane.vert_rate = vert_rate;
                            currPlane.heading = heading;
                        }
                        else
                        {
                            Graphic gr = new Graphic(g, plane3DSymbol);
                            gr.Attributes["HEADING"] = heading + 180;
                            Plane p = new Plane(gr, velocity, vert_rate, heading);
                            planes.Add(callsign, p);
                            _graphicsOverlay.Graphics.Add(gr);
                        }
                    }
                }
            } catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private int updateCounter = 0;
        private void AnimatePlane(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            updateCounter++;
            if (updateCounter % 10 == 0)
            {
                queryPlanes();
            }
            else
            {
                foreach (var plane in planes)
                {
                    MapPoint g = (MapPoint)plane.Value.graphic.Geometry;
                    List<MapPoint> lmp = new List<MapPoint>();
                    lmp.Add(g);
                    IReadOnlyList<MapPoint> new_location = GeometryEngine.MoveGeodetic(lmp, plane.Value.velocity, LinearUnits.Meters, plane.Value.heading, AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                    double dz = new_location[0].Z + plane.Value.vert_rate;
                    MapPoint ng = new MapPoint(new_location[0].X, new_location[0].Y, dz, g.SpatialReference);
                    plane.Value.graphic.Geometry = ng;
                }
            }
        }

        private static string GetModelPath()
        {
            //DataManager.DownloadDataItem("681d6f7694644709a7c830ec57a2d72b");
            //return DataManager.GetDataFolder("681d6f7694644709a7c830ec57a2d72b", "Bristol.dae");
            DataManager.DownloadDataItem("21274c9a36f445db912c7c31d2eb78b7");
            return DataManager.GetDataFolder("21274c9a36f445db912c7c31d2eb78b7", "Boeing787", "B_787_8.dae");
        }


    }
}