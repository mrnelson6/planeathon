// Copyright 2020 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;

namespace Sonderfly.iOS.Calibration
{
    /// <summary>
    /// Wraps the built-in location data source to enable altitude adjustment.
    /// </summary>
    public class AdjustableLocationDataSource : LocationDataSource
    {
        // Track the altitude offset and raise location changed event when it is updated.
        private double _altitudeOffset;
        private double _headingValue;
        private double _manualElevation;
        private bool _useManualElevation;
        private bool _ignoreLocationUpdate;

        public bool IgnoreLocationUpdate
        {
            get => _ignoreLocationUpdate;
            set
            {
                _ignoreLocationUpdate = value;
                if (_ignoreLocationUpdate)
                {
                    _baseSource.LocationChanged -= BaseSourceLocationChanged;
                } else
                {
                    _baseSource.LocationChanged -= BaseSourceLocationChanged;
                    _baseSource.LocationChanged += BaseSourceLocationChanged;
                }
            }
        }

        public double AltitudeOffset
        {
            get => _altitudeOffset;
            set
            {
                _altitudeOffset = value;

                if (LastLocation != null)
                {
                    BaseSourceLocationChanged(_baseSource, LastLocation);
                }
            }
        }

        public double HeadingValue
        {
            get => _headingValue;
            set
            {
                _headingValue = value;
                BaseSourceHeadingChanged(_baseSource, _headingValue);
            }
        }

        public Location LastLocation { get; private set; }

        // The system's location data source.
        private readonly SystemLocationDataSource _baseSource;

        public AdjustableLocationDataSource()
        {
            _baseSource = new SystemLocationDataSource();
            _baseSource.LocationChanged += BaseSourceLocationChanged;
            _baseSource.HeadingChanged += BaseSourceHeadingChanged;
        }

        private void BaseSourceLocationChanged(object sender, Location e)
        {
            if (IgnoreLocationUpdate) return;

            // Store the last location; used to raise location changed event when only the offset is changed.
            LastLocation = e;

            // Create the offset map point.
            MapPoint newPosition;

            if (_useManualElevation)
            {
                newPosition = new MapPoint(e.Position.X, e.Position.Y, _manualElevation,
                e.Position.SpatialReference);
            }
            else
            {
                newPosition = new MapPoint(e.Position.X, e.Position.Y, e.Position.Z + AltitudeOffset,
                e.Position.SpatialReference);
            }

            // Create a new location from the map point.
            Location newLocation = new Location(newPosition, e.HorizontalAccuracy, e.Velocity, _headingValue, e.IsLastKnown);

            // Call the base UpdateLocation implementation.
            UpdateLocation(newLocation);
        }

        private void BaseSourceHeadingChanged(object sender, double e)
        {
            _baseSource.HeadingChanged -= BaseSourceHeadingChanged;
            UpdateHeading(e);
        }

        /// <summary>
        /// Set known elevation and stop updating
        /// </summary>
        /// <param name="elevation"></param>
        public void SetKnownElevation(double elevation)
        {
            _useManualElevation = true;
            _manualElevation = elevation;
            BaseSourceLocationChanged(_baseSource, LastLocation);
        }

        protected override Task OnStartAsync() => _baseSource.StartAsync();

        protected override Task OnStopAsync() => _baseSource.StopAsync();
    }
}