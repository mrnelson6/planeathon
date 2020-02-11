// Copyright 2020 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using UIKit;

namespace Sonderfly.iOS
{
    public class PlanesMapView : UIViewController
    {
        // UI objects.
        private MapView _mapView;
        public MapPoint Center;

        public PlanesMapView(MapPoint mp)
        {
            Center = mp;
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
            GraphicsOverlay graphicsOverlay = new GraphicsOverlay();
            GraphicsOverlay graphicsOverlay2 = new GraphicsOverlay();
            AirplaneFinder sc = new AirplaneFinder(graphicsOverlay, graphicsOverlay2) {Center = Center};
            sc.SetupScene();

            Envelope en = new Envelope(Center, sc.CoordinateTolerance, sc.CoordinateTolerance);
            Viewpoint vp = new Viewpoint(en);
            Map newMap = new Map(Basemap.CreateImageryWithLabels());

            ArcGISPortal portal = await ArcGISPortal.CreateAsync(new Uri("https://runtime.maps.arcgis.com/"));
            PortalItem mapItem = await PortalItem.CreateAsync(portal, "b61d8171db5d4f9ea7c7f6c492949d3a");
            FeatureLayer fl = new FeatureLayer(mapItem, 0);
            await fl.LoadAsync();

            //This code is used if we use the FeatureLayer
            //FeatureLayer fl = new FeatureLayer(sc.sft);
            //await fl.LoadAsync();

            //TODO add code to select airplane that was used to enter this view

            newMap.OperationalLayers.Add(fl);
            newMap.InitialViewpoint = vp;

            _mapView.Map = newMap;
        }

        public override void LoadView()
        {
            View = new UIView { BackgroundColor = UIColor.White };

            _mapView = new MapView {TranslatesAutoresizingMaskIntoConstraints = false};

            View.AddSubviews(_mapView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _mapView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
            });
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (NavigationController != null)
            {
                NavigationController.NavigationBarHidden = false;
            }
        }
    }
}