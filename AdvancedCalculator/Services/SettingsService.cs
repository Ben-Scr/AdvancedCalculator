using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using AdvancedCalculator.Models;

namespace AdvancedCalculator.Services;

public sealed class SettingsService
{
    public async Task<UserSettings> LoadAsync()
    {
        await EnsureBootstrapAsync();

        try
        {
            var json = await File.ReadAllTextAsync(UserDataPaths.SettingsFilePath, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<UserSettings>(json, UserDataPaths.JsonOptions);
            return Normalize(settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load user settings: {ex}");
            return new UserSettings();
        }
    }

    public async Task SaveAsync(UserSettings settings)
    {
        await EnsureBootstrapAsync();

        try
        {
            var normalized = Normalize(settings);
            var json = JsonSerializer.Serialize(normalized, UserDataPaths.JsonOptions);
            await File.WriteAllTextAsync(UserDataPaths.SettingsFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save user settings: {ex}");
        }
    }

    public async Task EnsureBootstrapAsync()
    {
        try
        {
            Directory.CreateDirectory(UserDataPaths.BaseDirectory);

            if (!File.Exists(UserDataPaths.SettingsFilePath))
            {
                var json = JsonSerializer.Serialize(new UserSettings(), UserDataPaths.JsonOptions);
                await File.WriteAllTextAsync(UserDataPaths.SettingsFilePath, json, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to ensure settings bootstrap: {ex}");
        }
    }

    private static UserSettings Normalize(UserSettings? settings)
    {
        var theme = settings?.Theme?.Trim().ToLowerInvariant();
        return new UserSettings
        {
            Theme = theme is "light" ? "light" : "dark"
        };
    }
}
