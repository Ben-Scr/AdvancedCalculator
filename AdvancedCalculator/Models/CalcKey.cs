using System.Windows.Input;

namespace AdvancedCalculator.Models
{
    public class CalcKey
    {
        public string Label { get; set; } = "";
        public object? CommandParameter { get; set; }
        public ICommand? Command { get; set; }
        public bool IsAccent { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}