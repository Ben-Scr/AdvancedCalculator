using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using BenScr.AdvancedCalculator.Models;

namespace BenScr.AdvancedCalculator.Services;

public sealed class HistoryService
{
    public async Task<IReadOnlyList<CalculationHistoryItem>> LoadAsync()
    {
        await EnsureBootstrapAsync();

        try
        {
            var json = await File.ReadAllTextAsync(UserDataPaths.HistoryFilePath, Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<CalculationHistoryItem>>(json, UserDataPaths.JsonOptions);
            return items ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load history: {ex}");
            return [];
        }
    }

    public async Task SaveAsync(IEnumerable<CalculationHistoryItem> items)
    {
        await EnsureBootstrapAsync();

        try
        {
            var json = JsonSerializer.Serialize(items, UserDataPaths.JsonOptions);
            await File.WriteAllTextAsync(UserDataPaths.HistoryFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save history: {ex}");
        }
    }

    public async Task EnsureBootstrapAsync()
    {
        try
        {
            Directory.CreateDirectory(UserDataPaths.BaseDirectory);

            if (File.Exists(UserDataPaths.HistoryFilePath))
            {
                return;
            }

            if (File.Exists(UserDataPaths.LegacyHistoryFilePath))
            {
                File.Copy(UserDataPaths.LegacyHistoryFilePath, UserDataPaths.HistoryFilePath);
                return;
            }

            await File.WriteAllTextAsync(UserDataPaths.HistoryFilePath, "[]", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to ensure history bootstrap: {ex}");
        }
    }
}
