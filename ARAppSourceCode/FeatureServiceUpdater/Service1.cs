using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using SharedAirplaneFinder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FeatureServiceUpdater
{
    public partial class PlaneUpdaterService : ServiceBase
    {
        private ServiceFeatureTable _table;
        private Timer _timer;

        double xMax;
        double xMin;
        double yMax;
        double yMin;

        private Guid _receiverId = Guid.NewGuid();

        public PlaneUpdaterService()
        {
            InitializeComponent();
        }

        public void Start()
        {
            OnStart(null);
        }

        protected override async void OnStart(string[] args)
        {
            try
            {
                _table = new ServiceFeatureTable(new Uri("https://services2.arcgis.com/ZQgQTuoyBrtmoGdP/arcgis/rest/services/AR_Airplanes/FeatureServer/0"));
                _table.LoadAsync().Wait();

                MapPoint mp = new MapPoint(-117.18, 33.5556, SpatialReferences.Wgs84);

                Envelope en = new Envelope(mp, 100, 100);
                xMax = en.XMax;
                yMax = en.YMax;
                xMin = en.XMin;
                yMin = en.YMin;

                _timer = new Timer(10000);
                _timer.Elapsed += _timer_Elapsed;
                _timer.AutoReset = false;
                _timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            var planes = AirplaneFinder.GetPlanes(xMin, yMin, xMax, yMax).Result;

            var newFeatures = new List<Feature>(planes.Count());

            foreach(var plane in planes)
            {
                var feature = _table.CreateFeature();
                feature.Geometry = plane.graphic.Geometry;
                feature.Attributes["CallSign"] = plane.callsign;
                feature.Attributes["Velocity"] = plane.velocity;
                feature.Attributes["VerticalRate"] = plane.vert_rate;
                feature.Attributes["Heading"] = plane.heading.ToString(); // to-do: fix service definition
                feature.Attributes["TimeOfRecord"] = DateTime.Now;
                feature.Attributes["ReceiverId"] = _receiverId;

                newFeatures.Add(feature);
            }
            _table.AddFeaturesAsync(newFeatures).Wait();
            _table.ApplyEditsAsync().Wait();
            _timer.Start();
        }

        protected override void OnStop()
        {
            _timer.Stop();
        }
    }
}
