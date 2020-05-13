using System;
using System.Linq;
using ARKit;
using Esri.ArcGISRuntime.ARToolkit;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Sonderfly.iOS.BottomSheet;
using UIKit;

namespace Sonderfly.iOS
{
    public class TabletopViewController : UIViewController
    {
        private AirplaneFinder _airplaneFinder;

        // UI objects.
        private ARSceneView _arView;
        private UIButton _closeButton;
        private UISegmentedControl _placementModeSegment;

        // Overlay for testing plane graphics.
        private GraphicsOverlay _graphicsOverlay;

        // Items on ground, viewable from plane.
        private GraphicsOverlay _groundPointsOverlay;

        private Scene _displayedScene;

        private bool _isUsingImageAnchor = true;

        // Timer control enables stopping and starting frame-by-frame animation.
        private FlightInfoViewController _flightInfoVc;
        private NSLayoutConstraint[] _flightInfoVcHorizontalConstraints;
        private NSLayoutConstraint[] _flightInfoVcVerticalConstraints;

        private UIPinchGestureRecognizer _pinchRecognizer;

        private nfloat desiredTableWidth = 1;

        public TabletopViewController(Scene scene)
        {
            _displayedScene = scene;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Initialize();
        }

        /*
         * This method is used for all of the main setup code for the app.
         * Load feature layers.
         * Set users initial position.
         * ETC...
         */

