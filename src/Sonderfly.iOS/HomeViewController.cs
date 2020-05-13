using System;
using AVFoundation;
using CoreLocation;
using Foundation;
using UIKit;

namespace Sonderfly.iOS
{
    public class HomeViewController : UITableViewController
    {
        public HomeViewController()
        {
            Title = "Sonderfly";
            //NavigationController.NavigationBar.PrefersLargeTitles = true;
            var source = new HomeScreenTableDataSource();
            TableView.Source = source;
            TableView.RowHeight = 300;
            TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            TableView.UserInteractionEnabled = false;

            source.TableRowSelected += Source_TableRowSelected;
        }

        private void Source_TableRowSelected(object sender, int e)
        {
            switch (e)
            {
                case 0:
                    NavigationController?.PushViewController(new ArPlaneSceneViewController(), true);
                    break;
                case 1:
                    NavigationController?.PushViewController(new CitySearchController(), true);
                    break;
                case 2:
                    break;
            }
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (CLLocationManager.Status == CLAuthorizationStatus.AuthorizedWhenInUse && AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video) == AVAuthorizationStatus.Authorized)
            {
                TableView.UserInteractionEnabled = true;
            }
            else
            {
                var locationmanager = new CLLocationManager();
                locationmanager.AuthorizationChanged += Locationmanager_AuthorizationChanged;
                locationmanager.RequestWhenInUseAuthorization();

                AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
            }
        }

        private void Locationmanager_AuthorizationChanged(object sender, CLAuthorizationChangedEventArgs e)
        {
            if (e.Status == CLAuthorizationStatus.Authorized || e.Status == CLAuthorizationStatus.AuthorizedWhenInUse)
            {
                TableView.UserInteractionEnabled = true;
                (sender as CLLocationManager).AuthorizationChanged -= Locationmanager_AuthorizationChanged;
            }
            else
            {
                (sender as CLLocationManager).RequestWhenInUseAuthorization();
            }
        }
    }

    class HomeScreenTableDataSource : UITableViewSource
    {
        private CardCell _aroundMeCell;
        private CardCell _cityCell;
        private CardCell _flightCell;

        private UITableViewCell[] _cells;

        public HomeScreenTableDataSource()
        {
            _aroundMeCell = new CardCell(UIImage.FromBundle("LiveAR"), "Planes near me", "Show planes near me in their real-world location");
            _cityCell = new CardCell(UIImage.FromBundle("Tabletop"), "Tabletop City", "Show planes above a city in tabletop");
            _flightCell = new CardCell(UIImage.FromBundle("Flyover"), "Flyover", "See a real-time view from any ongoing flight");

            _cells = new[] { _aroundMeCell, _cityCell, _flightCell };
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            return _cells[indexPath.Row];
        }

        public override nint RowsInSection(UITableView tableView, nint section)
        {
            if (section == 0)
                return _cells.Length;
            return 0;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, false);
            TableRowSelected?.Invoke(this, indexPath.Row);
        }

        public event EventHandler<int> TableRowSelected;
    }

    class CardCell : UITableViewCell
    {
        private UIImageView _backgroundImage;
        private UILabel _primaryLabel;
        private UILabel _secondaryLabel;
        private UIView _effectView;

        public CardCell(UIImage image, string primaryText, string secondaryText)
        {
            _backgroundImage = new UIImageView { TranslatesAutoresizingMaskIntoConstraints = false };
            _backgroundImage.Layer.CornerRadius = 8;
            _backgroundImage.ClipsToBounds = true;
            _backgroundImage.ContentMode = UIViewContentMode.ScaleAspectFill;

            _primaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.White,
                Font = UIFont.BoldSystemFontOfSize(36)
            };

            _secondaryLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(16)
            };

            _effectView = new UIView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                ClipsToBounds = true,
                BackgroundColor = UIColor.FromWhiteAlpha(0, 0.5f)
            };
            _effectView.Layer.CornerRadius = 8;

            _effectView.AddSubview(_primaryLabel);
            _effectView.AddSubview(_secondaryLabel);

            ContentView.AddSubview(_backgroundImage);
            ContentView.AddSubview(_effectView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _backgroundImage.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor),
                _backgroundImage.CenterXAnchor.ConstraintEqualTo(ContentView.CenterXAnchor),
                _backgroundImage.LeadingAnchor.ConstraintGreaterThanOrEqualTo(ContentView.LeadingAnchor, 16),
                _backgroundImage.TrailingAnchor.ConstraintLessThanOrEqualTo(ContentView.TrailingAnchor, -16),
                _backgroundImage.HeightAnchor.ConstraintLessThanOrEqualTo(260),
                // constraint effect view to match background image
                _effectView.LeadingAnchor.ConstraintEqualTo(_backgroundImage.LeadingAnchor),
                _effectView.TrailingAnchor.ConstraintEqualTo(_backgroundImage.TrailingAnchor),
                _effectView.BottomAnchor.ConstraintEqualTo(_backgroundImage.BottomAnchor),
                _effectView.TopAnchor.ConstraintEqualTo(_backgroundImage.TopAnchor),
                // labels
                _secondaryLabel.LeadingAnchor.ConstraintEqualTo(_effectView.LeadingAnchor, 16),
                _secondaryLabel.TrailingAnchor.ConstraintEqualTo(_effectView.TrailingAnchor, -16),
                _secondaryLabel.BottomAnchor.ConstraintEqualTo(_effectView.BottomAnchor, -16),

                _primaryLabel.LeadingAnchor.ConstraintEqualTo(_secondaryLabel.LeadingAnchor),
                _primaryLabel.BottomAnchor.ConstraintEqualTo(_secondaryLabel.TopAnchor, -8),
                _primaryLabel.TrailingAnchor.ConstraintEqualTo(_secondaryLabel.TrailingAnchor)
            });

            _primaryLabel.Text = primaryText;
            _secondaryLabel.Text = secondaryText;
            _backgroundImage.Image = image;
        }
    }
}
