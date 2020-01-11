using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.UI;
using System.Net.Http;
using Esri.ArcGISRuntime.UI.Controls;
using System.Timers;
using ArcGISRuntime.Samples.Managers;

namespace Test
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


    /// <summary>
    /// Provides map data to an application
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {
        private Timer _animationTimer;
        private Dictionary<String, Plane> planes = new Dictionary<String, Plane>();
        private GraphicsOverlay _graphicsOverlay;
        public SceneView SceneView;
        private ModelSceneSymbol plane3DSymbol;
        private SpatialReference sr;
        private SimpleMarkerSceneSymbol _tappedPointSymbol = new SimpleMarkerSceneSymbol(SimpleMarkerSceneSymbolStyle.Diamond, System.Drawing.Color.Orange, 500.0, 500.0, 500.0, SceneSymbolAnchorPosition.Center);
        public int updates_per_second = 5;
        public int seconds_per_query = 10;
        private static readonly HttpClient client = new HttpClient();

        public MapViewModel()
        {
            CreateNewScene();
        }

        private static string GetModelPath()
        {
            //DataManager.DownloadDataItem("681d6f7694644709a7c830ec57a2d72b");
            //return DataManager.GetDataFolder("681d6f7694644709a7c830ec57a2d72b", "Bristol.dae");
            DataManager.DownloadDataItem("21274c9a36f445db912c7c31d2eb78b7");   
            return DataManager.GetDataFolder("21274c9a36f445db912c7c31d2eb78b7", "Boeing787\\B_787_8.dae");
        }

        private async void CreateNewScene()
        {
            sr = new SpatialReference(4326);
            String modelPath = GetModelPath();
            plane3DSymbol = await ModelSceneSymbol.CreateAsync(new Uri(modelPath), 5.0);
            Scene newScene = new Scene(Basemap.CreateImageryWithLabels());
            Scene = newScene;

            //solves timing issues
            FeatureLayer trailHeadsLayer = new FeatureLayer(new Uri("https://services3.arcgis.com/GVgbJbqm8hXASVYi/arcgis/rest/services/Trailheads/FeatureServer/0"));
            await trailHeadsLayer.LoadAsync();
    
            _graphicsOverlay = new GraphicsOverlay();
            _graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
            SceneView.GraphicsOverlays.Add(_graphicsOverlay);
            SimpleRenderer renderer3D = new SimpleRenderer();
            RendererSceneProperties renderProperties = renderer3D.SceneProperties;
            // Use expressions to keep the renderer properties updated as parameters of the rendered object
            renderProperties.HeadingExpression = "[HEADING]";
            renderProperties.PitchExpression = "[PITCH]";
            renderProperties.RollExpression = "[ROLL]";
            // Apply the renderer to the scene view's overlay
            _graphicsOverlay.Renderer = renderer3D;


            MapPoint mp = new MapPoint(-117.18, 33.5556, 10000, sr);
            Camera cm = new Camera(mp, 0, 90, 0);
            SceneView.SetViewpointCamera(cm);


            queryPlanes();


            _animationTimer = new Timer(1000 / updates_per_second)
            {
                Enabled = true,
                AutoReset = true
            };

            // Move the plane every time the timer expires
            _animationTimer.Elapsed += AnimatePlane;

            _animationTimer.Start();
        }

        // a max_planes of 0 will not apply a max limit
        //private async void queryPlanes(int max_planes)
        private async void queryPlanes()
        {
            int max_planes = 0;
            // var response = await client.GetAsync("https://matt9678:Window430@opensky-network.org/api/states/all");
            //var response = await client.GetAsync("https://matt9678:Window430@opensky-network.org/api/states/all?lamin=31.87845&lomin=-119.81135&lamax=34.98221&lomax=-114.54345");
            var response = await client.GetAsync("https://matt9678:Window430@opensky-network.org/api/states/all?lamin=33.82&lomin=-117.781&lamax=34.616&lomax=-115.712");
            var responseString = await response.Content.ReadAsStringAsync();
            
            Int32 time_message_sent = Convert.ToInt32(responseString.Substring(8, 10));
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            String states = responseString.Substring(30, responseString.Length - 30);
            String[] elements = states.Split('[');

            if(max_planes == 0)
            {
                max_planes = elements.Length;
            }
            for (int i = 0; i < max_planes; i++)
            {
                String[] attributes = elements[i].Split(',');
                if (attributes[5] != "null" || attributes[5] != "null")
                {
                    String callsign = attributes[1].Substring(1, attributes[1].Length-2);
                    Int32 last_timestamp = 0;
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
                    if(attributes[3] != "null")
                    {
                        last_timestamp = Convert.ToInt32(attributes[3]);
                    }
                    MapPoint g = new MapPoint(lon, lat, alt, sr);
                    Int32 time_difference = unixTimestamp - last_timestamp;

                    List<MapPoint> lmp = new List<MapPoint>();
                    lmp.Add(g);
                    IReadOnlyList<MapPoint> new_location = GeometryEngine.MoveGeodetic(lmp, velocity * time_difference, LinearUnits.Meters, heading, AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                    double dz = new_location[0].Z + (vert_rate * time_difference);
                    MapPoint ng = new MapPoint(new_location[0].X, new_location[0].Y, dz, g.SpatialReference);

                    if (planes.ContainsKey(callsign))
                    {
                        Plane currPlane = planes[callsign];
                        currPlane.graphic.Geometry = ng;
                        currPlane.graphic.IsSelected = true;
                        currPlane.graphic.Attributes["HEADING"] = heading + 180;
                        currPlane.velocity = velocity;
                        currPlane.vert_rate = vert_rate;
                        currPlane.heading = heading;
                    }
                    else
                    {
                        Graphic gr = new Graphic(ng, plane3DSymbol);
                        gr.IsSelected = true;
                        gr.Attributes["HEADING"] = heading + 180;
                        Plane p = new Plane(gr, velocity, vert_rate, heading);
                        planes.Add(callsign, p);
                        _graphicsOverlay.Graphics.Add(gr);
                    }
                }
            }
        }

        private int updateCounter = 0;
        private void AnimatePlane(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            updateCounter++;
            if (updateCounter % (updates_per_second * seconds_per_query) == 0)
            {
                updateCounter = 0;
                queryPlanes();
            }
            else
            {
                foreach (var plane in planes)
                {
                    MapPoint g = (MapPoint)plane.Value.graphic.Geometry;
                    List<MapPoint> lmp = new List<MapPoint>();
                    lmp.Add(g);
                    IReadOnlyList<MapPoint> new_location = GeometryEngine.MoveGeodetic(lmp, plane.Value.velocity / updates_per_second, LinearUnits.Meters, plane.Value.heading, AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                    double dz = new_location[0].Z + (plane.Value.vert_rate / updates_per_second);
                    MapPoint ng = new MapPoint(new_location[0].X, new_location[0].Y, dz, g.SpatialReference);
                    plane.Value.graphic.Geometry = ng;
                    plane.Value.graphic.IsSelected = false;
                }
            }
        }

        private Scene _scene;
        public Scene Scene
        {
            get { return _scene; }
            set { _scene = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Raises the <see cref="MapViewModel.PropertyChanged" /> event
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var propertyChangedHandler = PropertyChanged;
            if (propertyChangedHandler != null)
                propertyChangedHandler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
