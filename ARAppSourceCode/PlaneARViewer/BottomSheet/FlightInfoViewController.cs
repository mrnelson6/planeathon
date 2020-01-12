using System;
using UIKit;

namespace PlaneARViewer.BottomSheet
{
    internal class FlightInfoViewController : UITableViewController
    {
        public FlightInfoViewController() : base()
        {
            TableView.DataSource = new FlightInfoViewControllerDataSource();
            TableView.AllowsSelection = false;
            TableView.ScrollEnabled = false;

            View.Layer.CornerRadius = 16;
            View.Layer.Opacity = 0.9f;
        }

        public int GetViewHeight() => 64 * 3 + FlightHeaderViewCell.Height;
    }
}
