using System;
using System.Windows;
using System.Windows.Controls;

namespace BenScr.AdvancedCalculator.Core;

public static class CaretIndexBinding
{
    public static readonly DependencyProperty CaretIndexProperty =
        DependencyProperty.RegisterAttached(
            "CaretIndex",
            typeof(int),
            typeof(CaretIndexBinding),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnCaretIndexChanged));

    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(CaretIndexBinding),
            new PropertyMetadata(false, OnEnableChanged));

    private static readonly DependencyProperty IsHookedProperty =
        DependencyProperty.RegisterAttached(
            "IsHooked",
            typeof(bool),
            typeof(CaretIndexBinding),
            new PropertyMetadata(false));

    public static void SetCaretIndex(DependencyObject obj, int value) => obj.SetValue(CaretIndexProperty, value);

    public static int GetCaretIndex(DependencyObject obj) => (int)obj.GetValue(CaretIndexProperty);

    public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

    public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);

    private static bool GetIsHooked(DependencyObject obj) => (bool)obj.GetValue(IsHookedProperty);

    private static void SetIsHooked(DependencyObject obj, bool value) => obj.SetValue(IsHookedProperty, value);

    private static void OnCaretIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not TextBox textBox)
        {
            return;
        }

        var newIndex = (int)e.NewValue;
        var coercedIndex = Math.Clamp(newIndex, 0, textBox.Text?.Length ?? 0);

        if (textBox.CaretIndex != coercedIndex)
        {
            textBox.CaretIndex = coercedIndex;
        }
    }

    private static void OnEnableChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not TextBox textBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            if (GetIsHooked(textBox))
            {
                return;
            }

            textBox.Loaded += SyncToViewModel;
            textBox.GotKeyboardFocus += SyncToViewModel;
            textBox.SelectionChanged += SelectionChanged;
            SetIsHooked(textBox, true);

            SyncToViewModel(textBox, EventArgs.Empty);
            return;
        }

        if (!GetIsHooked(textBox))
        {
            return;
        }

        textBox.Loaded -= SyncToViewModel;
        textBox.GotKeyboardFocus -= SyncToViewModel;
        textBox.SelectionChanged -= SelectionChanged;
        SetIsHooked(textBox, false);
    }

    private static void SyncToViewModel(object? sender, EventArgs e)
    {
        var textBox = (TextBox)sender!;
        var caretIndex = textBox.CaretIndex;

        if (GetCaretIndex(textBox) != caretIndex)
        {
            SetCaretIndex(textBox, caretIndex);
        }
    }

    private static void SelectionChanged(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var caretIndex = textBox.CaretIndex;

        if (GetCaretIndex(textBox) != caretIndex)
        {
            SetCaretIndex(textBox, caretIndex);
        }
    }
}
