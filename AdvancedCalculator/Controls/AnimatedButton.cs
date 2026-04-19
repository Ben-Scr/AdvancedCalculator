using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AdvancedCalculator.Controls;

public class AnimatedButton : Button
{
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(AnimatedButton),
            new PropertyMetadata(new CornerRadius(6)));

    public static readonly DependencyProperty NormalColorProperty =
        DependencyProperty.Register(
            nameof(NormalColor),
            typeof(Color),
            typeof(AnimatedButton),
            new PropertyMetadata(Colors.Transparent, OnNormalColorChanged));

    public static readonly DependencyProperty HoverColorProperty =
        DependencyProperty.Register(
            nameof(HoverColor),
            typeof(Color),
            typeof(AnimatedButton),
            new PropertyMetadata(Colors.Transparent));

    public static readonly DependencyProperty PressedColorProperty =
        DependencyProperty.Register(
            nameof(PressedColor),
            typeof(Color),
            typeof(AnimatedButton),
            new PropertyMetadata(Colors.Transparent));

    private Border? _backgroundBorder;
    private ScaleTransform? _scaleTransform;
    private SolidColorBrush? _backgroundBrush;

    static AnimatedButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedButton), new FrameworkPropertyMetadata(typeof(AnimatedButton)));
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Color NormalColor
    {
        get => (Color)GetValue(NormalColorProperty);
        set => SetValue(NormalColorProperty, value);
    }

    public Color HoverColor
    {
        get => (Color)GetValue(HoverColorProperty);
        set => SetValue(HoverColorProperty, value);
    }

    public Color PressedColor
    {
        get => (Color)GetValue(PressedColorProperty);
        set => SetValue(PressedColorProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _backgroundBorder = GetTemplateChild("PART_BackgroundBorder") as Border;
        _scaleTransform = GetTemplateChild("PART_ScaleTransform") as ScaleTransform;

        if (_backgroundBorder is not null)
        {
            _backgroundBrush = new SolidColorBrush(NormalColor);
            _backgroundBorder.Background = _backgroundBrush;
        }

        AnimateToColor(NormalColor, TimeSpan.Zero);
        AnimateScale(1d, TimeSpan.Zero);
    }

    protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        AnimateToColor(HoverColor, TimeSpan.FromMilliseconds(140));
    }

    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        AnimateToColor(NormalColor, TimeSpan.FromMilliseconds(140));
        AnimateScale(1d, TimeSpan.FromMilliseconds(90));
    }

    protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        AnimateToColor(PressedColor, TimeSpan.FromMilliseconds(90));
        AnimateScale(0.93d, TimeSpan.FromMilliseconds(90));
    }

    protected override void OnPreviewMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        AnimateToColor(IsMouseOver ? HoverColor : NormalColor, TimeSpan.FromMilliseconds(110));
        AnimateScale(1d, TimeSpan.FromMilliseconds(110));
    }

    private static void OnNormalColorChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs _)
    {
        if (dependencyObject is AnimatedButton button && !button.IsMouseOver && !button.IsPressed)
        {
            button.AnimateToColor(button.NormalColor, TimeSpan.Zero);
        }
    }

    private void AnimateToColor(Color targetColor, TimeSpan duration)
    {
        if (_backgroundBrush is null)
        {
            return;
        }

        var animation = new ColorAnimation
        {
            To = targetColor,
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _backgroundBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }

    private void AnimateScale(double targetScale, TimeSpan duration)
    {
        if (_scaleTransform is null)
        {
            return;
        }

        var animation = new DoubleAnimation
        {
            To = targetScale,
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }
}
