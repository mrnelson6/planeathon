using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace SharedAirplaneFinder
{
    public class Plane : INotifyPropertyChanged
    {
        private string _callSign;
        private double _velocity;
        private double _vertRate;
        private double _heading;

        public Graphic graphic;
        public string callsign
        {
            get => _callSign;
            set
            {
                if (_callSign != value)
                {
                    _callSign = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(callsign)));
                }
            }
        }
        public double velocity
        {
            get => _velocity;
            set
            {
                if (_velocity != value)
                {
                    _velocity = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(velocity)));
                }
            }
        }
        public double vert_rate
        {
            get => _vertRate;
            set
            {
                if (_vertRate != value)
                {
                    _vertRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(vert_rate)));
                }
            }
        }
        public double heading
        {
            get => _heading;
            set
            {
                if (_heading != value)
                {
                    _heading = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(heading)));
                }
            }
        }
        public Int32 last_update;
        public bool big_plane;

        public Plane(Graphic p_graphic, double p_velocity, double p_vert_rate, double p_heading, Int32 p_last_update, bool p_big_plane, string callsign)
        {
            graphic = p_graphic;
            velocity = p_velocity;
            vert_rate = p_vert_rate;
            heading = p_heading;
            last_update = p_last_update;
            big_plane = p_big_plane;
            this.callsign = callsign;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    class AirplaneFinder : INotifyPropertyChanged
    {
        public Timer _animationTimer;
        public Dictionary<String, Plane> planes = new Dictionary<String, Plane>();
        private ModelSceneSymbol smallPlane3DSymbol;
        private ModelSceneSymbol largePlane3DSymbol;
        private SpatialReference sr;
        public ServiceFeatureTable sft;

        private static readonly HttpClient client = new HttpClient();

        // Overlay for testing plane graphics.
        public GraphicsOverlay _graphicsOverlay;
        public int updates_per_second = 30;
        public int seconds_per_query = 10;
        public int small_plane_size = 60;
        public int large_plane_size = 20;
        public int seconds_per_cleanup = 30;
        public double coord_tolerance = 0.5;

        public bool ShouldUpdateIdentifyOverlay = true;

        // Overlay for identification purposes only.
        public GraphicsOverlay _identifyOverlay;

        private Plane _selectedPlane;
        public Plane SelectedPlane
        {
            get => _selectedPlane;
            set
            {
                if (_selectedPlane != value)
                {
                    _selectedPlane = value;
                    UpdateProperty(nameof(SelectedPlane));
                }
            }
        }

        public MapPoint center;

        public AirplaneFinder(GraphicsOverlay go, GraphicsOverlay identifyOverlay)
        {
            _graphicsOverlay = go;
            _identifyOverlay = identifyOverlay;
        }

        public async void setupScene()
        {
            string licenseKey = "";
            ArcGISRuntimeEnvironment.SetLicense(licenseKey);

            sr = SpatialReferences.Wgs84;

            smallPlane3DSymbol = await ModelSceneSymbol.CreateAsync(new Uri(await GetSmallPlane()), small_plane_size);
            largePlane3DSymbol = await ModelSceneSymbol.CreateAsync(new Uri(await GetLargePlane()), large_plane_size);

            _graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
            _identifyOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
            SimpleRenderer renderer3D = new SimpleRenderer();
            RendererSceneProperties renderProperties = renderer3D.SceneProperties;
            // Use expressions to keep the renderer properties updated as parameters of the rendered object
            renderProperties.HeadingExpression = "[HEADING]";
            renderProperties.PitchExpression = "[PITCH]";
            renderProperties.RollExpression = "[ROLL]";
            // Apply the renderer to the scene view's overlay
            _graphicsOverlay.Renderer = renderer3D;

            _identifyOverlay.Opacity = 0.01;
            _identifyOverlay.Renderer = new SimpleRenderer(new SimpleMarkerSceneSymbol(SimpleMarkerSceneSymbolStyle.Sphere,
                System.Drawing.Color.Red, 1000, 1000, 1000, SceneSymbolAnchorPosition.Center));
            await queryPlanes();
            _animationTimer = new Timer(1000 / updates_per_second)
            {
                Enabled = true,
                AutoReset = true
            };

            // Move the plane every time the timer expires
            _animationTimer.Elapsed += AnimatePlane;

            _animationTimer.Start();
        }

        private async Task addPlanesViaAPI()
        {
            try
            {
                MapPoint mp;
                if (center == null)
                {
                    mp = new MapPoint(-117.18, 33.5556, sr);
                }
                else
                {
                    mp = center;
                }
                Envelope en = new Envelope(mp, coord_tolerance, coord_tolerance);
                double xMax = en.XMax;
                double yMax = en.YMax;
                double xMin = en.XMin;
                double yMin = en.YMin;

                string call = "https://matt9678:Window430@opensky-network.org/api/states/all?lamin=" + yMin + "&lomin=" + xMin + "&lamax=" + yMax + "&lomax=" + xMax;
                var response = await client.GetAsync(call);
                var responseString = await response.Content.ReadAsStringAsync();
                Int32 time_message_sent = Convert.ToInt32(responseString.Substring(8, 10));
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                String states = responseString.Substring(30, responseString.Length - 30);
                String[] elements = states.Split('[');

                for (int i = 0; i < elements.Length; i++)
                {
                    String[] attributes = elements[i].Split(',');
                    if (attributes[5] != "null" || attributes[5] != "null")
                    {
                        String callsign = attributes[1].Substring(1, attributes[1].Length - 2);
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
                        if (attributes[3] != "null")
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
                        MapPoint identifyGeometry = new MapPoint(new_location[0].X, new_location[0].Y, dz, g.SpatialReference);

                        if (planes.ContainsKey(callsign))
                        {
                            Plane currPlane = planes[callsign];
                            currPlane.graphic.Geometry = ng;
                            currPlane.graphic.Attributes["HEADING"] = heading + 180;
                            currPlane.graphic.Attributes["CALLSIGN"] = callsign;
                            currPlane.velocity = velocity;
                            currPlane.vert_rate = vert_rate;
                            currPlane.heading = heading;
                            currPlane.last_update = last_timestamp;

                            if (ShouldUpdateIdentifyOverlay)
                            {
                                var res = _identifyOverlay.Graphics.Where(gxxx => gxxx.Attributes["CALLSIGN"].ToString() == callsign);

                                if (res.Any())
                                {
                                    res.First().Geometry = identifyGeometry;
                                }
                            }
                        }
                        else
                        {
                            if (callsign.Length > 0 && callsign[0] == 'N')
                            {
                                Graphic gr = new Graphic(ng, smallPlane3DSymbol);
                                gr.Attributes["HEADING"] = heading;
                                gr.Attributes["CALLSIGN"] = callsign;
                                Plane p = new Plane(gr, velocity, vert_rate, heading, last_timestamp, false, callsign);
                                planes.Add(callsign, p);
                                _graphicsOverlay.Graphics.Add(gr);

                                var identifyGraphic = new Graphic(identifyGeometry);
                                identifyGraphic.Attributes["CALLSIGN"] = callsign;
                                _identifyOverlay.Graphics.Add(identifyGraphic);
                            }
                            else
                            {
                                Graphic gr = new Graphic(ng, largePlane3DSymbol);
                                gr.Attributes["HEADING"] = heading + 180;
                                gr.Attributes["CALLSIGN"] = callsign;
                                Plane p = new Plane(gr, velocity, vert_rate, heading, last_timestamp, true, callsign);
                                planes.Add(callsign, p);
                                _graphicsOverlay.Graphics.Add(gr);

                                if (ShouldUpdateIdentifyOverlay)
                                {
                                    var identifyGraphic = new Graphic(identifyGeometry);
                                    identifyGraphic.Attributes["CALLSIGN"] = callsign;
                                    _identifyOverlay.Graphics.Add(identifyGraphic);
                                }
                            }
                        }

                        if (SelectedPlane?.callsign == callsign)
                        {
                            SelectedPlane.velocity = velocity;
                            SelectedPlane.heading = heading;
                            SelectedPlane.vert_rate = vert_rate;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        //This code is used if we use the FeatureLayer
        private async Task queryFeatures()
        {
            //sft = new ServiceFeatureTable(new Uri("https://dev0011356.esri.com/server/rest/services/Hosted/Latest_Flights_1578796112/FeatureServer/0"));
            sft = new ServiceFeatureTable(new Uri("https://services.arcgis.com/Wl7Y1m92PbjtJs5n/arcgis/rest/services/Current_Flights/FeatureServer/0"));

            MapPoint mp;
            if (center == null)
            {
                mp = new MapPoint(-117.18, 33.5556, sr);
            }
            else
            {
                mp = center;
            }
            Envelope en = new Envelope(mp, coord_tolerance, coord_tolerance);
            QueryParameters qp = new QueryParameters();
            qp.Geometry = en;
            string[] outputFields = { "*" };
            await sft.PopulateFromServiceAsync(qp, false, outputFields);
            FeatureQueryResult fqr = await sft.QueryFeaturesAsync(qp);
            bool transfer_limit = fqr.IsTransferLimitExceeded;
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            foreach (var feature in fqr)
            {
                var attributes = feature.Attributes;
                if (attributes["longitude"] != null || attributes["latitude"] != null)
                {
                    String callsign = (String)feature.Attributes["callsign"];
                    Int32 last_timestamp = 0;
                    double lon = Convert.ToDouble(attributes["longitude"]);
                    double lat = Convert.ToDouble(attributes["latitude"]);
                    double alt = 0.0;
                    if (attributes["geo_altitude"] != null)
                    {
                        alt = Convert.ToDouble(attributes["geo_altitude"]);
                    }
                    else if (attributes["baro_altitude"] != null)
                    {
                        alt = Convert.ToDouble(attributes["baro_altitude"]);
                    }

                    double velocity = 0.0;
                    double heading = 0.0;
                    double vert_rate = 0.0;
                    if (attributes["velocity"] != null)
                    {
                        velocity = Convert.ToDouble(attributes["velocity"]);
                    }
                    if (attributes["true_track"] != null)
                    {
                        heading = Convert.ToDouble(attributes["true_track"]);
                    }
                    if (attributes["vertical_rate"] != null)
                    {
                        vert_rate = Convert.ToDouble(attributes["vertical_rate"]);
                    }
                    if (attributes["time_position"] != null)
                    {
                        last_timestamp = Convert.ToInt32(attributes["time_position"]);
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
                        currPlane.graphic.Attributes["HEADING"] = heading + 180;
                        currPlane.graphic.Attributes["CALLSIGN"] = callsign;
                        currPlane.velocity = velocity;
                        currPlane.vert_rate = vert_rate;
                        currPlane.heading = heading;
                        currPlane.last_update = last_timestamp;
                    }
                    else
                    {
                        if (callsign.Length > 0 && callsign[0] == 'N')
                        {
                            Graphic gr = new Graphic(ng, smallPlane3DSymbol);
                            gr.Attributes["HEADING"] = heading;
                            gr.Attributes["CALLSIGN"] = callsign;
                            Plane p = new Plane(gr, velocity, vert_rate, heading, last_timestamp, false, callsign);
                            planes.Add(callsign, p);
                            _graphicsOverlay.Graphics.Add(gr);
                        }
                        else
                        {
                            Graphic gr = new Graphic(ng, largePlane3DSymbol);
                            gr.Attributes["HEADING"] = heading + 180;
                            gr.Attributes["CALLSIGN"] = callsign;
                            Plane p = new Plane(gr, velocity, vert_rate, heading, last_timestamp, true, callsign);
                            planes.Add(callsign, p);
                            _graphicsOverlay.Graphics.Add(gr);
                        }
                    }

                    if (SelectedPlane?.callsign == callsign)
                    {
                        SelectedPlane.velocity = velocity;
                        SelectedPlane.heading = heading;
                        SelectedPlane.vert_rate = vert_rate;
                    }
                }
            }
        }

        public async Task queryPlanes()
        {
            //This code is used if we use the FeatureLayer

            // await queryFeatures();
            await addPlanesViaAPI();
        }

        private int updateCounter = 0;

        public async void AnimatePlane(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            updateCounter++;
            if (updateCounter % (seconds_per_cleanup * updates_per_second) == 0)
            {
                List<String> planes_to_remove = new List<String>();
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                foreach (var plane in planes)
                {
                    if (unixTimestamp - plane.Value.last_update > seconds_per_cleanup)
                    {
                        _graphicsOverlay.Graphics.Remove(plane.Value.graphic);
                        _identifyOverlay.Graphics.Remove(_identifyOverlay.Graphics.Where(g => g.Attributes["CALLSIGN"].ToString() == plane.Value.callsign).First());
                        planes_to_remove.Add(plane.Key);
                    }
                }
                foreach (var callsign in planes_to_remove)
                {
                    planes.Remove(callsign);
                }
            }
            if (updateCounter % (updates_per_second * seconds_per_query) == 0)
            {
                await queryPlanes();
            }
            else
            {
                try
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
                        if (!plane.Value.big_plane)
                        {
                            plane.Value.graphic.Attributes["HEADING"] = plane.Value.heading;
                        }

                        if (ShouldUpdateIdentifyOverlay)
                        {
                            var res = _identifyOverlay.Graphics.Where(gxxxx => gxxxx.Attributes["CALLSIGN"].ToString() == plane.Value.callsign).FirstOrDefault();
                            if (res != null)
                            {
                                res.Geometry = new MapPoint(new_location[0].X, new_location[0].Y, dz, g.SpatialReference);
                            }
                        }
                        
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static async Task<string> GetSmallPlane()
        {
            await DataManager.DownloadDataItem("681d6f7694644709a7c830ec57a2d72b");
            return DataManager.GetDataFolder("681d6f7694644709a7c830ec57a2d72b", "Bristol.dae");
        }

        private static async Task<string> GetLargePlane()
        {
            await DataManager.DownloadDataItem("21274c9a36f445db912c7c31d2eb78b7");
            return DataManager.GetDataFolder("21274c9a36f445db912c7c31d2eb78b7", "Boeing787", "B_787_8.dae");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void UpdateProperty(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
#if __MOBILE__
        public static async Task<Dictionary<string, string>> GetRequest(string callSign)
        {
            string URL = $"{"https://flightaware.com/live/flight/"}{callSign}";

            var dict = new Dictionary<string, string>();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.GetAsync(URL))
                    {
                        using (HttpContent content = response.Content)
                        {
                            // Main Data
                            string data = await content.ReadAsStringAsync();
                            string iata1 = data.Split("iata\":\"")[1];
                            string iata2 = data.Split("iata\":\"")[2];

                            // Departure airport
                            var airport1Strings = iata1.Split("\",\"friendlyName\":\"");
                            string code1 = airport1Strings[0];
                            string friendly1 = airport1Strings[1].Split("\",\"")[0];

                            dict.Add("DepAirportCode", code1);
                            dict.Add("DepAirportName", friendly1);

                            // Arrival airport
                            var airport2Strings = iata2.Split("\",\"friendlyName\":\"");
                            string code2 = airport2Strings[0];
                            string friendly2 = airport2Strings[1].Split("\",\"")[0];

                            dict.Add("ArrAirportCode", code2);
                            dict.Add("ArrAirportName", friendly2);

                            // Times
                            string takeoffTime = iata2.Split("\"takeoffTimes\":{\"scheduled\":")[1].Split(",\"est")[0];
                            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                            dtDateTime = dtDateTime.AddSeconds(int.Parse(takeoffTime)).ToLocalTime();
                            string takeOffTimeEnglish = dtDateTime.ToString();

                            dict.Add("TakeoffTime", takeOffTimeEnglish);
                            dict.Add("TakeoffUnix", takeoffTime);

                            string landingTime = iata2.Split("\"landingTimes\":{\"scheduled\":")[1].Split(",\"est")[0];
                            dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                            dtDateTime = dtDateTime.AddSeconds(int.Parse(landingTime)).ToLocalTime();
                            string landTimeEnglish = dtDateTime.ToString();

                            string estimatedLandingTime = iata2.Split("\"landingTimes\":{\"scheduled\":")[1].Split(",\"estimated\":")[1].Split(",")[0];
                            int difference = (int.Parse(estimatedLandingTime) - int.Parse(landingTime)) / 60;
                            dict.Add("MinutesLate", difference.ToString());

                            dict.Add("LandingTime", landTimeEnglish);
                            dict.Add("LandingUnix", landingTime);

                            // Airplane and airline info
                            string airlineName = data.Split("<title>")[1].Split(" (")[0];
                            if(airlineName.Contains("Live Flight Tracking and History"))
                            {
                                airlineName = "N/A";
                            }
                            dict.Add("AirlineName", airlineName);

                            string aircraftType = data.Split("\"aircraftTypeFriendly\":\"")[1].Split("\"")[0];
                            dict.Add("AirplaneType", aircraftType);

                            return dict;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
#endif
    }
}
