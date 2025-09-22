using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AdvancedCalculator.Core
{
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

        public static void SetCaretIndex(DependencyObject obj, int value)
            => obj.SetValue(CaretIndexProperty, value);
        public static int GetCaretIndex(DependencyObject obj)
            => (int)obj.GetValue(CaretIndexProperty);

        private static void OnCaretIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                int newIndex = (int)e.NewValue;
                int coerced = Math.Max(0, Math.Min(newIndex, tb.Text?.Length ?? 0));
                if (tb.CaretIndex != coerced)
                    tb.CaretIndex = coerced;
            }
        }

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(CaretIndexBinding),
                new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject obj, bool value)
            => obj.SetValue(EnableProperty, value);
        public static bool GetEnable(DependencyObject obj)
            => (bool)obj.GetValue(EnableProperty);


        private static readonly DependencyProperty IsHookedProperty =
            DependencyProperty.RegisterAttached(
                "IsHooked",
                typeof(bool),
                typeof(CaretIndexBinding),
                new PropertyMetadata(false));
        private static bool GetIsHooked(DependencyObject o) => (bool)o.GetValue(IsHookedProperty);
        private static void SetIsHooked(DependencyObject o, bool v) => o.SetValue(IsHookedProperty, v);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb) return;

            if ((bool)e.NewValue)
            {
                if (GetIsHooked(tb)) return;
                tb.Loaded += Tb_SyncToVm;
                tb.GotKeyboardFocus += Tb_SyncToVm;
                tb.SelectionChanged += Tb_SelectionChanged;
                SetIsHooked(tb, true);

                Tb_SyncToVm(tb, EventArgs.Empty);
            }
            else
            {
                if (!GetIsHooked(tb)) return;
                tb.Loaded -= Tb_SyncToVm;
                tb.GotKeyboardFocus -= Tb_SyncToVm;
                tb.SelectionChanged -= Tb_SelectionChanged;
                SetIsHooked(tb, false);
            }
        }

        private static void Tb_SyncToVm(object? sender, EventArgs e)
        {
            var tb = (TextBox)sender!;
            int caret = tb.CaretIndex;
            if (GetCaretIndex(tb) != caret)
                SetCaretIndex(tb, caret);
        }

        private static void Tb_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            int caret = tb.CaretIndex;
            if (GetCaretIndex(tb) != caret)
                SetCaretIndex(tb, caret);
        }
    }
}
