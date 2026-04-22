using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BenScr.AdvancedCalculator.Controls;
using BenScr.AdvancedCalculator.ViewModels;

namespace BenScr.AdvancedCalculator.Views;

public partial class MainWindow : Window
{
    private const double SidebarOpenWidth = 310d;
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
        UpdateHistoryState(animated: false);
        UpdateWindowStateControls();
        UpdateErrorState(animated: false);

        if (ViewModel.IsCalculatorPage)
        {
            FocusExpressionBox();
        }
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
            UpdateHistoryState(animated: true);
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.SelectedNavigationPage) ||
            e.PropertyName == nameof(MainViewModel.SelectedCalculatorMode))
        {
            if (ViewModel.IsCalculatorPage)
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
        if (sender is not AnimatedButton button || button.ContextMenu is null)
        {
            return;
        }

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }

    private void HistoryOverlay_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.CloseHistoryCommand.CanExecute(null))
        {
            ViewModel.CloseHistoryCommand.Execute(null);
        }
    }

    private void OnWindowStateChanged(object? sender, EventArgs e) => UpdateWindowStateControls();

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateHistoryState(bool animated)
    {
        HistoryOverlay.BeginAnimation(OpacityProperty, null);
        HistorySidebar.BeginAnimation(OpacityProperty, null);
        HistorySidebarTransform.BeginAnimation(TranslateTransform.XProperty, null);

        if (ViewModel.IsHistoryOpen)
        {
            HistoryOverlay.Visibility = Visibility.Visible;
            HistoryOverlay.IsHitTestVisible = true;
            HistorySidebar.Visibility = Visibility.Visible;
            HistorySidebar.IsHitTestVisible = true;

            if (!animated)
            {
                HistoryOverlay.Opacity = 1d;
                HistorySidebar.Opacity = 1d;
                HistorySidebarTransform.X = 0d;
                return;
            }

            HistoryOverlay.BeginAnimation(OpacityProperty, CreateDoubleAnimation(HistoryOverlay.Opacity, 1d, 180));
            HistorySidebar.BeginAnimation(OpacityProperty, CreateDoubleAnimation(HistorySidebar.Opacity, 1d, 180));
            HistorySidebarTransform.BeginAnimation(
                TranslateTransform.XProperty,
                CreateDoubleAnimation(HistorySidebarTransform.X, 0d, 220));

            return;
        }

        HistorySidebar.IsHitTestVisible = false;

        if (!animated)
        {
            HistoryOverlay.Opacity = 0d;
            HistoryOverlay.Visibility = Visibility.Collapsed;
            HistoryOverlay.IsHitTestVisible = false;
            HistorySidebar.Opacity = 0d;
            HistorySidebarTransform.X = SidebarOpenWidth;
            HistorySidebar.Visibility = Visibility.Collapsed;
            return;
        }

        var overlayAnimation = CreateDoubleAnimation(HistoryOverlay.Opacity, 0d, 140);
        overlayAnimation.Completed += (_, _) =>
        {
            if (!ViewModel.IsHistoryOpen)
            {
                HistoryOverlay.Visibility = Visibility.Collapsed;
                HistoryOverlay.IsHitTestVisible = false;
            }
        };

        var sidebarOpacityAnimation = CreateDoubleAnimation(HistorySidebar.Opacity, 0d, 140);
        sidebarOpacityAnimation.Completed += (_, _) =>
        {
            if (!ViewModel.IsHistoryOpen)
            {
                HistorySidebar.Visibility = Visibility.Collapsed;
            }
        };

        HistoryOverlay.BeginAnimation(OpacityProperty, overlayAnimation);
        HistorySidebar.BeginAnimation(OpacityProperty, sidebarOpacityAnimation);
        HistorySidebarTransform.BeginAnimation(
            TranslateTransform.XProperty,
            CreateDoubleAnimation(HistorySidebarTransform.X, SidebarOpenWidth, 220));
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

            ErrorContainer.BeginAnimation(OpacityProperty, CreateDoubleAnimation(0d, 1d, 180));
            ErrorTranslateTransform.BeginAnimation(
                TranslateTransform.YProperty,
                CreateDoubleAnimation(ErrorOffset, 0d, 180));
            return;
        }

        if (!animated)
        {
            ErrorContainer.Opacity = 0d;
            ErrorTranslateTransform.Y = ErrorOffset;
            ErrorContainer.Visibility = Visibility.Collapsed;
            return;
        }

        var opacityAnimation = CreateDoubleAnimation(ErrorContainer.Opacity, 0d, 120);
        opacityAnimation.Completed += (_, _) =>
        {
            if (!ViewModel.HasError)
            {
                ErrorContainer.Visibility = Visibility.Collapsed;
            }
        };

        ErrorContainer.BeginAnimation(OpacityProperty, opacityAnimation);
        ErrorTranslateTransform.BeginAnimation(
            TranslateTransform.YProperty,
            CreateDoubleAnimation(ErrorTranslateTransform.Y, ErrorOffset, 140));
    }

    private void AnimateResult()
    {
        ResultTextBlock.BeginAnimation(OpacityProperty, CreateDoubleAnimation(0d, 1d, 200));
    }

    private void FocusExpressionBox()
    {
        if (!ViewModel.IsCalculatorPage)
        {
            return;
        }

        ExpressionTextBox.Focus();
        ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
    }

    private void UpdateWindowStateControls()
    {
        MaximizeRestoreButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        MaximizeRestoreButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
    }

    private static DoubleAnimation CreateDoubleAnimation(double from, double to, int milliseconds) =>
        new()
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(milliseconds),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
}
