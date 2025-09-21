namespace AdvancedCalculator.ViewModels
{
    public class MainWindowViewModel
    {
        public CalculatorViewModel Calculator { get; }
        public MainWindowViewModel(CalculatorViewModel calculator)
        {
            Calculator = calculator;
        }
    }
}
