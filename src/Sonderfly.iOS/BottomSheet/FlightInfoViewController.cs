// Copyright 2020 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using UIKit;

namespace Sonderfly.iOS.BottomSheet
{
    internal sealed class FlightInfoViewController : UITableViewController
    {
        private AirplaneFinder _airplaneFinder;
        private readonly FlightInfoViewControllerDataSource _dataSource;

        // TODO - these shouldn't be public
        public UIButton MapButton;
        public UIButton FlyoverButton;

        public FlightInfoViewController()
        {
            _dataSource = new FlightInfoViewControllerDataSource(TableView);
            TableView.DataSource = _dataSource;
            TableView.AllowsSelection = false;
            TableView.ScrollEnabled = true;

            MapButton = _dataSource.ActionViewCell.MapButton;
            FlyoverButton = _dataSource.ActionViewCell.FlyoverButton;

            View.Layer.CornerRadius = 16;
            View.Layer.Opacity = 0.9f;
        }

        public Plane GetPlane()
        {
            return _dataSource?.CurrentPlane;
        }

        public void AssociateAirplaneFinder(AirplaneFinder airplaneFinder)
        {
            _airplaneFinder = airplaneFinder;

            _airplaneFinder.PropertyChanged += AirplaneFinderPropertyChanged;
        }

        private void AirplaneFinderPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AirplaneFinder.SelectedPlane))
            {
                _dataSource.SetNewPlane(_airplaneFinder.SelectedPlane);
            }
        }

        public int GetViewHeight() => 64 * 3 + ThreePartViewCell.Height;
    }
}