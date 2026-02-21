using AdvancedCalculator.Core;
using AdvancedCalculator.Models;
using BenScr.Math.Parser;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AdvancedCalculator.ViewModels
{
    public class CalculatorViewModel : INotifyPropertyChanged
    {
        private string display = "0";
        public string Display
        {
            get => display;
            set 
            { 
                if(value != null)
                {
                    display = value; 
                    OnPropertyChanged();
                }
            }
        }

        
        private string expressionPreview = string.Empty;
        public string ExpressionPreview
        {
            get => expressionPreview;
            set
            {
                expressionPreview = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        private int cursorIndex;
        public int CursorIndex
        {
            get => cursorIndex;
            set
            {
                if (value >= 0 && value <= Display.Length)
                {
                    cursorIndex = value; 
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<CalcKey> Keys { get; } = new();
        private string ans;

        public CalculatorViewModel()
        {
            BuildKeys();
        }

        private void BuildKeys()
        {
            // Row 1
            Add("%", keyType: KeyType.RightOperator);
            Add("log", keyType: KeyType.Function);
            Add("sin", keyType: KeyType.Function);
            Add("cos", keyType: KeyType.Function);
            Add("tan", keyType: KeyType.Function);
            // Row 2
            Add("(");
            Add(")");
            Add("^", keyType: KeyType.RightOperator);
            Add("√", keyType: KeyType.LeftOperator);
            Add("π", keyType: KeyType.Default);
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
            Add("×", keyType: KeyType.RightOperator);
            Add("÷", keyType: KeyType.RightOperator);

            // Row 5
            Add("1");
            Add("2");
            Add("3");
            Add("+", keyType: KeyType.Operator);
            Add("−", keyType: KeyType.Operator);
            // Row 6
            Add("0");
            Add(Lexer.FloatingPointSeparator.ToString(), new RelayCommand(func => AppendDot()));
            Add("e", keyType: KeyType.Default);
            Add("Ans", new RelayCommand(func => Append(ans, KeyType.DirectFunction)));
            Add("=", new RelayCommand(func => Evaluate()), isAccent: true);
        }

        private void Add(string label, RelayCommand command = null, bool isAccent = false, string displayOverride = null, KeyType keyType = KeyType.Digit)
        {
            if (string.IsNullOrEmpty(label))
            {
                Keys.Add(new CalcKey { Label = "", Command = new RelayCommand(_ => { }), IsAccent = false, IsEnabled = false });
                return;
            }

            command ??= new RelayCommand(a => Append(displayOverride ?? label, keyType));
            Keys.Add(new CalcKey { Label = label, Command = command, IsAccent = isAccent });
        }



        private void Append(string s, KeyType keyType)
        {
            int idx = cursorIndex;

            bool displayZeroOrNull = Display == "0" || string.IsNullOrEmpty(Display);

            if (displayZeroOrNull && keyType == KeyType.RightOperator) return;

            if (!displayZeroOrNull && (keyType == KeyType.Operator || keyType == KeyType.RightOperator) && s != "^")
            {
                s = $" {s} ";
            }
            else if (keyType == KeyType.Function)
            {
                s = $"{s}(";
            }
            if ((displayZeroOrNull || Display == ans) && keyType != KeyType.RightOperator)
            {
                Display = s;
            }
            else Display = Display.Insert(Math.Clamp(idx, 0, Display.Length), s ?? "0");

            CursorIndex = idx + s?.Length ?? 0;
        }

        private void ClearDisplay()
        {
            Display = "0";
            ExpressionPreview = string.Empty;
        }


        private void AppendDot()
        {
            var txt = Display;
            int i = txt.Length - 1;

            while (i >= 0 && (char.IsDigit(txt[i]) || txt[i] == '.')) i--;

            var last = txt[(i + 1)..];
            if (!last.Contains(Lexer.FloatingPointSeparator.ToString()))
            {
                Append(Lexer.FloatingPointSeparator.ToString(), KeyType.Default);
            }
        }

        private void Backspace()
        {
            int idx = CursorIndex;

            if (idx - 1 < 0) return;

            if (Display.Length <= 1)
            {
                Display = "0";
            }
            else
            {
                int removeCount = Display[Display.Length - 1] == ' ' ? 3 : 1;
                Display = Display.Remove(idx - removeCount);
                idx -= removeCount;
            }
            CursorIndex = idx;
        }

        private void Evaluate()
        {
            ExpressionPreview = $"{Display} =";
            var expr = Display.Replace("×", "*").Replace("÷", "/").Replace("−", "-");
            Display = Calculator.Evaluate<string>(expr);
            ans = Display;
            CursorIndex = Display.Length;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
