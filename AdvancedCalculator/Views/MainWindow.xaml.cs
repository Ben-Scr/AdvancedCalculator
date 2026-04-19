using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using AdvancedCalculator.ViewModels;

namespace AdvancedCalculator.Views;

public partial class MainWindow : Window
{
    private const double SidebarOpenWidth = 280d;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        HistorySidebar.Width = ViewModel.IsHistoryOpen ? SidebarOpenWidth : 0d;
        UpdateErrorState(animated: false);
        FocusExpressionBox();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        Loaded -= OnLoaded;
        Closed -= OnClosed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsHistoryOpen))
        {
            AnimateSidebar(ViewModel.IsHistoryOpen);
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

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void AnimateSidebar(bool isOpen)
    {
        var animation = new DoubleAnimation
        {
            To = isOpen ? SidebarOpenWidth : 0d,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        HistorySidebar.BeginAnimation(FrameworkElement.WidthProperty, animation);
    }

    private void UpdateErrorState(bool animated)
    {
        if (ViewModel.HasError)
        {
            ErrorContainer.Visibility = Visibility.Visible;
            if (!animated)
            {
                ErrorContainer.MaxHeight = 40d;
                ErrorContainer.Opacity = 1d;
                return;
            }

            ErrorContainer.BeginAnimation(MaxHeightProperty, new DoubleAnimation
            {
                From = 0d,
                To = 40d,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            ErrorContainer.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0d,
                To = 1d,
                Duration = TimeSpan.FromMilliseconds(180)
            });

            return;
        }

        if (!animated)
        {
            ErrorContainer.MaxHeight = 0d;
            ErrorContainer.Opacity = 0d;
            ErrorContainer.Visibility = Visibility.Collapsed;
            return;
        }

        var collapseAnimation = new DoubleAnimation
        {
            To = 0d,
            Duration = TimeSpan.FromMilliseconds(140),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        collapseAnimation.Completed += (_, _) => ErrorContainer.Visibility = Visibility.Collapsed;
        ErrorContainer.BeginAnimation(MaxHeightProperty, collapseAnimation);
        ErrorContainer.BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            To = 0d,
            Duration = TimeSpan.FromMilliseconds(120)
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
}
