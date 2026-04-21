using System.IO;
using System.Text.Json;

namespace BenScr.AdvancedCalculator.Services;

internal static class UserDataPaths
{
    private static readonly JsonSerializerOptions JsonOptionsValue = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string BaseDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BenScr",
            "AdvancedCalculator",
            "User");

    public static string LegacyBaseDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BenScr",
            "SmartCalculator",
            "User");

    public static string HistoryFilePath => Path.Combine(BaseDirectory, "history.json");

    public static string SettingsFilePath => Path.Combine(BaseDirectory, "user.json");

    public static string LegacyHistoryFilePath => Path.Combine(LegacyBaseDirectory, "history.json");

    public static string LegacySettingsFilePath => Path.Combine(LegacyBaseDirectory, "user.json");

    public static JsonSerializerOptions JsonOptions => JsonOptionsValue;
}
