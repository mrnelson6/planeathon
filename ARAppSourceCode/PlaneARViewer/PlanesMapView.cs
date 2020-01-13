using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Linq;
using UIKit;

namespace PlaneARViewer
{
    public partial class PlanesMapView : UIViewController
    {
        // UI objects.
        private MapView mapView;
        public MapPoint center;

        public PlanesMapView(IntPtr handle) : base(handle)
        {
        }

        public PlanesMapView(MapPoint mp)
        {
            center = mp;
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
            GraphicsOverlay _graphicsOverlay = new GraphicsOverlay();
            GraphicsOverlay _graphicsOverlay2 = new GraphicsOverlay();
            SharedAirplaneFinder.AirplaneFinder sc = new SharedAirplaneFinder.AirplaneFinder(_graphicsOverlay, _graphicsOverlay2);
            sc.center = center;
            sc.setupScene();

            SpatialReference sr = SpatialReferences.Wgs84;
            Envelope en = new Envelope(center, sc.coord_tolerance, sc.coord_tolerance);
            Viewpoint vp = new Viewpoint(en);
            Map newMap = new Map(Basemap.CreateImageryWithLabels());


            //ServiceFeatureTable sft = new ServiceFeatureTable(new Uri("https://services.arcgis.com/Wl7Y1m92PbjtJs5n/arcgis/rest/services/Current_Flights/FeatureServer/0"));
            //FeatureLayer fl = new FeatureLayer(sft);
            ////FeatureLayer fl = new FeatureLayer(sc.sft);
            //await fl.LoadAsync();


            ArcGISPortal portal = await ArcGISPortal.CreateAsync(new Uri("https://runtime.maps.arcgis.com/"));
            PortalItem mapItem = await PortalItem.CreateAsync(portal, "b61d8171db5d4f9ea7c7f6c492949d3a");

            //https://services.arcgis.com/Wl7Y1m92PbjtJs5n/arcgis/rest/services/Current_Flights/FeatureServer

            FeatureLayer fl = new FeatureLayer(mapItem, 0);
            await fl.LoadAsync();

            newMap.OperationalLayers.Add(fl);
            newMap.InitialViewpoint = vp;
            mapView.Map = newMap;
        }

        public override void LoadView()
        {
            View = new UIView { BackgroundColor = UIColor.White };

            mapView = new MapView();
            mapView.TranslatesAutoresizingMaskIntoConstraints = false;



            View.AddSubviews(mapView);//, toolbar);//, _helpLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                mapView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
            });
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
        }
    }
}