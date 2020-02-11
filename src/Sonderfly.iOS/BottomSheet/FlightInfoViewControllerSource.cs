// Copyright 2020 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Esri.ArcGISRuntime.Geometry;
using Foundation;
using UIKit;

namespace Sonderfly.iOS.BottomSheet
{
    public class FlightInfoViewControllerDataSource : UITableViewDataSource
    {
        // Header cell
        // flight status (on time, departure, estimated arrival)
        // aircraft type & airline
        // Speed, rate of ascent/descent, altitude

        private readonly ThreePartViewCell _flightHeaderViewCell;
        private readonly TwoPartViewCell _flightStatusViewCell;
        private readonly UITableViewCell _flightAltitudeViewCell;
        private readonly UITableViewCell _flightSpeedViewCell;
        private readonly UITableViewCell _flightAscentViewCell;
        private readonly UITableViewCell _airlineViewCell;
        private readonly UITableViewCell _airframeViewCell;
        private readonly UITableViewCell _callSignViewCell;
        // TODO - this shouldn't be public
        public ActionViewCell ActionViewCell;

        private readonly UITableView _tableView;

        private bool _hasFullDetails;

        public Plane CurrentPlane { get; set; }
        private Dictionary<string, string> _flightFacts;

        public FlightInfoViewControllerDataSource(UITableView tv)
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

            ActionViewCell = new ActionViewCell();

            _tableView = tv;
        }

        public void SetNewPlane(Plane plane)
        {
            InvokeOnMainThread(async () =>
            {
                if (CurrentPlane != null)
                {
                    CurrentPlane.PropertyChanged -= Plane_PropertyChanged;
                }

                CurrentPlane = plane;

                RefreshFromCurrentPlane();

                _flightFacts = await AirplaneFinder.GetRequest(plane.CallSign);

                RefreshFromRandomFacts();

                CurrentPlane.PropertyChanged += Plane_PropertyChanged;
            });
        }

        private void RefreshFromCurrentPlane()
        {
            string movementName = CurrentPlane.VerticalRateOfChange > 0 ? "Ascent" : "Descent";

            _flightAscentViewCell.DetailTextLabel.Text = $"Rate of {movementName}";
            _flightAscentViewCell.TextLabel.Text = $"{Math.Abs(CurrentPlane.VerticalRateOfChange)} ft/sec";
            _flightAltitudeViewCell.TextLabel.Text = $"{((MapPoint)CurrentPlane.Graphic.Geometry).Z} ft";
            _flightSpeedViewCell.TextLabel.Text = $"{CurrentPlane.Velocity} MPH";
            _callSignViewCell.TextLabel.Text = CurrentPlane.CallSign;
        }

        private void RefreshFromRandomFacts()
        {
            if (_flightFacts == null)
            {
                _hasFullDetails = false;
                _tableView.ReloadData();
                return;
            }

            _hasFullDetails = true;

            string depAirportCode = _flightFacts["DepAirportCode"];
            string depAirportName = _flightFacts["DepAirportName"];
            string arrAirportCode = _flightFacts["ArrAirportCode"];
            string arrAirportName = _flightFacts["ArrAirportName"];
            string airplaneType = _flightFacts["AirplaneType"];
            string airlineName = _flightFacts["AirlineName"];
            string takeOffTime = _flightFacts["TakeoffTime"];
            string landingTime = _flightFacts["LandingTime"];

            if (int.TryParse(_flightFacts["MinutesLate"], out var minutesLate))
            {
                if (minutesLate > 5)
                {
                    _flightStatusViewCell.Update(UIColor.SystemRedColor, takeOffTime, "Departure Time", landingTime, "Estimated Arrival");
                }
                else
                {
                    _flightStatusViewCell.Update(UIColor.SystemGreenColor, takeOffTime, "Departure Time", landingTime, "Estimated Arrival");
                }
            }

            _airframeViewCell.TextLabel.Text = airplaneType;
            _airlineViewCell.TextLabel.Text = airlineName;

            _flightHeaderViewCell.Update(depAirportCode, depAirportName, arrAirportCode, arrAirportName);

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
                        case 8: return ActionViewCell;
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
                        case 4: return ActionViewCell;
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

    internal sealed class ThreePartViewCell : UITableViewCell
    {
        private readonly UILabel _valLabelOne;
        private readonly UILabel _valLabelThree;
        private readonly UILabel _valNameOne;
        private readonly UILabel _valNameThree;

        private const int Margin = 16;

        public const int Height = 32 + 28 + 2 * 16;

        public void Update(string fieldValOne, string fieldNameOne, string fieldValThree, string fieldNameThree)
        {
            _valLabelOne.Text = fieldValOne;
            _valLabelThree.Text = fieldValThree;

            _valNameOne.Text = fieldNameOne;
            _valNameThree.Text = fieldNameThree;
        }

        public ThreePartViewCell()
        {
            var centerPlaneImage = new UIImageView
            {
                Image = UIImage.GetSystemImage("airplane"),
                TranslatesAutoresizingMaskIntoConstraints = false,
                ContentMode = UIViewContentMode.ScaleAspectFill
            };

            _valLabelOne = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false, TextAlignment = UITextAlignment.Center
            };
            _valLabelOne.Font = _valLabelOne.Font.WithSize(32);

            _valLabelThree = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false, TextAlignment = UITextAlignment.Center
            };
            _valLabelThree.Font = _valLabelThree.Font.WithSize(32);

            _valNameOne = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false, TextAlignment = UITextAlignment.Center, AdjustsFontSizeToFitWidth = true
            };
            _valNameOne.Font = _valNameOne.Font.WithSize(18);