        private async void Initialize()
        {
            try
            {
                // Create and add the scene.
                _graphicsOverlay = new GraphicsOverlay();
                GraphicsOverlay identifyOverlay = new GraphicsOverlay();

                _arView.GraphicsOverlays.Add(_graphicsOverlay);
                _airplaneFinder = new AirplaneFinder(_graphicsOverlay, identifyOverlay)
                {
                    Center = _displayedScene.InitialViewpoint.TargetGeometry.Extent.GetCenter()
                };
                _airplaneFinder.SetupScene();
                _flightInfoVc.AssociateAirplaneFinder(_airplaneFinder);
                // Disable scene interaction.
                _arView.InteractionOptions = new SceneViewInteractionOptions() { IsEnabled = false };

                _groundPointsOverlay = new GraphicsOverlay
                {
                    SceneProperties = { SurfacePlacement = SurfacePlacement.DrapedBillboarded },
                    IsVisible = false
                };
                _arView.GraphicsOverlays.Add(_groundPointsOverlay);

                _pinchRecognizer = new UIPinchGestureRecognizer(HandlePinch);
                _arView.AddGestureRecognizer(_pinchRecognizer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void HandlePinch(UIPinchGestureRecognizer recognizer)
        {
            var scaleAmount = recognizer.Scale;
            _arView.TranslationFactor = 2 * _arView.ClippingDistance / scaleAmount;
        }


        public override void LoadView()
        {
            View = new UIView { BackgroundColor = UIColor.White };

            _arView = new ARSceneView { TranslatesAutoresizingMaskIntoConstraints = false };
            _arView.NorthAlign = false;

            _closeButton = new UIButton
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundColor = UIColor.SecondarySystemBackgroundColor
            };
            _closeButton.Layer.CornerRadius = 8;
            _closeButton.Layer.Opacity = 0.6f;
            _closeButton.SetImage(UIImage.GetSystemImage("xmark.circle").ApplyTintColor(UIColor.White), UIControlState.Normal);

            _flightInfoVc = new FlightInfoViewController();

            AddChildViewController(_flightInfoVc);
            _flightInfoVc.View.TranslatesAutoresizingMaskIntoConstraints = false;

            _placementModeSegment = new UISegmentedControl("Shared Anchor", "Manual placement");
            _placementModeSegment.TranslatesAutoresizingMaskIntoConstraints = false;
            _placementModeSegment.BackgroundColor = UIColor.SystemBackgroundColor;
            _placementModeSegment.SelectedSegment = 0;

            View.AddSubviews(_arView, _closeButton, _placementModeSegment);//, toolbar);//, _helpLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _arView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _arView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _arView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _arView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),

                _closeButton.HeightAnchor.ConstraintEqualTo(48),
                _closeButton.WidthAnchor.ConstraintEqualTo(48),
                _closeButton.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 16),
                _closeButton.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -16),

                _placementModeSegment.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor, 16),
                _placementModeSegment.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -16),
                _placementModeSegment.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor, -16),
                _placementModeSegment.HeightAnchor.ConstraintEqualTo(40)
            });

            _flightInfoVcHorizontalConstraints = new[]
            {
                _flightInfoVc.View.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor),
                _flightInfoVc.View.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 16),
                _flightInfoVc.View.BottomAnchor.ConstraintEqualTo(_arView.SafeAreaLayoutGuide.BottomAnchor, -16),
                _flightInfoVc.View.WidthAnchor.ConstraintEqualTo(320)
            };

            _flightInfoVcVerticalConstraints = new[]
            {
                _flightInfoVc.View.BottomAnchor.ConstraintEqualTo(_arView.SafeAreaLayoutGuide.BottomAnchor, -16),
                _flightInfoVc.View.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _flightInfoVc.View.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _flightInfoVc.View.HeightAnchor.ConstraintEqualTo(_flightInfoVc.GetViewHeight())
            };
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (NavigationController != null)
            {
                NavigationController.NavigationBarHidden = true;
            }

            // Configures ARKit to look for image anchors
            if (_arView?.ARConfiguration is ARWorldTrackingConfiguration trackingConfig)
            {
                // using a printed image, width 1.75inch, height 3.125 inch
                UIImage image = UIImage.FromBundle("ARReferenceImage");
                ARReferenceImage arImage = new ARReferenceImage(image.CGImage, ImageIO.CGImagePropertyOrientation.Down, 0.04445f);

                trackingConfig.DetectionImages = new Foundation.NSSet<ARReferenceImage>(arImage);
            }
            else
            {
                // AR Toolkit implementation changed
                throw new NotImplementedException();
            }


            // Start tracking as soon as the view has been shown.
            await _arView.StartTrackingAsync();

            _arView.GeoViewTapped += ArSceneViewTapped;
            _flightInfoVc.MapButton.TouchUpInside += ShowMapView;
            _closeButton.TouchUpInside += CalibrateButtonTapped;

            _arView.ARSCNViewDidAddNode += _arView_ARSCNViewDidAddNode;
            _arView.ARSCNViewDidUpdateNode += _arView_ARSCNViewDidUpdateNode;

            _placementModeSegment.ValueChanged += _placementModeSegment_ValueChanged;
        }

        private void _placementModeSegment_ValueChanged(object sender, EventArgs e)
        {
            _isUsingImageAnchor = _placementModeSegment.SelectedSegment == 0;
        }

        

        private void displayScene()
        {
            _arView.Scene = _displayedScene;
            _displayedScene.BaseSurface.NavigationConstraint = NavigationConstraint.None;

            // Configure scene view display for real-scale AR: no space effect or atmosphere effect.
            _arView.SpaceEffect = SpaceEffect.None;
            _arView.AtmosphereEffect = AtmosphereEffect.None;

            _arView.OriginCamera = new Camera(_displayedScene.InitialViewpoint.TargetGeometry.Extent.GetCenter(), 0, 90, 0);

            if (_displayedScene.InitialViewpoint.TargetGeometry.Extent.Width > 200)
            {
                _arView.ClippingDistance = _displayedScene.InitialViewpoint.TargetGeometry.Extent.Width / 2;
            }
            else
            {
                _arView.ClippingDistance = 8000;
            }

            _arView.TranslationFactor = 2 * _arView.ClippingDistance;
        }

        /// <summary>
        /// Called when an anchor first appears
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _arView_ARSCNViewDidAddNode(object sender, ARSCNViewNodeEventArgs e)
        {
            if (e.Anchor is ARImageAnchor imageAnchor && _isUsingImageAnchor)
            {
                // TODO - could support multiple anchors
                var refImage = imageAnchor.ReferenceImage;

                var t = e.Anchor.Transform;

                // inspired by toolkit hit test & set initial Transformation by screen point
                var tm = TransformationMatrix.Create(0, 0, 0, 1, t.Column3.X, t.Column3.Y, t.Column3.Z);

                _arView.SetInitialTransformation(TransformationMatrix.Identity - tm);

                displayScene();
            }
        }

        /// <summary>
        /// Called when a previously found node (image anchor in this case) is found again and has moved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _arView_ARSCNViewDidUpdateNode(object sender, ARSCNViewNodeEventArgs e)
        {
            // keep scene anchored to image
            if (e.Anchor is ARImageAnchor && _isUsingImageAnchor)
            {
                var t = e.Anchor.Transform;

                // inspired by toolkit hit test & set initial Transformation by screen point
                var tm = TransformationMatrix.Create(0, 0, 0, 1, t.Column3.X, t.Column3.Y, t.Column3.Z);

                _arView.SetInitialTransformation(TransformationMatrix.Identity - tm);
            }
        }

        private void CalibrateButtonTapped(object sender, EventArgs e) => NavigationController?.PopViewController(true);

        private void ShowMapView(object sender, EventArgs e) => NavigationController.PushViewController(new PlanesMapView(_arView.OriginCamera.Location), true);

        /// <summary>
        /// Identify an airplane or re-anchor the scene
        /// </summary>
        private async void ArSceneViewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            _airplaneFinder.ShouldUpdateIdentifyOverlay = false;
            _arView.GraphicsOverlays.Add(_airplaneFinder.IdentifyOverlay);
            var res = await _arView.IdentifyGraphicsOverlayAsync(_airplaneFinder.IdentifyOverlay, e.Position, 64, false, 1);
            _arView.GraphicsOverlays.Remove(_airplaneFinder.IdentifyOverlay);
            _airplaneFinder.ShouldUpdateIdentifyOverlay = true;
            if (res.Graphics.Any() && res.Graphics.First().Attributes["CALLSIGN"] is string callSign)
            {
                if (_airplaneFinder.Planes.ContainsKey(callSign))
                {
                    Plane targetPlane = _airplaneFinder.Planes[callSign];
                    _airplaneFinder.SelectedPlane = targetPlane;

                    View.AddSubview(_flightInfoVc.View);

                    if (TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Regular)
                    {
                        NSLayoutConstraint.ActivateConstraints(_flightInfoVcVerticalConstraints);
                    }
                    else
                    {
                        NSLayoutConstraint.ActivateConstraints(_flightInfoVcHorizontalConstraints);
                    }
                }
            }
            else
            {
                NSLayoutConstraint.DeactivateConstraints(_flightInfoVcHorizontalConstraints);
                NSLayoutConstraint.DeactivateConstraints(_flightInfoVcVerticalConstraints);
                _flightInfoVc.View.RemoveFromSuperview();

                // attempt to tap-to-place
                if (!_isUsingImageAnchor)
                {
                    _arView.SetInitialTransformation(e.Position);

                    displayScene();
                }
            }
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            if (NavigationController != null)
            {
                NavigationController.NavigationBarHidden = false;
            }
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // Stop ARKit tracking and unsubscribe from events when the view closes.
            if (_arView != null)
            {
                await _arView.StopTrackingAsync();
                _arView.GeoViewTapped -= ArSceneViewTapped;
                _closeButton.TouchUpInside -= CalibrateButtonTapped;
                _arView.ARSCNViewDidAddNode -= _arView_ARSCNViewDidAddNode;

                _placementModeSegment.ValueChanged -= _placementModeSegment_ValueChanged;
            }
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            if (!View.Subviews.ToList().Contains(_flightInfoVc.View))
            {
                return;
            }

            NSLayoutConstraint.DeactivateConstraints(_flightInfoVcHorizontalConstraints);
            NSLayoutConstraint.DeactivateConstraints(_flightInfoVcVerticalConstraints);

            if (TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                NSLayoutConstraint.ActivateConstraints(_flightInfoVcVerticalConstraints);
            }
            else
            {
                NSLayoutConstraint.ActivateConstraints(_flightInfoVcHorizontalConstraints);
            }
        }
    }
}
