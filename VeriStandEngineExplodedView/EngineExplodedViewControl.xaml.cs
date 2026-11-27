using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace NationalInstruments.VeriStand.EngineExplodedView
{
    /// <summary>
    /// 3D exploded view of a turbofan engine built with HelixToolkit.
    /// This view is independent of VeriStand. VeriStand only hosts it through <see cref="EngineExplodedViewViewModel"/>.
    /// </summary>
    public partial class EngineExplodedViewControl : UserControl
    {
        /// <summary>
        /// Dependency property for <see cref="ExplodeFactor"/>.
        /// </summary>
        public static readonly DependencyProperty ExplodeFactorProperty = DependencyProperty.Register(
            nameof(ExplodeFactor),
            typeof(double),
            typeof(EngineExplodedViewControl),
            new FrameworkPropertyMetadata(0.0, OnExplodeFactorChanged, CoerceExplodeFactor));

        /// <summary>
        /// Dependency property for <see cref="IsSpinning"/>.
        /// </summary>
        public static readonly DependencyProperty IsSpinningProperty = DependencyProperty.Register(
            nameof(IsSpinning),
            typeof(bool),
            typeof(EngineExplodedViewControl),
            new FrameworkPropertyMetadata(true, OnIsSpinningChanged));

        /// <summary>
        /// Dependency property for <see cref="ShowLabels"/>.
        /// </summary>
        public static readonly DependencyProperty ShowLabelsProperty = DependencyProperty.Register(
            nameof(ShowLabels),
            typeof(bool),
            typeof(EngineExplodedViewControl),
            new FrameworkPropertyMetadata(true, OnShowLabelsChanged));

        private const double LowPressureSpoolDegreesPerSecond = 90;
        private const double HighPressureSpoolDegreesPerSecond = 160;
        private static readonly Duration ExplodeAnimationDuration = new Duration(TimeSpan.FromSeconds(1.2));

        private readonly IList<EngineComponent> _components;
        private readonly List<BillboardTextVisual3D> _labels = new List<BillboardTextVisual3D>();
        private readonly Stopwatch _spinClock = new Stopwatch();
        private double _spinSeconds;
        private bool _isRenderingHooked;
        private EngineComponent _selected;

        /// <summary>
        /// Creates the view and builds the engine scene.
        /// </summary>
        public EngineExplodedViewControl()
        {
            InitializeComponent();

            _components = EngineGeometryFactory.CreateEngine();
            foreach (EngineComponent component in _components)
            {
                EngineRoot.Children.Add(component.Visual);
                _labels.Add(new BillboardTextVisual3D
                {
                    Text = component.Name,
                    Position = component.LabelAnchor,
                    Transform = component.Translation,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(0xA0, 0x10, 0x18, 0x22)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x5A, 0x8F, 0xD0)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(3, 1, 3, 1),
                    FontSize = 11
                });
            }

            UpdateLabels();
            ApplyExplodeFactor(ExplodeFactor);
            ResetCamera();

            Loaded += (sender, e) => UpdateSpinHook();
            Unloaded += (sender, e) => UnhookRendering();
        }

        /// <summary>
        /// Gets or sets how far the engine is exploded. 0 is fully assembled, 1 is fully exploded.
        /// </summary>
        public double ExplodeFactor
        {
            get { return (double)GetValue(ExplodeFactorProperty); }
            set { SetValue(ExplodeFactorProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the rotating parts spin.
        /// </summary>
        public bool IsSpinning
        {
            get { return (bool)GetValue(IsSpinningProperty); }
            set { SetValue(IsSpinningProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether part names are displayed.
        /// </summary>
        public bool ShowLabels
        {
            get { return (bool)GetValue(ShowLabelsProperty); }
            set { SetValue(ShowLabelsProperty, value); }
        }

        /// <summary>
        /// Gets the engine components shown by the view.
        /// </summary>
        public IList<EngineComponent> Components => _components;

        /// <summary>
        /// Smoothly animates the explode factor to the given value.
        /// </summary>
        /// <param name="target">Target explode factor between 0 and 1.</param>
        public void AnimateExplodeFactor(double target)
        {
            var animation = new DoubleAnimation(target, ExplodeAnimationDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop
            };
            animation.Completed += (sender, e) => ExplodeFactor = target;
            BeginAnimation(ExplodeFactorProperty, animation);
        }

        /// <summary>
        /// Restores the default camera position, framing the fully exploded engine.
        /// </summary>
        public void ResetCamera()
        {
            Camera.Position = new Point3D(-15, 11.5, 28);
            Camera.LookDirection = new Vector3D(14, -11.5, -28);
            Camera.UpDirection = new Vector3D(0, 1, 0);
        }

        private static object CoerceExplodeFactor(DependencyObject d, object value)
        {
            return Math.Max(0.0, Math.Min(1.0, (double)value));
        }

        private static void OnExplodeFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EngineExplodedViewControl)d).ApplyExplodeFactor((double)e.NewValue);
        }

        private static void OnIsSpinningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EngineExplodedViewControl)d).UpdateSpinHook();
        }

        private static void OnShowLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EngineExplodedViewControl)d).UpdateLabels();
        }

        private void ApplyExplodeFactor(double factor)
        {
            foreach (EngineComponent component in _components)
            {
                component.ApplyExplodeFactor(factor);
            }
        }

        private void UpdateLabels()
        {
            LabelRoot.Children.Clear();
            if (ShowLabels)
            {
                foreach (BillboardTextVisual3D label in _labels)
                {
                    LabelRoot.Children.Add(label);
                }
            }
        }

        private void UpdateSpinHook()
        {
            if (IsSpinning && IsLoaded)
            {
                if (!_isRenderingHooked)
                {
                    _spinClock.Restart();
                    CompositionTarget.Rendering += OnRendering;
                    _isRenderingHooked = true;
                }
            }
            else
            {
                UnhookRendering();
            }
        }

        private void UnhookRendering()
        {
            if (_isRenderingHooked)
            {
                CompositionTarget.Rendering -= OnRendering;
                _isRenderingHooked = false;
                _spinClock.Stop();
            }
        }

        private void OnRendering(object sender, EventArgs e)
        {
            _spinSeconds += _spinClock.Elapsed.TotalSeconds;
            _spinClock.Restart();
            SetSpinTime(_spinSeconds);
        }

        private void SetSpinTime(double seconds)
        {
            foreach (EngineComponent component in _components)
            {
                switch (component.Spool)
                {
                    case EngineSpool.LowPressure:
                        component.SetSpinAngle(seconds * LowPressureSpoolDegreesPerSecond);
                        break;
                    case EngineSpool.HighPressure:
                        component.SetSpinAngle(seconds * HighPressureSpoolDegreesPerSecond);
                        break;
                }
            }
        }

        private void Select(EngineComponent component)
        {
            if (_selected != null)
            {
                _selected.IsHighlighted = false;
            }
            _selected = component;
            if (component == null)
            {
                SelectionName.Text = "Click a part to inspect it";
                SelectionDescription.Text = string.Empty;
                return;
            }
            component.IsHighlighted = true;
            SelectionName.Text = component.Name;
            SelectionDescription.Text = component.Description;
        }

        private void OnViewportMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Viewport3D viewport = Viewport.Viewport;
            EngineComponent hitComponent = null;
            VisualTreeHelper.HitTest(
                viewport,
                null,
                result =>
                {
                    var rayResult = result as RayHitTestResult;
                    if (rayResult != null)
                    {
                        foreach (EngineComponent component in _components)
                        {
                            if (component.Owns(rayResult.VisualHit as Visual3D))
                            {
                                hitComponent = component;
                                return HitTestResultBehavior.Stop;
                            }
                        }
                    }
                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(e.GetPosition(viewport)));
            Select(hitComponent);
        }

        private void OnSliderPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Stop a running animation so the user can take over the slider immediately.
            double current = ExplodeFactor;
            BeginAnimation(ExplodeFactorProperty, null);
            ExplodeFactor = current;
        }

        private void OnExplodeClick(object sender, RoutedEventArgs e) => AnimateExplodeFactor(1.0);

        private void OnAssembleClick(object sender, RoutedEventArgs e) => AnimateExplodeFactor(0.0);

        private void OnResetViewClick(object sender, RoutedEventArgs e) => ResetCamera();
    }
}
