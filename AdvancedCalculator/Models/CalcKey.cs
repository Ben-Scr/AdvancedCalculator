using System.Windows.Input;

namespace AdvancedCalculator.Models;

public sealed class CalcKey
{
    public string Label { get; init; } = string.Empty;

    public object? CommandParameter { get; init; }

    public ICommand? Command { get; init; }

    public bool IsAccent { get; init; }

    public bool IsEnabled { get; init; } = true;

    public bool IsPlaceholder { get; init; }
}
