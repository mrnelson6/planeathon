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
        private GraphicsOverlay _graphicsOverlay;
        public SceneView SceneView;


        public MapViewModel()
        {
            CreateNewScene();
        }

        private async void CreateNewScene()
        {
            Scene newScene = new Scene(Basemap.CreateImageryWithLabels());

            //solves timing issues
            //also useful for testing if featureservice is working correctly
            //FeatureLayer trailHeadsLayer = new FeatureLayer(new Uri("https://services.arcgis.com/Wl7Y1m92PbjtJs5n/arcgis/rest/services/Current_Flights/FeatureServer/0"));
            //await trailHeadsLayer.LoadAsync();
            //newScene.OperationalLayers.Add(trailHeadsLayer);

            ArcGISTiledElevationSource _elevationSource = new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer"));
            await _elevationSource.LoadAsync();
            Surface _elevationSurface = new Surface();
            _elevationSurface.ElevationSources.Add(_elevationSource);
            newScene.BaseSurface = _elevationSurface;

            Scene = newScene;

            _graphicsOverlay = new GraphicsOverlay();
            SceneView.GraphicsOverlays.Add(_graphicsOverlay);

            SpatialReference sr = new SpatialReference(4326);
            MapPoint mp = new MapPoint(-117.18, 34.0, 10000, sr);
            Camera cm = new Camera(mp, 0, 90, 0);
            SceneView.SetViewpointCamera(cm);

            GraphicsOverlay _graphicsOverlay2 = new GraphicsOverlay();
            SharedAirplaneFinder.AirplaneFinder sc = new SharedAirplaneFinder.AirplaneFinder(_graphicsOverlay, _graphicsOverlay2);
            sc.center = mp;
            sc.setupScene();
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
