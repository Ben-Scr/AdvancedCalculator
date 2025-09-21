using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using AdvancedCalculator.ViewModels;
using AdvancedCalculator.Core;

namespace AdvancedCalculator
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var sc = new ServiceCollection();
            sc.AddSingleton<CalculatorViewModel>();
            sc.AddSingleton<MainWindowViewModel>();

            Services = sc.BuildServiceProvider();
        }
    }
}
