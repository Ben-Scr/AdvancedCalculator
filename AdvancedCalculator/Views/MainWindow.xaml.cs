using AdvancedCalculator.ViewModels;
using System.Windows;

namespace AdvancedCalculator.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var calcVm = new CalculatorViewModel();
            var mainVm = new MainWindowViewModel(calcVm);

            DataContext = mainVm;
        }
    }
}