            _valNameThree = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false, TextAlignment = UITextAlignment.Center, AdjustsFontSizeToFitWidth = true
            };
            _valNameThree.Font = _valNameThree.Font.WithSize(18);

            ContentView.AddSubviews(_valLabelOne, _valLabelThree, centerPlaneImage, _valNameOne, _valNameThree);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _valLabelOne.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, Margin),
                _valLabelOne.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor, Margin),
                _valLabelOne.TrailingAnchor.ConstraintEqualTo(centerPlaneImage.LeadingAnchor, -Margin),
                _valLabelOne.HeightAnchor.ConstraintEqualTo(32),
                _valNameOne.HeightAnchor.ConstraintEqualTo(28),
                centerPlaneImage.CenterXAnchor.ConstraintEqualTo(ContentView.CenterXAnchor),
                centerPlaneImage.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor),
                centerPlaneImage.LeadingAnchor.ConstraintEqualTo(_valLabelOne.TrailingAnchor, Margin),
                centerPlaneImage.TrailingAnchor.ConstraintEqualTo(_valNameThree.LeadingAnchor, -Margin),
                centerPlaneImage.HeightAnchor.ConstraintEqualTo(48),
                _valLabelThree.TopAnchor.ConstraintEqualTo(_valLabelOne.TopAnchor),
                _valLabelThree.BottomAnchor.ConstraintEqualTo(_valLabelOne.BottomAnchor),
                _valLabelThree.LeadingAnchor.ConstraintEqualTo(centerPlaneImage.TrailingAnchor, Margin),
                _valLabelThree.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -Margin),
                _valNameOne.LeadingAnchor.ConstraintEqualTo(_valLabelOne.LeadingAnchor),
                _valNameOne.TrailingAnchor.ConstraintEqualTo(_valLabelOne.TrailingAnchor),
                _valNameOne.TopAnchor.ConstraintEqualTo(_valLabelOne.BottomAnchor),
                _valNameOne.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -Margin),
                _valNameThree.LeadingAnchor.ConstraintEqualTo(_valLabelThree.LeadingAnchor),
                _valNameThree.TrailingAnchor.ConstraintEqualTo(_valLabelThree.TrailingAnchor),
                _valNameThree.TopAnchor.ConstraintEqualTo(_valNameOne.TopAnchor),
                _valNameThree.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -Margin)
            });
        }
    }

    public sealed class ActionViewCell : UITableViewCell
    {
        // TODO - these shouldn't be public
        public UIButton MapButton;
        public UIButton FlyoverButton;

        private const int Margin = 16;

        public ActionViewCell()
        {
            MapButton = new UIButton {TranslatesAutoresizingMaskIntoConstraints = false, BackgroundColor = TintColor};
            MapButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            MapButton.SetTitleColor(UIColor.LightGray, UIControlState.Focused);
            MapButton.SetTitle("View on Map", UIControlState.Normal);

            FlyoverButton = new UIButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false, BackgroundColor = TintColor
            };
            FlyoverButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            FlyoverButton.SetTitleColor(UIColor.LightGray, UIControlState.Focused);
            FlyoverButton.SetTitle("View from plane", UIControlState.Normal);

            FlyoverButton.Layer.CornerRadius = 8;
            MapButton.Layer.CornerRadius = 8;

            ContentView.AddSubviews(MapButton, FlyoverButton);

            NSLayoutConstraint.ActivateConstraints(new[]{
                MapButton.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, Margin),
                MapButton.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor, Margin),
                MapButton.TrailingAnchor.ConstraintEqualTo(ContentView.CenterXAnchor, - (Margin / 2.0f)),
                MapButton.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -Margin),
                MapButton.HeightAnchor.ConstraintEqualTo(32),
                FlyoverButton.LeadingAnchor.ConstraintEqualTo(MapButton.TrailingAnchor, Margin),
                FlyoverButton.TopAnchor.ConstraintEqualTo(MapButton.TopAnchor),
                FlyoverButton.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -Margin),
                FlyoverButton.BottomAnchor.ConstraintEqualTo(MapButton.BottomAnchor)
            });
        }
    }

    internal sealed class TwoPartViewCell : UITableViewCell
    {
        private readonly UILabel _firstValueLabel;
        private readonly UILabel _firstDescriptionLabel;
        private readonly UILabel _secondValueLabel;
        private readonly UILabel _secondDescriptionLabel;

        private const int Margin = 16;

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

        public TwoPartViewCell()
        {
            _firstValueLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false, TextAlignment = UITextAlignment.Center, AdjustsFontSizeToFitWidth = true
            };
            _firstValueLabel.Font = _firstValueLabel.Font.WithSize(32);

            _secondValueLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false, TextAlignment = UITextAlignment.Center, AdjustsFontSizeToFitWidth = true
            };
            _secondValueLabel.Font = _secondValueLabel.Font.WithSize(32);

            _firstDescriptionLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false, TextAlignment = UITextAlignment.Center, AdjustsFontSizeToFitWidth = true
            };
            _firstDescriptionLabel.Font = _firstDescriptionLabel.Font.WithSize(18);

            _secondDescriptionLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false, TextAlignment = UITextAlignment.Center, AdjustsFontSizeToFitWidth = true
            };
            _secondDescriptionLabel.Font = _secondDescriptionLabel.Font.WithSize(18);

            ContentView.AddSubviews(_firstValueLabel, _secondValueLabel, _firstDescriptionLabel, _secondDescriptionLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _firstValueLabel.HeightAnchor.ConstraintEqualTo(32),
                _firstDescriptionLabel.HeightAnchor.ConstraintEqualTo(28),
                _firstValueLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, Margin),
                _firstDescriptionLabel.LeadingAnchor.ConstraintEqualTo(_firstValueLabel.LeadingAnchor),
                _firstValueLabel.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor, Margin),
                _firstDescriptionLabel.TopAnchor.ConstraintEqualTo(_firstValueLabel.BottomAnchor, Margin),
                _secondValueLabel.TopAnchor.ConstraintEqualTo(_firstValueLabel.TopAnchor),
                _secondValueLabel.HeightAnchor.ConstraintEqualTo(_firstValueLabel.HeightAnchor),
                _secondDescriptionLabel.TopAnchor.ConstraintEqualTo(_firstDescriptionLabel.TopAnchor),
                _secondDescriptionLabel.HeightAnchor.ConstraintEqualTo(_firstDescriptionLabel.HeightAnchor),
                _secondValueLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -Margin),
                _secondDescriptionLabel.TrailingAnchor.ConstraintEqualTo(_secondValueLabel.TrailingAnchor),
                _firstValueLabel.TrailingAnchor.ConstraintEqualTo(ContentView.CenterXAnchor, -Margin),
                _firstDescriptionLabel.TrailingAnchor.ConstraintEqualTo(_firstValueLabel.TrailingAnchor),
                _secondValueLabel.LeadingAnchor.ConstraintEqualTo(ContentView.CenterXAnchor, Margin),
                _secondDescriptionLabel.LeadingAnchor.ConstraintEqualTo(_secondValueLabel.LeadingAnchor),
                _secondDescriptionLabel.BottomAnchor.ConstraintEqualTo(_firstDescriptionLabel.BottomAnchor),
                _firstDescriptionLabel.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -Margin)
            });
        }
    }
}