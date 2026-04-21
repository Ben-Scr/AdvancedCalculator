namespace BenScr.AdvancedCalculator.Models;

public sealed class CalculationHistoryItem
{
    public string Expression { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}
