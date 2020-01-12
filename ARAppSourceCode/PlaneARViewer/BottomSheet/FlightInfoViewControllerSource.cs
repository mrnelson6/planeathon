using System;
using System.ComponentModel;
using Foundation;
using SharedAirplaneFinder;
using UIKit;

namespace PlaneARViewer.BottomSheet
{
    public class FlightInfoViewControllerDataSource : UITableViewDataSource
    {
        private FlightHeaderViewCell _flightHeaderViewCell;
        private UITableViewCell _flightSpeedViewCell;
        private UITableViewCell _flightVerticalSpeedViewCell;
        private UITableViewCell _aircraftModelInfo;
        private UITableViewCell _aircraftAgeInfo;

        private ActionViewCell actionViewCell = new ActionViewCell();

        private Plane _currentPlane;

        public FlightInfoViewControllerDataSource() : base()
        {
            _flightHeaderViewCell = new FlightHeaderViewCell("AOC", "PGB");

            _flightSpeedViewCell = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_flightSpeedViewCell));
            _flightVerticalSpeedViewCell = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_flightVerticalSpeedViewCell));
            _aircraftModelInfo = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_aircraftModelInfo));
            _aircraftAgeInfo = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_aircraftAgeInfo));

            _flightSpeedViewCell.TextLabel.Text = "120 MPH";
            _flightSpeedViewCell.DetailTextLabel.Text = "Ground Speed";

            _flightVerticalSpeedViewCell.TextLabel.Text = "6000 feet/minute";
            _flightVerticalSpeedViewCell.DetailTextLabel.Text = "Rate of descent";

            _aircraftModelInfo.TextLabel.Text = "Boeing 787 MAX 8";
            _aircraftModelInfo.DetailTextLabel.Text = "Aircraft Make & Model";

            _aircraftAgeInfo.TextLabel.Text = "1 Year 8 Months";
            _aircraftAgeInfo.DetailTextLabel.Text = "Aircraft years in service";
        }

        public void Update(Plane plane)
        {
            InvokeOnMainThread(() =>
            {
                if (_currentPlane != null)
                {
                    _currentPlane.PropertyChanged -= Plane_PropertyChanged;
                }

                _currentPlane = plane;

                _currentPlane.PropertyChanged += Plane_PropertyChanged;

                UpdateVelocity(_currentPlane.velocity);
                UpdateVertRate(plane.vert_rate);


                if (plane.big_plane)
                {
                    _aircraftModelInfo.TextLabel.Text = "Big Plane";
                }
                else
                {
                    _aircraftModelInfo.TextLabel.Text = "Lil' Plane";
                }
                _flightHeaderViewCell.Update(plane);
            });
        }

        private void UpdateVelocity(double newVelocity)
        {
            _flightSpeedViewCell.TextLabel.Text = $"{newVelocity} Kilometers per Hour";
        }

        private void UpdateVertRate(double vertRate)
        {
            _flightVerticalSpeedViewCell.TextLabel.Text = $"{Math.Abs(vertRate)} Meters per Second";

            if (vertRate > 0)
            {
                _flightVerticalSpeedViewCell.DetailTextLabel.Text = "Rate of descent";
            }
            else
            {
                _flightVerticalSpeedViewCell.DetailTextLabel.Text = "Rate of ascent";
            }
        }

        private void Plane_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Plane.velocity):
                    InvokeOnMainThread(() => UpdateVelocity(_currentPlane.velocity));
                    break;
                case nameof(Plane.vert_rate):
                    InvokeOnMainThread(() => UpdateVertRate(_currentPlane.vert_rate));
                    break;
            }
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            switch (indexPath.Section)
            {
                case 0:
                    return _flightHeaderViewCell;
                case 1:
                    switch (indexPath.Row)
                    {
                        case 0:
                            return _flightSpeedViewCell;
                        case 1:
                            return _flightVerticalSpeedViewCell;
                    }
                    break;
                case 2:
                    return actionViewCell;
            }
            return null;
        }

        public override nint RowsInSection(UITableView tableView, nint section)
        {
            switch (section)
            {
                case 0:
                    return 1;
                case 1:
                    return 2;
                case 2:
                    return 1;
            }
            return 0;
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            return 3;
        }
    }

    internal class FlightHeaderViewCell : UITableViewCell
    {
        private UILabel _originCodeLabel;
        private UILabel _destinationLabel;
        private UILabel _originNameView;
        private UILabel _destinationNameView;

        private UILabel _arrowLabel;
        private const int _margin = 16;

        public const int Height = 32 + 28 + 2 * 16;

        public void Update(Plane plane)
        {
            _originCodeLabel.Text = "ORG";
            _destinationLabel.Text = "DST";
            _originNameView.Text = "Origin Airport";
            _destinationNameView.Text = "Destination Airport";
        }

        public FlightHeaderViewCell(string origin, string destination) : base()
        {
            _arrowLabel = new UILabel();
            _arrowLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            _arrowLabel.Text = "->";
            _arrowLabel.TextAlignment = UITextAlignment.Center;
            _arrowLabel.Font = _arrowLabel.Font.WithSize(32);

            _originCodeLabel = new UILabel();
            _originCodeLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            _originCodeLabel.Text = origin;
            _originCodeLabel.TextAlignment = UITextAlignment.Center;
            _originCodeLabel.Font = _originCodeLabel.Font.WithSize(32);

            _destinationLabel = new UILabel();
            _destinationLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            _destinationLabel.Text = destination;
            _destinationLabel.TextAlignment = UITextAlignment.Center;
            _destinationLabel.Font = _destinationLabel.Font.WithSize(32);

            _originNameView = new UILabel();
            _originNameView.TranslatesAutoresizingMaskIntoConstraints = false;
            _originNameView.Text = "Atlanta Intl. Airport";
            _originNameView.TextAlignment = UITextAlignment.Center;
            _originNameView.Font = _originNameView.Font.WithSize(18);
            _originNameView.AdjustsFontSizeToFitWidth = true;

            _destinationNameView = new UILabel();
            _destinationNameView.TranslatesAutoresizingMaskIntoConstraints = false;
            _destinationNameView.Text = "Ontario, Intl. Airport";
            _destinationNameView.TextAlignment = UITextAlignment.Center;
            _destinationNameView.Font = _destinationNameView.Font.WithSize(18);
            _destinationNameView.AdjustsFontSizeToFitWidth = true;

            this.ContentView.AddSubviews(_originCodeLabel, _destinationLabel, _arrowLabel, _originNameView, _destinationNameView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _originCodeLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, _margin),
                _originCodeLabel.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor, _margin),
                _originCodeLabel.TrailingAnchor.ConstraintEqualTo(_arrowLabel.LeadingAnchor, -_margin),
                _originCodeLabel.HeightAnchor.ConstraintEqualTo(32),
                _originNameView.HeightAnchor.ConstraintEqualTo(28),
                _arrowLabel.CenterXAnchor.ConstraintEqualTo(ContentView.CenterXAnchor),
                _arrowLabel.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor),
                _arrowLabel.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor),
                _destinationLabel.TopAnchor.ConstraintEqualTo(_originCodeLabel.TopAnchor),
                _destinationLabel.BottomAnchor.ConstraintEqualTo(_originCodeLabel.BottomAnchor),
                _destinationLabel.LeadingAnchor.ConstraintEqualTo(_arrowLabel.TrailingAnchor, _margin),
                _destinationLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -_margin),
                _originNameView.LeadingAnchor.ConstraintEqualTo(_originCodeLabel.LeadingAnchor),
                _originNameView.TrailingAnchor.ConstraintEqualTo(_originCodeLabel.TrailingAnchor),
                _originNameView.TopAnchor.ConstraintEqualTo(_originCodeLabel.BottomAnchor),
                _originNameView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -_margin),
                _destinationNameView.LeadingAnchor.ConstraintEqualTo(_destinationLabel.LeadingAnchor),
                _destinationNameView.TrailingAnchor.ConstraintEqualTo(_destinationLabel.TrailingAnchor),
                _destinationNameView.TopAnchor.ConstraintEqualTo(_originNameView.TopAnchor),
                _destinationNameView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -_margin)
            });
        }
    }

    internal class FlightSpeedViewCell : UITableViewCell
    {
        public FlightSpeedViewCell() : base()
        {
            TextLabel.Text = $"Flight speed: ";
        }
    }

    internal class FlightVerticalSpeedViewCell : UITableViewCell
    {
        public FlightVerticalSpeedViewCell() : base()
        {
            TextLabel.Text = "Vertical speed: too low";
        }
    }

    internal class ActionViewCell : UITableViewCell
    {
        private UIButton _MapButton;
        private UIButton _FlyoverButton;

        private const int margin = 16;
        private const int controlHeight = 32;

        public ActionViewCell() : base()
        {
            _MapButton = new UIButton();
            _MapButton.TranslatesAutoresizingMaskIntoConstraints = false;
            _MapButton.BackgroundColor = TintColor;
            _MapButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            _MapButton.SetTitleColor(UIColor.LightGray, UIControlState.Focused);
            _MapButton.SetTitle("View on Map", UIControlState.Normal);

            _FlyoverButton = new UIButton();
            _FlyoverButton.TranslatesAutoresizingMaskIntoConstraints = false;
            _FlyoverButton.BackgroundColor = TintColor;
            _FlyoverButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            _FlyoverButton.SetTitleColor(UIColor.LightGray, UIControlState.Focused);
            _FlyoverButton.SetTitle("View from plane", UIControlState.Normal);

            _FlyoverButton.Layer.CornerRadius = 8;
            _MapButton.Layer.CornerRadius = 8;

            ContentView.AddSubviews(_MapButton, _FlyoverButton);

            NSLayoutConstraint.ActivateConstraints(new[]{
                _MapButton.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, margin),
                _MapButton.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor, margin),
                _MapButton.TrailingAnchor.ConstraintEqualTo(ContentView.CenterXAnchor, - (margin / 2.0f)),
                _MapButton.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -margin),
                _MapButton.HeightAnchor.ConstraintEqualTo(32),
                _FlyoverButton.LeadingAnchor.ConstraintEqualTo(_MapButton.TrailingAnchor, margin),
                _FlyoverButton.TopAnchor.ConstraintEqualTo(_MapButton.TopAnchor),
                _FlyoverButton.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -margin),
                _FlyoverButton.BottomAnchor.ConstraintEqualTo(_MapButton.BottomAnchor)
            });
        }
    }
}
