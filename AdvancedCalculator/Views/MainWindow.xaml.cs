using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AdvancedCalculator.ViewModels;

namespace AdvancedCalculator.Views;

public partial class MainWindow : Window
{
    private const double SidebarOpenWidth = 280d;
    private const double ErrorOffset = -6d;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
        StateChanged += OnWindowStateChanged;
        Closed += OnClosed;
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private TranslateTransform ErrorTranslateTransform => (TranslateTransform)ErrorContainer.RenderTransform;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        HistorySidebar.Width = ViewModel.IsHistoryOpen ? SidebarOpenWidth : 0d;
        HistorySidebar.Visibility = ViewModel.IsHistoryOpen ? Visibility.Visible : Visibility.Collapsed;
        HistorySidebar.IsHitTestVisible = ViewModel.IsHistoryOpen;
        UpdateWindowStateControls();
        UpdateErrorState(animated: false);
        FocusExpressionBox();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        Loaded -= OnLoaded;
        StateChanged -= OnWindowStateChanged;
        Closed -= OnClosed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsHistoryOpen))
        {
            UpdateSidebarState(ViewModel.IsHistoryOpen);
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.SelectedCalculatorMode))
        {
            NavigationFlyout.IsOpen = false;

            if (ViewModel.IsCalculatorMode)
            {
                Dispatcher.InvokeAsync(FocusExpressionBox);
            }

            return;
        }

        if (e.PropertyName == nameof(MainViewModel.ErrorMessage))
        {
            UpdateErrorState(animated: true);
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.Result))
        {
            AnimateResult();
        }
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private void Minimize_OnClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeRestore_OnClick(object sender, RoutedEventArgs e) => ToggleWindowState();

    private void Close_OnClick(object sender, RoutedEventArgs e) => Close();

    private void NavigationMenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        NavigationFlyout.IsOpen = !NavigationFlyout.IsOpen;
    }

    private void NavigationFlyoutAction_OnClick(object sender, RoutedEventArgs e) => NavigationFlyout.IsOpen = false;

    private void OnWindowStateChanged(object? sender, EventArgs e) => UpdateWindowStateControls();

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateSidebarState(bool isOpen)
    {
        HistorySidebar.BeginAnimation(FrameworkElement.WidthProperty, null);
        HistorySidebar.Width = isOpen ? SidebarOpenWidth : 0d;
        HistorySidebar.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
        HistorySidebar.IsHitTestVisible = isOpen;
    }

    private void UpdateErrorState(bool animated)
    {
        ErrorContainer.BeginAnimation(OpacityProperty, null);
        ErrorTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);

        if (ViewModel.HasError)
        {
            ErrorContainer.Visibility = Visibility.Visible;
            if (!animated)
            {
                ErrorContainer.Opacity = 1d;
                ErrorTranslateTransform.Y = 0d;
                return;
            }

            ErrorContainer.Opacity = 0d;
            ErrorTranslateTransform.Y = ErrorOffset;

            ErrorContainer.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0d,
                To = 1d,
                Duration = TimeSpan.FromMilliseconds(180)
            });

            ErrorTranslateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
            {
                From = ErrorOffset,
                To = 0d,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            return;
        }

        if (!animated)
        {
            ErrorContainer.Opacity = 0d;
            ErrorTranslateTransform.Y = ErrorOffset;
            ErrorContainer.Visibility = Visibility.Collapsed;
            return;
        }

        var opacityAnimation = new DoubleAnimation
        {
            To = 0d,
            Duration = TimeSpan.FromMilliseconds(120)
        };

        opacityAnimation.Completed += (_, _) =>
        {
            if (!ViewModel.HasError)
            {
                ErrorContainer.Visibility = Visibility.Collapsed;
            }
        };

        ErrorContainer.BeginAnimation(OpacityProperty, opacityAnimation);
        ErrorTranslateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
        {
            To = ErrorOffset,
            Duration = TimeSpan.FromMilliseconds(140),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        });
    }

    private void AnimateResult()
    {
        ResultTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            From = 0d,
            To = 1d,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private void FocusExpressionBox()
    {
        ExpressionTextBox.Focus();
        ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
    }

    private void UpdateWindowStateControls()
    {
        MaximizeRestoreButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        MaximizeRestoreButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
    }
}
