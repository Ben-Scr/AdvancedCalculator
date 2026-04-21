namespace BenScr.AdvancedCalculator.Models;

public sealed class UserSettings
{
    public string Theme { get; set; } = "dark";

    public bool LiveCalculationEnabled { get; set; } = true;
}
