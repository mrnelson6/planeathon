// Copyright 2020 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using System;
using System.Linq;
using Foundation;
using UIKit;

namespace Sonderfly.iOS
{
    class PanCompassCalibrationGestureRecognizer : UIPanGestureRecognizer
    {
        private AdjustableLocationDataSource _locationSource;

        public double CalibrationSpeedMultiplier = 0.1;

        public PanCompassCalibrationGestureRecognizer(AdjustableLocationDataSource locationSource)
            : base()
        {
            _locationSource = locationSource;
            CancelsTouchesInView = false;
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            base.TouchesMoved(touches, evt);

            if (evt.AllTouches.Count == 2 && touches.ElementAt(0) is UITouch firstTouch)
            {
                // move the shape
                nfloat offsetX = firstTouch.PreviousLocationInView(View).X - firstTouch.LocationInView(View).X;

                nfloat positionY = firstTouch.LocationInView(View).Y;

                nfloat totalY = View.Frame.Height;

                nfloat ratio = positionY / totalY;

                _locationSource.HeadingValue += (offsetX / ratio) * CalibrationSpeedMultiplier;
            }
        }
    }
}
