using SharedAirplaneFinder;
using UIKit;

namespace PlaneARViewer.BottomSheet
{
    internal class FlightInfoViewController : UITableViewController
    {
        private AirplaneFinder _airplaneFinder;
        private FlightInfoViewControllerDataSource _dataSource;

        public UIButton mapButton;
        public UIButton flyoverButton;

        public FlightInfoViewController() : base()
        {
            _dataSource = new FlightInfoViewControllerDataSource();
            TableView.DataSource = _dataSource;
            TableView.AllowsSelection = false;
            TableView.ScrollEnabled = false;

            mapButton = _dataSource.actionViewCell._MapButton;
            flyoverButton = _dataSource.actionViewCell._FlyoverButton;

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