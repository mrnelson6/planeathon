// Copyright 2020 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using UIKit;

namespace Sonderfly.iOS
{
    internal class FlightInfoViewController : UITableViewController
    {
        private AirplaneFinder _airplaneFinder;
        private FlightInfoViewControllerDataSource _dataSource;

        public UIButton mapButton;
        public UIButton flyoverButton;

        public FlightInfoViewController() : base()
        {
            _dataSource = new FlightInfoViewControllerDataSource(TableView);
            TableView.DataSource = _dataSource;
            TableView.AllowsSelection = false;
            TableView.ScrollEnabled = true;

            mapButton = _dataSource._actionViewCell._MapButton;
            flyoverButton = _dataSource._actionViewCell._FlyoverButton;

            View.Layer.CornerRadius = 16;
            View.Layer.Opacity = 0.9f;
        }

        public Plane GetPlane()
        {
            return _dataSource?._currentPlane;
        }

        public void AssociateAirplaneFinder(AirplaneFinder airplaneFinder)
        {
            _airplaneFinder = airplaneFinder;

            _airplaneFinder.PropertyChanged += _airplaneFinder_PropertyChanged;
        }

        private void _airplaneFinder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AirplaneFinder.SelectedPlane))
            {
                _dataSource.SetNewPlane(_airplaneFinder.SelectedPlane);
            }
        }

        public int GetViewHeight() => 64 * 3 + ThreePartViewCell.Height;
    }
}