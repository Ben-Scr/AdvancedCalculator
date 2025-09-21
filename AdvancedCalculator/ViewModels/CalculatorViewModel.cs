using AdvancedCalculator.Core;
using AdvancedCalculator.Models;
using Parser;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AdvancedCalculator.ViewModels
{
    public class CalculatorViewModel : INotifyPropertyChanged
    {
        private string display = "0";

        public string Display
        {
            get => display;
            set { display = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CalcKey> Keys { get; } = new();

        private Evaluator evaluator;

        public CalculatorViewModel()
        {
            BuildKeys();
            evaluator = Evaluator.Calculator();
        }

        private void BuildKeys()
        {
            // Row 1
            Add("%");
            Add("log");
            Add("sin");
            Add("cos");
            Add("tan");
            // Row 2
            Add("(");
            Add(")");
            Add("^");
            Add("√");
            Add("π");
            // Row 3
            Add("7");
            Add("8");
            Add("9");
            Add("DEL", new RelayCommand(func => Backspace()));
            Add("AC", new RelayCommand(func => ClearDisplay()));
            // Row 4
            Add("4");
            Add("5");
            Add("6");
            Add("×");
            Add("÷");

            // Row 5
            Add("1");
            Add("2");
            Add("3");
            Add("+");
            Add("−");
            // Row 6
            Add("0");
            Add(".", new RelayCommand(func => AppendDot()));
            Add("");
            Add("Ans");
            Add("=", new RelayCommand(func => Evaluate()), isAccent: true);
        }

        private void Add(string label, RelayCommand command = null, bool isAccent = false, string displayOverride = null)
        {
            if (string.IsNullOrEmpty(label))
            {
                Keys.Add(new CalcKey { Label = "", Command = new RelayCommand(_ => { }), IsAccent = false, IsEnabled = false });
                return;
            }

            command ??= new RelayCommand(a => Append(displayOverride ?? label));
            Keys.Add(new CalcKey { Label = label, Command = command, IsAccent = isAccent });
        }

        private void Append(string s)
        {
            if (Display == "0") Display = s;
            else Display += s;
        }

        private void ClearDisplay() { Display = "0"; }

        private void AppendDot()
        {
            var txt = Display;
            int i = txt.Length - 1;

            while (i >= 0 && (char.IsDigit(txt[i]) || txt[i] == '.')) i--;

            var last = txt[(i + 1)..];
            if (!last.Contains("."))
            {
                Append(".");
            }
        }

        private void Backspace()
        {
            if (Display.Length <= 1) Display = "0";
            else if (Display[Display.Length - 1] == ' ') Display = Display.Remove(Math.Clamp(Display.Length - 3, 0, int.MaxValue));
            else Display = Display[..^1];
        }

        private void Evaluate()
        {
            try
            {
                var expr = Display.Replace("×", "*").Replace("÷", "/").Replace("−", "-");
                Display = Parser.ParserRuntime.Run(expr, evaluator)?.ToString() ?? "0";
            }
            catch { Display = "Error"; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
