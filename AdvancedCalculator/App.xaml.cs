using System.Windows;
using BenScr.AdvancedCalculator.Services;
using BenScr.AdvancedCalculator.ViewModels;
using BenScr.AdvancedCalculator.Views;

namespace BenScr.AdvancedCalculator;

public partial class App : Application
{
    private static readonly Uri DarkThemeUri = new("Resources/Themes/DarkTheme.xaml", UriKind.Relative);
    private static readonly Uri LightThemeUri = new("Resources/Themes/LightTheme.xaml", UriKind.Relative);

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var calculatorService = new CalculatorService();
        var converterService = new ConverterService();
        var historyService = new HistoryService();
        var settingsService = new SettingsService();
        var historyViewModel = new HistoryViewModel();
        var mainViewModel = new MainViewModel(calculatorService, converterService, historyService, settingsService, historyViewModel);

        await historyService.EnsureBootstrapAsync();
        var settings = await settingsService.LoadAsync();
        ApplyTheme(settings.Theme);
        await mainViewModel.InitializeAsync(settings);

        MainWindow = new MainWindow(mainViewModel);
        MainWindow.Show();
    }

    public static void ApplyTheme(string theme)
    {
        if (Current is null)
        {
            return;
        }

        var mergedDictionaries = Current.Resources.MergedDictionaries;
        var existingThemeDictionary = mergedDictionaries
            .FirstOrDefault(dictionary => dictionary.Source is not null &&
                                          dictionary.Source.OriginalString.Contains("/Themes/", StringComparison.OrdinalIgnoreCase));

        if (existingThemeDictionary is not null)
        {
            mergedDictionaries.Remove(existingThemeDictionary);
        }

        mergedDictionaries.Add(new ResourceDictionary
        {
            Source = theme.Equals("light", StringComparison.OrdinalIgnoreCase)
                ? LightThemeUri
                : DarkThemeUri
        });
    }
}
