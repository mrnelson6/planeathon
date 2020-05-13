using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Geocoding;

namespace SharedAirplaneFinder
{
    public class CitySearchViewModel : INotifyPropertyChanged
    {
        private LocatorTask _geocoder;
        private GeocodeParameters _geocodeParams;

        private string _query;
        private bool _isBusy;

        public string Query
        {
            get => _query;
            set
            {
                if (_query != value)
                {
                    _query = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Query)));
                    _ = RefreshQuery();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBusy)));
                }
            }
        }

        public IEnumerable<string> Results
        {
            get
            {
                if (_geocodeResults?.Any() ?? false)
                {
                    return _geocodeResults.Select(res => res.Label);
                }
                else
                {
                    return _prebakedResults.Keys;
                }
            }
        }

        // List of names of cities and corresponding scene layer item IDs
        private Dictionary<string, string> _prebakedResults;

        private IEnumerable<GeocodeResult> _geocodeResults;

        public CitySearchViewModel()
        {
            // Items from AGOL
            _prebakedResults = new Dictionary<string, string>
            {
                {"San Francisco","d3344ba99c3f4efaa909ccfbcc052ed5" },
                {"Montreal", "f4b4881270124343a4cc2f847f86f54c" },
                //{"Rotterdam", "399c14ff7f0a4fa0b58ae2c5b4e993fd" },
                //{"Zurich", "65696aefd99445bf86bf682a7f2530c6" },
                {"Dallas", "995118ec37824b99a9607f9913aa53da" },
                //{"St Charles", "bb0911c964404abea49f7620d3a592f4" }
            };

            _ = RefreshQuery();

            _geocoder = new LocatorTask(new Uri("https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer"));

            _geocodeParams = new GeocodeParameters();
            _geocodeParams.Categories.Add("City");
        }

        private async Task RefreshQuery()
        {
            IsBusy = true;
            // If no query, use pre-baked results
            if (string.IsNullOrWhiteSpace(Query))
            {
                _geocodeResults = null;
            }
            else
            {
                if (_geocoder.LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded)
                {
                    await _geocoder.LoadAsync();
                }
                _geocodeResults = await _geocoder.GeocodeAsync(Query, _geocodeParams);
            }


            // Set results
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Results)));
            IsBusy = false;
        }

        public async Task<Scene> SceneForSelection(int rowId)
        {
            // Set basemap
            Scene scene = new Scene(Basemap.CreateImagery());

            // Add elevation source
            scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));

            // Add scene layer if available
            var potentialKey = Results.ElementAt(rowId);
            MapPoint sceneCenter = null;

            if (_prebakedResults.ContainsKey(potentialKey))
            {
                ArcGISSceneLayer sceneLayer = new ArcGISSceneLayer(new Uri($"https://www.arcgis.com/home/item.html?id={_prebakedResults[potentialKey]}"));
                scene.OperationalLayers.Add(sceneLayer);

                try
                {
                    await sceneLayer.LoadAsync();
                    sceneCenter = sceneLayer.FullExtent.GetCenter();
                }
                catch (Exception ex)
                {
                    // Ignore
                }
            }

            if (sceneCenter == null)
            {
                sceneCenter = _geocodeResults.ElementAt(rowId).DisplayLocation;
            }

            scene.InitialViewpoint = new Viewpoint(GeometryEngine.BufferGeodetic(sceneCenter, 400, LinearUnits.Meters));

            return scene;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
