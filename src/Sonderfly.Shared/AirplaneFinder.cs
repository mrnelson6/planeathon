// Copyright 2020 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

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

namespace Sonderfly
{
    public class Plane : INotifyPropertyChanged
    {
        private string _callSign;
        private double _velocity;
        private double _verticalRateOfChange;
        private double _heading;

        public Graphic Graphic { get; }

        public string CallSign
        {
            get => _callSign;
            set
            {
                if (_callSign != value)
                {
                    _callSign = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CallSign)));
                }
            }
        }

        public double Velocity
        {
            get => _velocity;
            set
            {
                if (_velocity != value)
                {
                    _velocity = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Velocity)));
                }
            }
        }

        public double VerticalRateOfChange
        {
            get => _verticalRateOfChange;
            set
            {
                if (_verticalRateOfChange != value)
                {
                    _verticalRateOfChange = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VerticalRateOfChange)));
                }
            }
        }

        public double Heading
        {
            get => _heading;
            set
            {
                if (_heading != value)
                {
                    _heading = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Heading)));
                }
            }
        }

        public int LastUpdate;
        public bool BigPlane;

        public Plane(Graphic graphic, double velocity, double verticalRateOfChange, double heading, int lastUpdate,
            bool bigPlane, string callSign)
        {
            Graphic = graphic;
            Velocity = velocity;
            VerticalRateOfChange = verticalRateOfChange;
            Heading = heading;
            LastUpdate = lastUpdate;
            BigPlane = bigPlane;
            CallSign = callSign;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    class AirplaneFinder : INotifyPropertyChanged
    {
        private Timer _animationTimer;
        public Dictionary<string, Plane> Planes = new Dictionary<string, Plane>();
        private ModelSceneSymbol _smallPlane3DSymbol;
        private ModelSceneSymbol _largePlane3DSymbol;
        private ServiceFeatureTable _serviceTable;

        private static readonly HttpClient WebClient = new HttpClient();

        // Overlay for testing plane graphics.
        private readonly GraphicsOverlay _graphicsOverlay;
        private readonly int _updatesPerSecond = 30;
        private readonly int _secondsPerQuery = 10;
        private readonly int _smallPlaneSize = 60;
        private readonly int _largePlaneSize = 20;
        private readonly int _secondsPerCleanup = 30;
        public double CoordinateTolerance = 0.5;

        public bool ShouldUpdateIdentifyOverlay = true;

        // Overlay for identification purposes only.
        public GraphicsOverlay IdentifyOverlay;

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

        public MapPoint Center { get; set; }

        public AirplaneFinder(GraphicsOverlay go, GraphicsOverlay identifyOverlay)
        {
            _graphicsOverlay = go;
            IdentifyOverlay = identifyOverlay;
        }

        public async void SetupScene()
        {
            string licenseKey = "";
            ArcGISRuntimeEnvironment.SetLicense(licenseKey);

            _smallPlane3DSymbol = await ModelSceneSymbol.CreateAsync(new Uri(await GetSmallPlane()), _smallPlaneSize);
            _largePlane3DSymbol = await ModelSceneSymbol.CreateAsync(new Uri(await GetLargePlane()), _largePlaneSize);

            _graphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
            IdentifyOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
            SimpleRenderer renderer3D = new SimpleRenderer();
            RendererSceneProperties renderProperties = renderer3D.SceneProperties;
            // Use expressions to keep the renderer properties updated as parameters of the rendered object
            renderProperties.HeadingExpression = "[HEADING]";
            renderProperties.PitchExpression = "[PITCH]";
            renderProperties.RollExpression = "[ROLL]";
            // Apply the renderer to the scene view's overlay
            _graphicsOverlay.Renderer = renderer3D;

            IdentifyOverlay.Opacity = 0.01;
            IdentifyOverlay.Renderer = new SimpleRenderer(new SimpleMarkerSceneSymbol(
                SimpleMarkerSceneSymbolStyle.Sphere,
                System.Drawing.Color.Red, 1000, 1000, 1000, SceneSymbolAnchorPosition.Center));
            await QueryPlanes();
            _animationTimer = new Timer(1000 / _updatesPerSecond)
            {
                Enabled = true,
                AutoReset = true
            };

            // Move the plane every time the timer expires
            _animationTimer.Elapsed += AnimatePlane;

            _animationTimer.Start();
        }

        private async Task AddPlanesViaApi()
        {
            try
            {
                MapPoint mp;
                if (Center == null)
                {
                    mp = new MapPoint(-117.18, 33.5556, SpatialReferences.Wgs84);
                }
                else
                {
                    mp = Center;
                }

                Envelope en = new Envelope(mp, CoordinateTolerance, CoordinateTolerance);
                double xMax = en.XMax;
                double yMax = en.YMax;
                double xMin = en.XMin;
                double yMin = en.YMin;

                string call = "https://matt9678:Window430@opensky-network.org/api/states/all?lamin=" + yMin +
                              "&lomin=" + xMin + "&lamax=" + yMax + "&lomax=" + xMax;
                var response = await WebClient.GetAsync(call);
                var responseString = await response.Content.ReadAsStringAsync();
                int time_message_sent = Convert.ToInt32(responseString.Substring(8, 10));
                int unixTimestamp = (int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string states = responseString.Substring(30, responseString.Length - 30);
                string[] elements = states.Split('[');

                for (int i = 0; i < elements.Length; i++)
                {
                    string[] attributes = elements[i].Split(',');
                    if (attributes[5] != "null" || attributes[5] != "null")
                    {
                        string callsign = attributes[1].Substring(1, attributes[1].Length - 2);
                        int last_timestamp = 0;
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

                        MapPoint g = new MapPoint(lon, lat, alt, SpatialReferences.Wgs84);
                        int time_difference = unixTimestamp - last_timestamp;

                        List<MapPoint> lmp = new List<MapPoint> {g};
                        IReadOnlyList<MapPoint> new_location = GeometryEngine.MoveGeodetic(lmp,
                            velocity * time_difference, LinearUnits.Meters, heading, AngularUnits.Degrees,
                            GeodeticCurveType.Geodesic);
                        double dz = new_location[0].Z + (vert_rate * time_difference);
                        MapPoint ng = new MapPoint(new_location[0].X, new_location[0].Y, dz, g.SpatialReference);
                        MapPoint identifyGeometry =
                            new MapPoint(new_location[0].X, new_location[0].Y, dz, g.SpatialReference);

                        if (Planes.ContainsKey(callsign))
                        {
                            Plane currPlane = Planes[callsign];
                            currPlane.Graphic.Geometry = ng;
                            currPlane.Graphic.Attributes["HEADING"] = heading + 180;
                            currPlane.Graphic.Attributes["CALLSIGN"] = callsign;
                            currPlane.Velocity = velocity;
                            currPlane.VerticalRateOfChange = vert_rate;
                            currPlane.Heading = heading;
                            currPlane.LastUpdate = last_timestamp;

                            if (ShouldUpdateIdentifyOverlay)
                            {
                                var res = IdentifyOverlay.Graphics.Where(gxxx =>
                                    gxxx.Attributes["CALLSIGN"].ToString() == callsign);

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
                                Graphic gr = new Graphic(ng, _smallPlane3DSymbol);
                                gr.Attributes["HEADING"] = heading;
                                gr.Attributes["CALLSIGN"] = callsign;
                                Plane p = new Plane(gr, velocity, vert_rate, heading, last_timestamp, false, callsign);
                                Planes.Add(callsign, p);
                                _graphicsOverlay.Graphics.Add(gr);

                                var identifyGraphic = new Graphic(identifyGeometry);
                                identifyGraphic.Attributes["CALLSIGN"] = callsign;
                                IdentifyOverlay.Graphics.Add(identifyGraphic);
                            }
                            else
                            {
                                Graphic gr = new Graphic(ng, _largePlane3DSymbol);
                                gr.Attributes["HEADING"] = heading + 180;
                                gr.Attributes["CALLSIGN"] = callsign;
                                Plane p = new Plane(gr, velocity, vert_rate, heading, last_timestamp, true, callsign);
                                Planes.Add(callsign, p);
                                _graphicsOverlay.Graphics.Add(gr);

                                if (ShouldUpdateIdentifyOverlay)
                                {
                                    var identifyGraphic = new Graphic(identifyGeometry);
                                    identifyGraphic.Attributes["CALLSIGN"] = callsign;
                                    IdentifyOverlay.Graphics.Add(identifyGraphic);
                                }
                            }
                        }

                        if (SelectedPlane?.CallSign == callsign)
                        {
                            SelectedPlane.Velocity = velocity;
                            SelectedPlane.Heading = heading;
                            SelectedPlane.VerticalRateOfChange = vert_rate;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task QueryFeatures()
        {
            //ServiceTable = new ServiceFeatureTable(new Uri("https://dev0011356.esri.com/server/rest/services/Hosted/Latest_Flights_1578796112/FeatureServer/0"));
            _serviceTable = new ServiceFeatureTable(new Uri(
                "https://services.arcgis.com/Wl7Y1m92PbjtJs5n/arcgis/rest/services/Current_Flights/FeatureServer/0"));

            MapPoint mp;
            if (Center == null)
            {
                mp = new MapPoint(-117.18, 33.5556, SpatialReferences.Wgs84);
            }
            else
            {
                mp = Center;
            }

            Envelope en = new Envelope(mp, CoordinateTolerance, CoordinateTolerance);
            QueryParameters qp = new QueryParameters {Geometry = en};
            string[] outputFields = {"*"};
            await _serviceTable.PopulateFromServiceAsync(qp, false, outputFields);
            FeatureQueryResult fqr = await _serviceTable.QueryFeaturesAsync(qp);
            // TODO - handle transfer limit exceeded
            bool transferLimit = fqr.IsTransferLimitExceeded;
            int unixTimestamp = (int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            foreach (var feature in fqr)
            {
                var attributes = feature.Attributes;
                if (attributes["longitude"] != null || attributes["latitude"] != null)
                {
                    string callsign = (string) feature.Attributes["callsign"];
                    int last_timestamp = 0;
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

                    MapPoint g = new MapPoint(lon, lat, alt, SpatialReferences.Wgs84);
                    int time_difference = unixTimestamp - last_timestamp;

                    List<MapPoint> lmp = new List<MapPoint> {g};
                    IReadOnlyList<MapPoint> new_location = GeometryEngine.MoveGeodetic(lmp, velocity * time_difference,
                        LinearUnits.Meters, heading, AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                    double dz = new_location[0].Z + (vert_rate * time_difference);
                    MapPoint ng = new MapPoint(new_location[0].X, new_location[0].Y, dz, g.SpatialReference);

                    if (Planes.ContainsKey(callsign))
                    {
                        Plane currPlane = Planes[callsign];
                        currPlane.Graphic.Geometry = ng;
                        currPlane.Graphic.Attributes["HEADING"] = heading + 180;
                        currPlane.Graphic.Attributes["CALLSIGN"] = callsign;
                        currPlane.Velocity = velocity;
                        currPlane.VerticalRateOfChange = vert_rate;
                        currPlane.Heading = heading;
                        currPlane.LastUpdate = last_timestamp;
                    }
                    else
                    {
                        if (callsign.Length > 0 && callsign[0] == 'N')
                        {
                            Graphic gr = new Graphic(ng, _smallPlane3DSymbol);
                            gr.Attributes["HEADING"] = heading;
                            gr.Attributes["CALLSIGN"] = callsign;
                            Plane p = new Plane(gr, velocity, vert_rate, heading, last_timestamp, false, callsign);
                            Planes.Add(callsign, p);
                            _graphicsOverlay.Graphics.Add(gr);
                        }
                        else
                        {
                            Graphic gr = new Graphic(ng, _largePlane3DSymbol);
                            gr.Attributes["HEADING"] = heading + 180;
                            gr.Attributes["CALLSIGN"] = callsign;
                            Plane p = new Plane(gr, velocity, vert_rate, heading, last_timestamp, true, callsign);
                            Planes.Add(callsign, p);
                            _graphicsOverlay.Graphics.Add(gr);
                        }
                    }

                    if (SelectedPlane?.CallSign == callsign)
                    {
                        SelectedPlane.Velocity = velocity;
                        SelectedPlane.Heading = heading;
                        SelectedPlane.VerticalRateOfChange = vert_rate;
                    }
                }
            }
        }

        public async Task QueryPlanes()
        {
            //This code is used if we use the FeatureLayer

            // await queryFeatures();
            await AddPlanesViaApi();
        }

        private int updateCounter = 0;

        public async void AnimatePlane(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            updateCounter++;
            if (updateCounter % (_secondsPerCleanup * _updatesPerSecond) == 0)
            {
                List<string> planes_to_remove = new List<string>();
                int unixTimestamp = (int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                foreach (var plane in Planes)
                {
                    if (unixTimestamp - plane.Value.LastUpdate > _secondsPerCleanup)
                    {
                        _graphicsOverlay.Graphics.Remove(plane.Value.Graphic);
                        IdentifyOverlay.Graphics.Remove(IdentifyOverlay.Graphics
                            .Where(g => g.Attributes["CALLSIGN"].ToString() == plane.Value.CallSign).First());
                        planes_to_remove.Add(plane.Key);
                    }
                }

                foreach (var callsign in planes_to_remove)
                {
                    Planes.Remove(callsign);
                }
            }

            if (updateCounter % (_updatesPerSecond * _secondsPerQuery) == 0)
            {
                await QueryPlanes();
            }
            else
            {
                try
                {
                    foreach (var plane in Planes)
                    {
                        MapPoint g = (MapPoint) plane.Value.Graphic.Geometry;
                        List<MapPoint> lmp = new List<MapPoint> {g};
                        IReadOnlyList<MapPoint> new_location = GeometryEngine.MoveGeodetic(lmp,
                            plane.Value.Velocity / _updatesPerSecond, LinearUnits.Meters, plane.Value.Heading,
                            AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                        double dz = new_location[0].Z + (plane.Value.VerticalRateOfChange / _updatesPerSecond);
                        MapPoint ng = new MapPoint(new_location[0].X, new_location[0].Y, dz, g.SpatialReference);
                        plane.Value.Graphic.Geometry = ng;
                        if (!plane.Value.BigPlane)
                        {
                            plane.Value.Graphic.Attributes["HEADING"] = plane.Value.Heading;
                        }

                        if (ShouldUpdateIdentifyOverlay)
                        {
                            var res = IdentifyOverlay.Graphics
                                .Where(gxxxx => gxxxx.Attributes["CALLSIGN"].ToString() == plane.Value.CallSign)
                                .FirstOrDefault();
                            if (res != null)
                            {
                                res.Geometry = new MapPoint(new_location[0].X, new_location[0].Y, dz,
                                    g.SpatialReference);
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
            string url = $"https://flightaware.com/live/flight/{callSign}";

            var dict = new Dictionary<string, string>();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.GetAsync(url))
                    {
                        using (HttpContent content = response.Content)
                        {
                            // Main Data
                            var data = await content.ReadAsStringAsync();
                            var iata1 = data.Split("iata\":\"")[1];
                            var iata2 = data.Split("iata\":\"")[2];

                            // Departure airport
                            var airport1Strings = iata1.Split("\",\"friendlyName\":\"");
                            var code1 = airport1Strings[0];
                            var friendly1 = airport1Strings[1].Split("\",\"")[0];

                            dict.Add("DepAirportCode", code1);
                            dict.Add("DepAirportName", friendly1);

                            // Arrival airport
                            var airport2Strings = iata2.Split("\",\"friendlyName\":\"");
                            var code2 = airport2Strings[0];
                            var friendly2 = airport2Strings[1].Split("\",\"")[0];

                            dict.Add("ArrAirportCode", code2);
                            dict.Add("ArrAirportName", friendly2);

                            // Times
                            var takeoffTime = iata2.Split("\"takeoffTimes\":{\"scheduled\":")[1].Split(",\"est")[0];
                            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                            dtDateTime = dtDateTime.AddSeconds(int.Parse(takeoffTime)).ToLocalTime();
                            var takeOffTimeEnglish = dtDateTime.ToString();

                            dict.Add("TakeoffTime", takeOffTimeEnglish);
                            dict.Add("TakeoffUnix", takeoffTime);

                            var landingTime = iata2.Split("\"landingTimes\":{\"scheduled\":")[1].Split(",\"est")[0];
                            dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                            dtDateTime = dtDateTime.AddSeconds(int.Parse(landingTime)).ToLocalTime();
                            var landTimeEnglish = dtDateTime.ToString();

                            var estimatedLandingTime =
                                iata2.Split("\"landingTimes\":{\"scheduled\":")[1].Split(",\"estimated\":")[1]
                                    .Split(",")[0];
                            int difference = (int.Parse(estimatedLandingTime) - int.Parse(landingTime)) / 60;
                            dict.Add("MinutesLate", difference.ToString());

                            dict.Add("LandingTime", landTimeEnglish);
                            dict.Add("LandingUnix", landingTime);

                            // Airplane and airline info
                            var airlineName = data.Split("<title>")[1].Split(" (")[0];
                            if (airlineName.Contains("Live Flight Tracking and History"))
                            {
                                airlineName = "N/A";
                            }

                            dict.Add("AirlineName", airlineName);

                            var aircraftType = data.Split("\"aircraftTypeFriendly\":\"")[1].Split("\"")[0];
                            dict.Add("AirplaneType", aircraftType);

                            return dict;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
#endif
    }
}