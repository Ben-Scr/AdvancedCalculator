using System.Collections.ObjectModel;
using BenScr.AdvancedCalculator.Core;
using BenScr.AdvancedCalculator.Services;

namespace BenScr.AdvancedCalculator.ViewModels;

public sealed class UnitConverterViewModel : ObservableObject
{
    private readonly Func<string, string, string, string> _convertValue;
    private string _sourceUnit;
    private string _targetUnit;
    private string _sourceValue = string.Empty;
    private string _targetValue = string.Empty;
    private string _errorMessage = string.Empty;

    public UnitConverterViewModel(
        string title,
        IEnumerable<string> units,
        string defaultSourceUnit,
        string defaultTargetUnit,
        Func<string, string, string, string> convertValue)
    {
        Title = title;
        Units = new ObservableCollection<string>(units);
        _convertValue = convertValue;
        _sourceUnit = ResolveInitialUnit(defaultSourceUnit, 0);
        _targetUnit = ResolveInitialUnit(defaultTargetUnit, 1);

        if (string.Equals(_sourceUnit, _targetUnit, StringComparison.OrdinalIgnoreCase) && Units.Count > 1)
        {
            _targetUnit = Units.First(unit => !string.Equals(unit, _sourceUnit, StringComparison.OrdinalIgnoreCase));
        }
    }

    public string Title { get; }

    public ObservableCollection<string> Units { get; }

    public string SourceUnit
    {
        get => _sourceUnit;
        set
        {
            if (SetProperty(ref _sourceUnit, ResolveUnit(value, _sourceUnit)))
            {
                UpdateConversion();
            }
        }
    }

    public string TargetUnit
    {
        get => _targetUnit;
        set
        {
            if (SetProperty(ref _targetUnit, ResolveUnit(value, _targetUnit)))
            {
                UpdateConversion();
            }
        }
    }

    public string SourceValue
    {
        get => _sourceValue;
        set
        {
            if (SetProperty(ref _sourceValue, value ?? string.Empty))
            {
                UpdateConversion();
            }
        }
    }

    public string TargetValue
    {
        get => _targetValue;
        private set => SetProperty(ref _targetValue, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    private void UpdateConversion()
    {
        if (string.IsNullOrWhiteSpace(SourceValue))
        {
            TargetValue = string.Empty;
            ErrorMessage = string.Empty;
            return;
        }

        try
        {
            TargetValue = _convertValue(SourceValue, SourceUnit, TargetUnit);
            ErrorMessage = string.Empty;
        }
        catch (CalculationException ex)
        {
            TargetValue = string.Empty;
            ErrorMessage = ex.Message;
        }
    }

    private string ResolveInitialUnit(string requestedUnit, int fallbackIndex)
    {
        if (TryFindUnit(requestedUnit, out var resolved))
        {
            return resolved;
        }

        if (Units.Count == 0)
        {
            return string.Empty;
        }

        var index = Math.Clamp(fallbackIndex, 0, Units.Count - 1);
        return Units[index];
    }

    private string ResolveUnit(string? requestedUnit, string fallbackUnit)
    {
        if (TryFindUnit(requestedUnit, out var resolved))
        {
            return resolved;
        }

        return fallbackUnit;
    }

    private bool TryFindUnit(string? requestedUnit, out string resolvedUnit)
    {
        resolvedUnit = Units.FirstOrDefault(unit => string.Equals(unit, requestedUnit, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(resolvedUnit);
    }
}
