using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using SharedAirplaneFinder;
using UIKit;

namespace Sonderfly.iOS
{
    public class CitySearchController : UIViewController
    {
        private UISearchBar _searchBar;

        private CitySearchViewModel _vm;

        private CityResultsSource _source;

        private UITableView _tableView;

        private UIBarButtonItem _printerButton;

        public override void LoadView()
        {
            Title = "Find a city";

            _searchBar = new UISearchBar { TranslatesAutoresizingMaskIntoConstraints = false };

            _tableView = new UITableView { TranslatesAutoresizingMaskIntoConstraints = false };

            _vm = new CitySearchViewModel();

            _source = new CityResultsSource();
            _source.Results = _vm.Results.ToArray();
            _tableView.Source = _source;
            _tableView.ReloadData();

            _vm.PropertyChanged += viewmodel_propertyChanged;
            _searchBar.TextChanged += searchBar_textChanged;

            _source.TableRowSelected += _source_TableRowSelected;

            View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            _printerButton = new UIBarButtonItem(UIImage.GetSystemImage("printer"), UIBarButtonItemStyle.Plain, null);

            View.AddSubviews(_searchBar, _tableView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _searchBar.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _searchBar.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _searchBar.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _tableView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _tableView.TopAnchor.ConstraintEqualTo(_searchBar.BottomAnchor),
                _tableView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _tableView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor)
            });
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            NavigationItem.RightBarButtonItem = _printerButton;

            _printerButton.Clicked += _printerButton_Clicked;
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            _printerButton.Clicked -= _printerButton_Clicked;
        }

        private void _printerButton_Clicked(object sender, EventArgs e)
        {
            // Create the printing view controller
            var printController = UIPrintInteractionController.SharedPrintController;

            // configure print job
            var printInfo = UIPrintInfo.FromDictionary(new NSDictionary());
            printInfo.OutputType = UIPrintInfoOutputType.General;
            printInfo.JobName = "Sonderfly tabletop anchor";
            printController.PrintInfo = printInfo;

            // Set the image to be printed
            printController.PrintingItem = GetPrintImage();

            // show the dialog
            printController.PresentFromBarButtonItem(_printerButton, true, (controller, completed, error) => { });
        }

        /// <summary>
        /// Gets the anchor image, with a built-in border so it prints with the right size on a letter-sized piece of paper
        /// </summary>
        /// <returns></returns>
        private UIImage GetPrintImage()
        {
            // code inspired by https://stackoverflow.com/a/36236589/4630559
            var borderWidth = 700; // todo handle image sizing better
            UIImage printImage = UIImage.FromBundle("ARReferenceImage");

            var internalPrintVIew = new UIImageView(new CoreGraphics.CGRect(0, 0, printImage.Size.Width, printImage.Size.Height));
            var printView = new UIView(new CoreGraphics.CGRect(0, 0, printImage.Size.Width + borderWidth*2, printImage.Size.Width + borderWidth*2));
            internalPrintVIew.Image = printImage;
            internalPrintVIew.BackgroundColor = UIColor.Clear;
            internalPrintVIew.Center = new CoreGraphics.CGPoint(printView.Frame.Size.Width / 2, printView.Frame.Size.Height / 2);

            printView.BackgroundColor = UIColor.Clear;
            printView.AddSubview(internalPrintVIew);

            // from https://gist.github.com/ozzieperez/85cf1d0bad627a6ab928
            UIGraphics.BeginImageContextWithOptions(printView.Bounds.Size, false, 0);
            printView.Layer.RenderInContext(UIGraphics.GetCurrentContext());
            var img = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return img;
        }

        private async void _source_TableRowSelected(object sender, int e)
        {
            try
            {
                var scene = await _vm.SceneForSelection(e);
                NavigationController?.PushViewController(new TabletopViewController(scene), true);
            }
            catch (Exception)
            {
                // to-do log this
            }
        }

        private void viewmodel_propertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_vm.Results))
            {
                _source.Results = _vm.Results.ToArray();
                _tableView.ReloadData();
            }
        }

        private void searchBar_textChanged(object sender, UISearchBarTextChangedEventArgs e) => _vm.Query = _searchBar.Text;
    }

    public class CityResultsSource : UITableViewSource
    {
        public string[] Results { get; set; }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            UITableViewCell reusableCell = tableView.DequeueReusableCell(nameof(CityResultsSource)) ??
                new UITableViewCell(UITableViewCellStyle.Default, nameof(CityResultsSource));

            reusableCell.TextLabel.Text = Results[indexPath.Row];

            return reusableCell;
        }

        public override nint RowsInSection(UITableView tableview, nint section) => section == 0 ? Results.Length : 0;

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath) => TableRowSelected?.Invoke(this, indexPath.Row);

        public event EventHandler<int> TableRowSelected;
    }
}
