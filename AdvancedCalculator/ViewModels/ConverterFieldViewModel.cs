using BenScr.AdvancedCalculator.Core;

namespace BenScr.AdvancedCalculator.ViewModels;

public sealed class ConverterFieldViewModel : ObservableObject
{
    private readonly Action<ConverterFieldViewModel, string> _valueChangedCallback;
    private bool _suppressValueChangedCallback;
    private string _value = string.Empty;

    public ConverterFieldViewModel(string label, string unitKey, Action<ConverterFieldViewModel, string> valueChangedCallback)
    {
        Label = label;
        UnitKey = unitKey;
        _valueChangedCallback = valueChangedCallback;
    }

    public string Label { get; }

    public string UnitKey { get; }

    public string Value
    {
        get => _value;
        set
        {
            if (!SetProperty(ref _value, value ?? string.Empty) || _suppressValueChangedCallback)
            {
                return;
            }

            _valueChangedCallback(this, _value);
        }
    }

    public void SetValueSilently(string value)
    {
        value ??= string.Empty;

        if (string.Equals(_value, value, StringComparison.Ordinal))
        {
            return;
        }

        _suppressValueChangedCallback = true;
        _value = value;
        OnPropertyChanged(nameof(Value));
        _suppressValueChangedCallback = false;
    }
}
