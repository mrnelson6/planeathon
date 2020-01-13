using Esri.ArcGISRuntime.Geometry;
using Foundation;
using SharedAirplaneFinder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UIKit;

namespace PlaneARViewer.BottomSheet
{
    public class FlightInfoViewControllerDataSource : UITableViewDataSource
    {
        // Header cell
        // flight status (on time, departure, estimated arrival)
        // aircraft type & airline
        // Speed, rate of ascent/descent, altitude

        private ThreePartViewCell _flightHeaderViewCell;
        private TwoPartViewCell _flightStatusViewCell;
        private UITableViewCell _flightAltitudeViewCell;
        private UITableViewCell _flightSpeedViewCell;
        private UITableViewCell _flightAscentViewCell;
        private UITableViewCell _airlineViewCell;
        private UITableViewCell _airframeViewCell;
        private UITableViewCell _callSignViewCell;
        public ActionViewCell _actionViewCell;

        private UITableView _tableView;

        private bool _hasFullDetails = false;

        public Plane _currentPlane;
        private Dictionary<string, string> _randomFacts;

        public FlightInfoViewControllerDataSource(UITableView tv) : base()
        {
            _flightHeaderViewCell = new ThreePartViewCell();
            _flightStatusViewCell = new TwoPartViewCell();
            _flightAltitudeViewCell = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_flightAltitudeViewCell));
            _flightSpeedViewCell = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_flightSpeedViewCell));
            _flightAscentViewCell = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_flightAscentViewCell));
            _airframeViewCell = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_airframeViewCell));
            _airlineViewCell = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_airlineViewCell));
            _callSignViewCell = new UITableViewCell(UITableViewCellStyle.Subtitle, nameof(_callSignViewCell));

            _flightAltitudeViewCell.DetailTextLabel.Text = "Current Altitude";
            _flightSpeedViewCell.DetailTextLabel.Text = "Current Speed";
            _airlineViewCell.DetailTextLabel.Text = "Airline";
            _airframeViewCell.DetailTextLabel.Text = "Aircraft";
            _callSignViewCell.DetailTextLabel.Text = "Flight #";

            _actionViewCell = new ActionViewCell();

            _tableView = tv;
        }

        public void SetNewPlane(Plane plane)
        {
            InvokeOnMainThread(async () =>
            {
                if (_currentPlane != null)
                {
                    _currentPlane.PropertyChanged -= Plane_PropertyChanged;
                }

                _currentPlane = plane;

                RefreshFromCurrentPlane();

                _randomFacts = await AirplaneFinder.GetRequest(plane.callsign);

                RefreshFromRandomFacts();

                _currentPlane.PropertyChanged += Plane_PropertyChanged;
            });
        }

        private void RefreshFromCurrentPlane()
        {
            string movementName = _currentPlane.vert_rate > 0 ? "Ascent" : "Descent";

            _flightAscentViewCell.DetailTextLabel.Text = $"Rate of {movementName}";
            _flightAscentViewCell.TextLabel.Text = $"{Math.Abs(_currentPlane.vert_rate)} ft/sec";
            _flightAltitudeViewCell.TextLabel.Text = $"{((MapPoint)_currentPlane.graphic.Geometry).Z} ft";
            _flightSpeedViewCell.TextLabel.Text = $"{_currentPlane.velocity} MPH";
            _callSignViewCell.TextLabel.Text = _currentPlane.callsign;
        }

        private void RefreshFromRandomFacts()
        {
            if (_randomFacts == null)
            {
                _hasFullDetails = false;
                return;
            }

            _hasFullDetails = true;

            string DepAirportCode = _randomFacts[nameof(DepAirportCode)];
            string DepAirportName = _randomFacts["DepAirportName"];
            string ArrAirportCode = _randomFacts["ArrAirportCode"];
            string ArrAirportName = _randomFacts["ArrAirportName"];
            string AirplaneType = _randomFacts["AirplaneType"];
            string AirlineName = _randomFacts["AirlineName"];
            string takeofftime = _randomFacts["TakeoffTime"];
            string landingTime = _randomFacts["LandingTime"];

            int minutesLate;

            if (int.TryParse(_randomFacts["MinutesLate"], out minutesLate))
            {
                if (minutesLate > 5)
                {
                    _flightStatusViewCell.Update(UIColor.SystemRedColor, takeofftime, "Departure Time", landingTime, "Estimated Arrival");
                }
                else
                {
                    _flightStatusViewCell.Update(UIColor.SystemGreenColor, "1:23 PM", "Departure Time", "4:53 PM", "Estimated Arrival");
                }
            }

            _airframeViewCell.TextLabel.Text = AirplaneType;
            _airlineViewCell.TextLabel.Text = AirlineName;

            _flightHeaderViewCell.Update(DepAirportCode, DepAirportName, "->", "", ArrAirportCode, ArrAirportName);

            _tableView.ReloadData();
        }

        private void Plane_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InvokeOnMainThread(RefreshFromCurrentPlane);
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            if (indexPath.Section == 0)
            {
                if (_hasFullDetails)
                {
                    switch (indexPath.Row)
                    {
                        case 0: return _callSignViewCell;
                        case 1: return _flightHeaderViewCell;
                        case 2: return _flightStatusViewCell;
                        case 3: return _flightSpeedViewCell;
                        case 4: return _flightAltitudeViewCell;
                        case 5: return _flightAscentViewCell;
                        case 6: return _airlineViewCell;
                        case 7: return _airframeViewCell;
                        case 8: return _actionViewCell;
                    }
                }
                else
                {
                    switch (indexPath.Row)
                    {
                        case 0: return _callSignViewCell;
                        case 1: return _flightSpeedViewCell;
                        case 2: return _flightAltitudeViewCell;
                        case 3: return _flightAscentViewCell;
                        case 4: return _actionViewCell;
                    }
                }
                
            }
            return new UITableViewCell();
        }

        public override nint RowsInSection(UITableView tableView, nint section)
        {
            if (_hasFullDetails)
            {
                return 9;
            }
            return 5;
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }
    }

    internal class ThreePartViewCell : UITableViewCell
    {
        private UILabel _valLabelOne;
        private UILabel _valLabelThree;
        private UILabel _valNameOne;
        private UILabel _valNameThree;

        private UILabel _valLabelTwo;
        private UILabel _valNameTwo;
        private const int _margin = 16;

        public const int Height = 32 + 28 + 2 * 16;

        public void Update(string fieldValOne, string fieldNameOne, string fieldValTwo, string fieldNameTwo, string fieldValThree, string fieldNameThree)
        {
            _valLabelOne.Text = fieldValOne;
            _valLabelTwo.Text = fieldValTwo;
            _valLabelThree.Text = fieldValThree;

            _valNameOne.Text = fieldNameOne;
            _valNameTwo.Text = fieldNameTwo;
            _valNameThree.Text = fieldNameThree;
        }

        public ThreePartViewCell() : base()
        {
            _valLabelTwo = new UILabel();
            _valLabelTwo.TranslatesAutoresizingMaskIntoConstraints = false;
            _valLabelTwo.TextAlignment = UITextAlignment.Center;
            _valLabelTwo.Font = _valLabelTwo.Font.WithSize(32);

            _valNameTwo = new UILabel();
            _valNameTwo.TranslatesAutoresizingMaskIntoConstraints = false;
            _valNameTwo.TextAlignment = UITextAlignment.Center;
            _valNameTwo.Font = _valNameTwo.Font.WithSize(18);
            _valNameTwo.AdjustsFontSizeToFitWidth = true;

            _valLabelOne = new UILabel();
            _valLabelOne.TranslatesAutoresizingMaskIntoConstraints = false;
            _valLabelOne.TextAlignment = UITextAlignment.Center;
            _valLabelOne.Font = _valLabelOne.Font.WithSize(32);

            _valLabelThree = new UILabel();
            _valLabelThree.TranslatesAutoresizingMaskIntoConstraints = false;
            _valLabelThree.TextAlignment = UITextAlignment.Center;
            _valLabelThree.Font = _valLabelThree.Font.WithSize(32);

            _valNameOne = new UILabel();
            _valNameOne.TranslatesAutoresizingMaskIntoConstraints = false;
            _valNameOne.TextAlignment = UITextAlignment.Center;
            _valNameOne.Font = _valNameOne.Font.WithSize(18);
            _valNameOne.AdjustsFontSizeToFitWidth = true;

            _valNameThree = new UILabel();
            _valNameThree.TranslatesAutoresizingMaskIntoConstraints = false;
            _valNameThree.TextAlignment = UITextAlignment.Center;
            _valNameThree.Font = _valNameThree.Font.WithSize(18);
            _valNameThree.AdjustsFontSizeToFitWidth = true;

            this.ContentView.AddSubviews(_valLabelOne, _valLabelThree, _valLabelTwo, _valNameTwo, _valNameOne, _valNameThree);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _valLabelOne.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, _margin),
                _valLabelOne.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor, _margin),
                _valLabelOne.TrailingAnchor.ConstraintEqualTo(_valLabelTwo.LeadingAnchor, -_margin),
                _valLabelOne.HeightAnchor.ConstraintEqualTo(32),
                _valNameOne.HeightAnchor.ConstraintEqualTo(28),
                _valNameTwo.TopAnchor.ConstraintEqualTo(_valNameOne.TopAnchor),
                _valNameTwo.BottomAnchor.ConstraintEqualTo(_valNameOne.BottomAnchor),
                _valNameTwo.LeadingAnchor.ConstraintEqualTo(_valLabelTwo.LeadingAnchor),
                _valNameTwo.TrailingAnchor.ConstraintEqualTo(_valLabelTwo.TrailingAnchor),
                _valLabelTwo.CenterXAnchor.ConstraintEqualTo(ContentView.CenterXAnchor),
                _valLabelTwo.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor),
                _valLabelTwo.BottomAnchor.ConstraintEqualTo(_valLabelOne.BottomAnchor),
                _valLabelThree.TopAnchor.ConstraintEqualTo(_valLabelOne.TopAnchor),
                _valLabelThree.BottomAnchor.ConstraintEqualTo(_valLabelOne.BottomAnchor),
                _valLabelThree.LeadingAnchor.ConstraintEqualTo(_valLabelTwo.TrailingAnchor, _margin),
                _valLabelThree.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -_margin),
                _valNameOne.LeadingAnchor.ConstraintEqualTo(_valLabelOne.LeadingAnchor),
                _valNameOne.TrailingAnchor.ConstraintEqualTo(_valLabelOne.TrailingAnchor),
                _valNameOne.TopAnchor.ConstraintEqualTo(_valLabelOne.BottomAnchor),
                _valNameOne.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -_margin),
                _valNameThree.LeadingAnchor.ConstraintEqualTo(_valLabelThree.LeadingAnchor),
                _valNameThree.TrailingAnchor.ConstraintEqualTo(_valLabelThree.TrailingAnchor),
                _valNameThree.TopAnchor.ConstraintEqualTo(_valNameOne.TopAnchor),
                _valNameThree.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -_margin)
            });
        }
    }

    public class ActionViewCell : UITableViewCell
    {
        public UIButton _MapButton;
        public UIButton _FlyoverButton;

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

    internal class TwoPartViewCell : UITableViewCell
    {
        private UILabel _firstValueLabel;
        private UILabel _firstDescriptionLabel;
        private UILabel _secondValueLabel;
        private UILabel _secondDescriptionLabel;

        private const int _margin = 16;

        public const int Height = 32 + 28 + 2 * 16;

        public void Update(UIColor foregroundColor, string firstVal, string firstDesc, string secondVal, string secondDesc)
        {
            _firstValueLabel.TextColor = foregroundColor;
            _firstDescriptionLabel.TextColor = foregroundColor;
            _secondValueLabel.TextColor = foregroundColor;
            _secondDescriptionLabel.TextColor = foregroundColor;

            _firstValueLabel.Text = firstVal;
            _firstDescriptionLabel.Text = firstDesc;
            _secondValueLabel.Text = secondVal;
            _secondDescriptionLabel.Text = secondDesc;
        }

        public TwoPartViewCell() : base()
        {
            _firstValueLabel = new UILabel();
            _firstValueLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            _firstValueLabel.TextAlignment = UITextAlignment.Center;
            _firstValueLabel.Font = _firstValueLabel.Font.WithSize(32);
            _firstValueLabel.AdjustsFontSizeToFitWidth = true;

            _secondValueLabel = new UILabel();
            _secondValueLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            _secondValueLabel.TextAlignment = UITextAlignment.Center;
            _secondValueLabel.Font = _secondValueLabel.Font.WithSize(32);
            _secondValueLabel.AdjustsFontSizeToFitWidth = true;

            _firstDescriptionLabel = new UILabel();
            _firstDescriptionLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            _firstDescriptionLabel.TextAlignment = UITextAlignment.Center;
            _firstDescriptionLabel.Font = _firstDescriptionLabel.Font.WithSize(18);
            _firstDescriptionLabel.AdjustsFontSizeToFitWidth = true;

            _secondDescriptionLabel = new UILabel();
            _secondDescriptionLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            _secondDescriptionLabel.TextAlignment = UITextAlignment.Center;
            _secondDescriptionLabel.Font = _secondDescriptionLabel.Font.WithSize(18);
            _secondDescriptionLabel.AdjustsFontSizeToFitWidth = true;

            this.ContentView.AddSubviews(_firstValueLabel, _secondValueLabel, _firstDescriptionLabel, _secondDescriptionLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _firstValueLabel.HeightAnchor.ConstraintEqualTo(32),
                _firstDescriptionLabel.HeightAnchor.ConstraintEqualTo(28),
                _firstValueLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, _margin),
                _firstDescriptionLabel.LeadingAnchor.ConstraintEqualTo(_firstValueLabel.LeadingAnchor),
                _firstValueLabel.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor, _margin),
                _firstDescriptionLabel.TopAnchor.ConstraintEqualTo(_firstValueLabel.BottomAnchor, _margin),
                _secondValueLabel.TopAnchor.ConstraintEqualTo(_firstValueLabel.TopAnchor),
                _secondValueLabel.HeightAnchor.ConstraintEqualTo(_firstValueLabel.HeightAnchor),
                _secondDescriptionLabel.TopAnchor.ConstraintEqualTo(_firstDescriptionLabel.TopAnchor),
                _secondDescriptionLabel.HeightAnchor.ConstraintEqualTo(_firstDescriptionLabel.HeightAnchor),
                _secondValueLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -_margin),
                _secondDescriptionLabel.TrailingAnchor.ConstraintEqualTo(_secondValueLabel.TrailingAnchor),
                _firstValueLabel.TrailingAnchor.ConstraintEqualTo(ContentView.CenterXAnchor, -_margin),
                _firstDescriptionLabel.TrailingAnchor.ConstraintEqualTo(_firstValueLabel.TrailingAnchor),
                _secondValueLabel.LeadingAnchor.ConstraintEqualTo(ContentView.CenterXAnchor, _margin),
                _secondDescriptionLabel.LeadingAnchor.ConstraintEqualTo(_secondValueLabel.LeadingAnchor),
                _secondDescriptionLabel.BottomAnchor.ConstraintEqualTo(_firstDescriptionLabel.BottomAnchor),
                _firstDescriptionLabel.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -_margin)
            });
        }
    }
}