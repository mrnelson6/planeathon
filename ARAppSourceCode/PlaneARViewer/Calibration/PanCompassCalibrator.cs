using System;
using System.Linq;
using Foundation;
using UIKit;

namespace PlaneARViewer.Calibration
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

            if (touches.Count == 2 && touches.ElementAt(0) is UITouch firstTouch)
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
