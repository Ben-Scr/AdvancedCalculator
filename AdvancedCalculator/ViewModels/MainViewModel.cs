using System.Collections.ObjectModel;
using System.Windows;
using BenScr.AdvancedCalculator.Core;
using BenScr.AdvancedCalculator.Models;
using BenScr.AdvancedCalculator.Services;

namespace BenScr.AdvancedCalculator.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private const string CalculatorPage = "Calculator";
    private const string ConverterPage = "Converter";
    private const string SettingsPage = "Settings";
    private const string StandardMode = "Standard";
    private const string ScientificMode = "Scientific";
    private const string NumberConverterKey = "Number";
    private const string MemoryConverterKey = "Memory";
    private const string WeightConverterKey = "Weight";
    private const string VolumeConverterKey = "Volume";
    private const string EnergyConverterKey = "Energy";
    private const string LengthConverterKey = "Length";
    private const string NumberConverterDefaultStatus = "Type in any number field to update the other bases.";
    private const string MemoryConverterDefaultStatus = "Type in any memory field to update the other units.";
    private const string WeightConverterDefaultStatus = "Type in any weight field to update the other units.";
    private const string VolumeConverterDefaultStatus = "Type in any volume field to update the other units.";
    private const string EnergyConverterDefaultStatus = "Type in any power or energy field to update the other units.";
    private const string LengthConverterDefaultStatus = "Type in any length field to update the other units.";

    private static readonly IReadOnlyList<string> NumberConverterUnits = ["Binary", "Octal", "Decimal", "Hexadecimal"];
    private static readonly IReadOnlyList<string> MemoryConverterUnits = ["B", "KB", "MB", "GB", "TB"];
    private static readonly IReadOnlyList<string> WeightConverterUnits = ["mg", "g", "kg", "t", "oz", "lb", "st"];
    private static readonly IReadOnlyList<string> VolumeConverterUnits = ["ml", "cl", "dl", "l", "m³", "tsp", "tbsp", "fl oz", "cup", "pt", "qt", "gal"];
    private static readonly IReadOnlyList<string> EnergyConverterUnits = ["J", "kJ", "MJ", "cal", "kcal", "Wh", "kWh", "eV", "BTU"];
    private static readonly IReadOnlyList<string> LengthConverterUnits = ["mm", "cm", "dm", "m", "km", "in", "ft", "yd", "mi", "nmi"];

    private readonly CalculatorService _calculatorService;
    private readonly ConverterService _converterService;
    private readonly HistoryService _historyService;
    private readonly SettingsService _settingsService;
    private double? _lastResult;
    private string _expression = string.Empty;
    private string _result = "0";
    private string _errorMessage = string.Empty;
    private int _caretIndex;
    private bool _isHistoryOpen;
    private string _selectedNavigationPage = CalculatorPage;
    private string _selectedCalculatorMode = StandardMode;
    private string? _activeConverterKey;
    private string _theme = "dark";
    private bool _liveCalculationEnabled = true;
    private bool _suppressLiveCalculation;
    private string _numberConverterStatus = NumberConverterDefaultStatus;
    private string _memoryConverterStatus = MemoryConverterDefaultStatus;
    private string _weightConverterStatus = WeightConverterDefaultStatus;
    private string _volumeConverterStatus = VolumeConverterDefaultStatus;
    private string _energyConverterStatus = EnergyConverterDefaultStatus;
    private string _lengthConverterStatus = LengthConverterDefaultStatus;

    public MainViewModel(
        CalculatorService calculatorService,
        ConverterService converterService,
        HistoryService historyService,
        SettingsService settingsService,
        HistoryViewModel history)
    {
        _calculatorService = calculatorService;
        _converterService = converterService;
        _historyService = historyService;
        _settingsService = settingsService;
        History = history;

        EvaluateCommand = new RelayCommand(async _ => await EvaluateAsync());
        ClearCommand = new RelayCommand(_ => Clear());
        BackspaceCommand = new RelayCommand(_ => DeleteBackward());
        ToggleHistoryCommand = new RelayCommand(_ => IsHistoryOpen = !IsHistoryOpen);
        CloseHistoryCommand = new RelayCommand(_ => IsHistoryOpen = false, _ => IsHistoryOpen);
        SetNavigationPageCommand = new RelayCommand(page => SetNavigationPage(page as string));
        SetCalculatorModeCommand = new RelayCommand(mode => SelectedCalculatorMode = mode as string ?? StandardMode);
        OpenToolCommand = new RelayCommand(tool => OpenTool(tool as string));
        OpenConverterCommand = new RelayCommand(converterKey => OpenConverter(converterKey as string));
        CloseConverterCommand = new RelayCommand(_ => ActiveConverterKey = null, _ => HasActiveConverter);
        DeleteHistoryEntryCommand = new RelayCommand(async item => await DeleteHistoryEntryAsync(item as CalculationHistoryItem));
        LoadHistoryEntryCommand = new RelayCommand(item => LoadHistoryEntry(item as CalculationHistoryItem));
        CopyHistoryExpressionCommand = new RelayCommand(item => CopyHistoryExpression(item as CalculationHistoryItem));
        CopyHistoryResultCommand = new RelayCommand(item => CopyHistoryResult(item as CalculationHistoryItem));
        ClearHistoryCommand = new RelayCommand(async _ => await ClearHistoryAsync(), _ => History.HasEntries);
        SetThemeCommand = new RelayCommand(async theme => await SetThemeAsync(theme as string));
        SetLiveCalculationCommand = new RelayCommand(async value => await SetLiveCalculationAsync(value));

        InitializeUnitConverters();
        BuildStandardKeys();
        BuildScientificKeys();
    }

    public ObservableCollection<CalcKey> StandardKeys { get; } = [];

    public ObservableCollection<CalcKey> ScientificKeys { get; } = [];

    public ObservableCollection<string> CalculatorModeOptions { get; } = [StandardMode, ScientificMode];

    public UnitConverterViewModel NumberConverter { get; private set; } = null!;

    public UnitConverterViewModel MemoryConverter { get; private set; } = null!;

    public UnitConverterViewModel WeightConverter { get; private set; } = null!;

    public UnitConverterViewModel VolumeConverter { get; private set; } = null!;

    public UnitConverterViewModel EnergyConverter { get; private set; } = null!;

    public UnitConverterViewModel LengthConverter { get; private set; } = null!;

    public ObservableCollection<ConverterFieldViewModel> NumberConverterFields { get; } = [];

    public ObservableCollection<ConverterFieldViewModel> MemoryConverterFields { get; } = [];

    public ObservableCollection<ConverterFieldViewModel> WeightConverterFields { get; } = [];

    public ObservableCollection<ConverterFieldViewModel> VolumeConverterFields { get; } = [];

    public ObservableCollection<ConverterFieldViewModel> EnergyConverterFields { get; } = [];

    public ObservableCollection<ConverterFieldViewModel> LengthConverterFields { get; } = [];

    public HistoryViewModel History { get; }

    public RelayCommand EvaluateCommand { get; }

    public RelayCommand ClearCommand { get; }

    public RelayCommand BackspaceCommand { get; }

    public RelayCommand ToggleHistoryCommand { get; }

    public RelayCommand CloseHistoryCommand { get; }

    public RelayCommand SetNavigationPageCommand { get; }

    public RelayCommand SetCalculatorModeCommand { get; }

    public RelayCommand OpenToolCommand { get; }

    public RelayCommand OpenConverterCommand { get; }

    public RelayCommand CloseConverterCommand { get; }

    public RelayCommand DeleteHistoryEntryCommand { get; }

    public RelayCommand LoadHistoryEntryCommand { get; }

    public RelayCommand CopyHistoryExpressionCommand { get; }

    public RelayCommand CopyHistoryResultCommand { get; }

    public RelayCommand ClearHistoryCommand { get; }

    public RelayCommand SetThemeCommand { get; }

    public RelayCommand SetLiveCalculationCommand { get; }

    public string Expression
    {
        get => _expression;
        set
        {
            if (!SetProperty(ref _expression, value ?? string.Empty))
            {
                return;
            }

            if (_caretIndex > _expression.Length)
            {
                CaretIndex = _expression.Length;
            }

            if (_suppressLiveCalculation)
            {
                return;
            }

            if (IsLiveCalculationEnabled)
            {
                EvaluateLiveExpression();
            }
            else if (!string.IsNullOrWhiteSpace(ErrorMessage))
            {
                ErrorMessage = string.Empty;
            }
        }
    }

    public string Result
    {
        get => _result;
        private set => SetProperty(ref _result, value);
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

    public int CaretIndex
    {
        get => _caretIndex;
        set
        {
            var clamped = Math.Clamp(value, 0, Expression.Length);
            SetProperty(ref _caretIndex, clamped);
        }
    }

    public bool IsHistoryOpen
    {
        get => _isHistoryOpen;
        set
        {
            if (SetProperty(ref _isHistoryOpen, value))
            {
                CloseHistoryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedNavigationPage
    {
        get => _selectedNavigationPage;
        private set
        {
            var normalized = NormalizeNavigationPage(value);
            if (!SetProperty(ref _selectedNavigationPage, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(IsCalculatorPage));
            OnPropertyChanged(nameof(IsConverterPage));
            OnPropertyChanged(nameof(IsSettingsPage));
            OnPropertyChanged(nameof(IsConverterSelectionVisible));
            OnPropertyChanged(nameof(CurrentToolTitle));
        }
    }

    public bool IsCalculatorPage => SelectedNavigationPage == CalculatorPage;

    public bool IsConverterPage => SelectedNavigationPage == ConverterPage;

    public bool IsSettingsPage => SelectedNavigationPage == SettingsPage;

    public string CurrentToolTitle =>
        SelectedNavigationPage switch
        {
            ConverterPage when HasActiveConverter => ActiveConverterTitle,
            ConverterPage => "Converters",
            SettingsPage => "Settings",
            _ => CalculatorHeaderTitle
        };

    public string SelectedCalculatorMode
    {
        get => _selectedCalculatorMode;
        set
        {
            var normalized = NormalizeCalculatorMode(value);
            if (!SetProperty(ref _selectedCalculatorMode, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(IsStandardMode));
            OnPropertyChanged(nameof(IsScientificMode));
            OnPropertyChanged(nameof(CurrentToolTitle));
            OnPropertyChanged(nameof(CalculatorHeaderTitle));
            OnPropertyChanged(nameof(CalculatorModeLabel));
        }
    }

    public bool IsStandardMode => SelectedCalculatorMode == StandardMode;

    public bool IsScientificMode => SelectedCalculatorMode == ScientificMode;

    public string CalculatorHeaderTitle => IsScientificMode ? "Scientific Calculator" : "Calculator";

    public string CalculatorModeLabel => $"{SelectedCalculatorMode} mode";

    public string? ActiveConverterKey
    {
        get => _activeConverterKey;
        private set
        {
            var normalized = NormalizeConverterKey(value);
            if (!SetProperty(ref _activeConverterKey, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(HasActiveConverter));
            OnPropertyChanged(nameof(IsConverterSelectionVisible));
            OnPropertyChanged(nameof(ActiveConverterTitle));
            OnPropertyChanged(nameof(IsNumberConverterActive));
            OnPropertyChanged(nameof(IsMemoryConverterActive));
            OnPropertyChanged(nameof(IsWeightConverterActive));
            OnPropertyChanged(nameof(IsVolumeConverterActive));
            OnPropertyChanged(nameof(IsEnergyConverterActive));
            OnPropertyChanged(nameof(IsLengthConverterActive));
            OnPropertyChanged(nameof(ActiveUnitConverter));
            OnPropertyChanged(nameof(CurrentToolTitle));
            CloseConverterCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasActiveConverter => !string.IsNullOrWhiteSpace(ActiveConverterKey);

    public bool IsConverterSelectionVisible => IsConverterPage && !HasActiveConverter;

    public bool IsNumberConverterActive => ActiveConverterKey == NumberConverterKey;

    public bool IsMemoryConverterActive => ActiveConverterKey == MemoryConverterKey;

    public bool IsWeightConverterActive => ActiveConverterKey == WeightConverterKey;

    public bool IsVolumeConverterActive => ActiveConverterKey == VolumeConverterKey;

    public bool IsEnergyConverterActive => ActiveConverterKey == EnergyConverterKey;

    public bool IsLengthConverterActive => ActiveConverterKey == LengthConverterKey;

    public UnitConverterViewModel? ActiveUnitConverter =>
        ActiveConverterKey switch
        {
            NumberConverterKey => NumberConverter,
            MemoryConverterKey => MemoryConverter,
            WeightConverterKey => WeightConverter,
            VolumeConverterKey => VolumeConverter,
            EnergyConverterKey => EnergyConverter,
            LengthConverterKey => LengthConverter,
            _ => null
        };

    public string ActiveConverterTitle =>
        ActiveConverterKey switch
        {
            NumberConverterKey => "Number Converter",
            MemoryConverterKey => "Memory Converter",
            WeightConverterKey => "Weight Converter",
            VolumeConverterKey => "Volume Converter",
            EnergyConverterKey => "Power / Energy Converter",
            LengthConverterKey => "Length Converter",
            _ => "Converters"
        };

    public string Theme
    {
        get => _theme;
        private set
        {
            var normalized = NormalizeTheme(value);
            if (!SetProperty(ref _theme, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(IsLightTheme));
        }
    }

    public bool IsDarkTheme => Theme == "dark";

    public bool IsLightTheme => Theme == "light";

    public bool IsLiveCalculationEnabled
    {
        get => _liveCalculationEnabled;
        private set
        {
            if (!SetProperty(ref _liveCalculationEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsLiveCalculationDisabled));
        }
    }

    public bool IsLiveCalculationDisabled => !IsLiveCalculationEnabled;

    public string NumberConverterStatus
    {
        get => _numberConverterStatus;
        private set => SetProperty(ref _numberConverterStatus, value);
    }

    public string MemoryConverterStatus
    {
        get => _memoryConverterStatus;
        private set => SetProperty(ref _memoryConverterStatus, value);
    }

    public string WeightConverterStatus
    {
        get => _weightConverterStatus;
        private set => SetProperty(ref _weightConverterStatus, value);
    }

    public string VolumeConverterStatus
    {
        get => _volumeConverterStatus;
        private set => SetProperty(ref _volumeConverterStatus, value);
    }

    public string EnergyConverterStatus
    {
        get => _energyConverterStatus;
        private set => SetProperty(ref _energyConverterStatus, value);
    }

    public string LengthConverterStatus
    {
        get => _lengthConverterStatus;
        private set => SetProperty(ref _lengthConverterStatus, value);
    }

    public async Task InitializeAsync(UserSettings settings)
    {
        Theme = settings.Theme;
        IsLiveCalculationEnabled = settings.LiveCalculationEnabled;

        var historyItems = await _historyService.LoadAsync();
        History.SetEntries(historyItems);
        ClearHistoryCommand.RaiseCanExecuteChanged();

        if (IsLiveCalculationEnabled)
        {
            EvaluateLiveExpression();
        }
    }

    private void InitializeUnitConverters()
    {
        NumberConverter = new UnitConverterViewModel(
            "Number Converter",
            NumberConverterUnits,
            "Decimal",
            "Binary",
            (input, sourceUnit, targetUnit) => _converterService.ConvertNumberSystem(input, sourceUnit, targetUnit));

        MemoryConverter = CreateMeasurementConverter("Memory Converter", MemoryConverterUnits, MeasurementConverterKind.Memory, "MB", "GB");
        WeightConverter = CreateMeasurementConverter("Weight Converter", WeightConverterUnits, MeasurementConverterKind.Weight, "kg", "lb");
        VolumeConverter = CreateMeasurementConverter("Volume Converter", VolumeConverterUnits, MeasurementConverterKind.Volume, "l", "gal");
        EnergyConverter = CreateMeasurementConverter("Power / Energy Converter", EnergyConverterUnits, MeasurementConverterKind.Energy, "Wh", "kWh");
        LengthConverter = CreateMeasurementConverter("Length Converter", LengthConverterUnits, MeasurementConverterKind.Length, "m", "ft");
    }

    private UnitConverterViewModel CreateMeasurementConverter(
        string title,
        IReadOnlyList<string> units,
        MeasurementConverterKind converterKind,
        string defaultSourceUnit,
        string defaultTargetUnit)
    {
        return new UnitConverterViewModel(
            title,
            units,
            defaultSourceUnit,
            defaultTargetUnit,
            (input, sourceUnit, targetUnit) =>
            {
                var parsedValue = _converterService.ParseMeasurementInput(input, converterKind);
                var convertedValue = _converterService.ConvertMeasurement(parsedValue, converterKind, sourceUnit, targetUnit);
                return _converterService.FormatMeasurementValue(convertedValue);
            });
    }

    private void InitializeConverters()
    {
        PopulateFields(NumberConverterFields, NumberConverterUnits, UpdateNumberConverterFields);
        PopulateFields(MemoryConverterFields, MemoryConverterUnits, (field, value) =>
            UpdateMeasurementConverterFields(field, value, MeasurementConverterKind.Memory, MemoryConverterFields, MemoryConverterDefaultStatus, status => MemoryConverterStatus = status));
        PopulateFields(WeightConverterFields, WeightConverterUnits, (field, value) =>
            UpdateMeasurementConverterFields(field, value, MeasurementConverterKind.Weight, WeightConverterFields, WeightConverterDefaultStatus, status => WeightConverterStatus = status));
        PopulateFields(VolumeConverterFields, VolumeConverterUnits, (field, value) =>
            UpdateMeasurementConverterFields(field, value, MeasurementConverterKind.Volume, VolumeConverterFields, VolumeConverterDefaultStatus, status => VolumeConverterStatus = status));
        PopulateFields(EnergyConverterFields, EnergyConverterUnits, (field, value) =>
            UpdateMeasurementConverterFields(field, value, MeasurementConverterKind.Energy, EnergyConverterFields, EnergyConverterDefaultStatus, status => EnergyConverterStatus = status));
        PopulateFields(LengthConverterFields, LengthConverterUnits, (field, value) =>
            UpdateMeasurementConverterFields(field, value, MeasurementConverterKind.Length, LengthConverterFields, LengthConverterDefaultStatus, status => LengthConverterStatus = status));

        NumberConverterStatus = NumberConverterDefaultStatus;
        MemoryConverterStatus = MemoryConverterDefaultStatus;
        WeightConverterStatus = WeightConverterDefaultStatus;
        VolumeConverterStatus = VolumeConverterDefaultStatus;
        EnergyConverterStatus = EnergyConverterDefaultStatus;
        LengthConverterStatus = LengthConverterDefaultStatus;
    }

    private static void PopulateFields(
        ObservableCollection<ConverterFieldViewModel> fields,
        IEnumerable<string> unitLabels,
        Action<ConverterFieldViewModel, string> valueChangedCallback)
    {
        fields.Clear();

        foreach (var unitLabel in unitLabels)
        {
            fields.Add(new ConverterFieldViewModel(unitLabel, unitLabel, valueChangedCallback));
        }
    }

    private void UpdateNumberConverterFields(ConverterFieldViewModel sourceField, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ClearOtherFields(NumberConverterFields, sourceField);
            NumberConverterStatus = NumberConverterDefaultStatus;
            return;
        }

        try
        {
            foreach (var field in NumberConverterFields)
            {
                if (ReferenceEquals(field, sourceField))
                {
                    continue;
                }

                field.SetValueSilently(_converterService.ConvertNumberSystem(value, sourceField.UnitKey, field.UnitKey));
            }

            NumberConverterStatus = $"Converted from {sourceField.Label}.";
        }
        catch (CalculationException ex)
        {
            ClearOtherFields(NumberConverterFields, sourceField);
            NumberConverterStatus = ex.Message;
        }
    }

    private void UpdateMeasurementConverterFields(
        ConverterFieldViewModel sourceField,
        string value,
        MeasurementConverterKind converterKind,
        IReadOnlyList<ConverterFieldViewModel> allFields,
        string defaultStatus,
        Action<string> setStatus)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ClearOtherFields(allFields, sourceField);
            setStatus(defaultStatus);
            return;
        }

        try
        {
            var parsedValue = _converterService.ParseMeasurementInput(value, converterKind);

            foreach (var field in allFields)
            {
                if (ReferenceEquals(field, sourceField))
                {
                    continue;
                }

                var converted = _converterService.ConvertMeasurement(parsedValue, converterKind, sourceField.UnitKey, field.UnitKey);
                field.SetValueSilently(_converterService.FormatMeasurementValue(converted));
            }

            setStatus($"Converted from {sourceField.Label}.");
        }
        catch (CalculationException ex)
        {
            ClearOtherFields(allFields, sourceField);
            setStatus(ex.Message);
        }
    }

    private static void ClearOtherFields(IReadOnlyList<ConverterFieldViewModel> fields, ConverterFieldViewModel sourceField)
    {
        foreach (var field in fields)
        {
            if (!ReferenceEquals(field, sourceField))
            {
                field.SetValueSilently(string.Empty);
            }
        }
    }

    private void BuildScientificKeys()
    {
        ScientificKeys.Clear();

        AddKey(ScientificKeys, "AC", ClearCommand);
        AddKey(ScientificKeys, "DEL", BackspaceCommand);
        AddKey(ScientificKeys, "(", new RelayCommand(_ => InsertOpenParenthesis()));
        AddKey(ScientificKeys, ")", new RelayCommand(_ => InsertText(")")));
        AddKey(ScientificKeys, "%", new RelayCommand(_ => InsertPercent()));

        AddKey(ScientificKeys, "sin", new RelayCommand(_ => InsertFunction("sin")));
        AddKey(ScientificKeys, "cos", new RelayCommand(_ => InsertFunction("cos")));
        AddKey(ScientificKeys, "tan", new RelayCommand(_ => InsertFunction("tan")));
        AddKey(ScientificKeys, "log", new RelayCommand(_ => InsertFunction("log")));
        AddKey(ScientificKeys, "log10", new RelayCommand(_ => InsertFunction("log10")));

        AddKey(ScientificKeys, "sqrt", new RelayCommand(_ => InsertFunction("sqrt")));
        AddKey(ScientificKeys, "pow", new RelayCommand(_ => InsertFunction("pow")));
        AddKey(ScientificKeys, "abs", new RelayCommand(_ => InsertFunction("abs")));
        AddKey(ScientificKeys, "ceil", new RelayCommand(_ => InsertFunction("ceil")));
        AddKey(ScientificKeys, "floor", new RelayCommand(_ => InsertFunction("floor")));

        AddKey(ScientificKeys, "7", new RelayCommand(_ => InsertNumberOrToken("7")));
        AddKey(ScientificKeys, "8", new RelayCommand(_ => InsertNumberOrToken("8")));
        AddKey(ScientificKeys, "9", new RelayCommand(_ => InsertNumberOrToken("9")));
        AddKey(ScientificKeys, "/", new RelayCommand(_ => InsertOperator("/")));
        AddKey(ScientificKeys, "*", new RelayCommand(_ => InsertOperator("*")));

        AddKey(ScientificKeys, "4", new RelayCommand(_ => InsertNumberOrToken("4")));
        AddKey(ScientificKeys, "5", new RelayCommand(_ => InsertNumberOrToken("5")));
        AddKey(ScientificKeys, "6", new RelayCommand(_ => InsertNumberOrToken("6")));
        AddKey(ScientificKeys, "-", new RelayCommand(_ => InsertOperator("-")));
        AddKey(ScientificKeys, "+", new RelayCommand(_ => InsertOperator("+")));

        AddKey(ScientificKeys, "1", new RelayCommand(_ => InsertNumberOrToken("1")));
        AddKey(ScientificKeys, "2", new RelayCommand(_ => InsertNumberOrToken("2")));
        AddKey(ScientificKeys, "3", new RelayCommand(_ => InsertNumberOrToken("3")));
        AddKey(ScientificKeys, "pi", new RelayCommand(_ => InsertConstant("pi")));
        AddKey(ScientificKeys, "e", new RelayCommand(_ => InsertConstant("e")));

        AddKey(ScientificKeys, "0", new RelayCommand(_ => InsertNumberOrToken("0")));
        AddKey(ScientificKeys, ".", new RelayCommand(_ => InsertDecimalPoint()));
        AddKey(ScientificKeys, ",", new RelayCommand(_ => InsertArgumentSeparator()));
        AddKey(ScientificKeys, "Ans", new RelayCommand(_ => InsertConstant("Ans")));
        AddKey(ScientificKeys, "=", EvaluateCommand, isAccent: true);
    }

    private void BuildStandardKeys()
    {
        StandardKeys.Clear();

        AddKey(StandardKeys, "AC", ClearCommand);
        AddKey(StandardKeys, "DEL", BackspaceCommand);
        AddKey(StandardKeys, "(", new RelayCommand(_ => InsertOpenParenthesis()));
        AddKey(StandardKeys, ")", new RelayCommand(_ => InsertText(")")));

        AddKey(StandardKeys, "7", new RelayCommand(_ => InsertNumberOrToken("7")));
        AddKey(StandardKeys, "8", new RelayCommand(_ => InsertNumberOrToken("8")));
        AddKey(StandardKeys, "9", new RelayCommand(_ => InsertNumberOrToken("9")));
        AddKey(StandardKeys, "/", new RelayCommand(_ => InsertOperator("/")));

        AddKey(StandardKeys, "4", new RelayCommand(_ => InsertNumberOrToken("4")));
        AddKey(StandardKeys, "5", new RelayCommand(_ => InsertNumberOrToken("5")));
        AddKey(StandardKeys, "6", new RelayCommand(_ => InsertNumberOrToken("6")));
        AddKey(StandardKeys, "*", new RelayCommand(_ => InsertOperator("*")));

        AddKey(StandardKeys, "1", new RelayCommand(_ => InsertNumberOrToken("1")));
        AddKey(StandardKeys, "2", new RelayCommand(_ => InsertNumberOrToken("2")));
        AddKey(StandardKeys, "3", new RelayCommand(_ => InsertNumberOrToken("3")));
        AddKey(StandardKeys, "-", new RelayCommand(_ => InsertOperator("-")));

        AddKey(StandardKeys, "0", new RelayCommand(_ => InsertNumberOrToken("0")));
        AddKey(StandardKeys, ".", new RelayCommand(_ => InsertDecimalPoint()));
        AddKey(StandardKeys, "+", new RelayCommand(_ => InsertOperator("+")));
        AddKey(StandardKeys, "=", EvaluateCommand, isAccent: true);
    }

    private static void AddKey(ICollection<CalcKey> keys, string label, RelayCommand command, bool isAccent = false)
    {
        keys.Add(new CalcKey
        {
            Label = label,
            Command = command,
            IsAccent = isAccent
        });
    }

    private static void AddPlaceholder(ICollection<CalcKey> keys)
    {
        keys.Add(new CalcKey
        {
            IsEnabled = false,
            IsPlaceholder = true
        });
    }

    private async Task EvaluateAsync()
    {
        var expression = Expression.Trim();

        if (string.IsNullOrWhiteSpace(expression))
        {
            Result = "0";
            ErrorMessage = string.Empty;
            return;
        }

        try
        {
            var value = _calculatorService.Evaluate(expression, _lastResult);
            var formattedResult = _calculatorService.FormatResult(value);

            _lastResult = value;
            Result = formattedResult;
            ErrorMessage = string.Empty;

            History.Add(new CalculationHistoryItem
            {
                Expression = expression,
                Result = formattedResult,
                Timestamp = DateTimeOffset.Now
            });

            ClearHistoryCommand.RaiseCanExecuteChanged();
            await _historyService.SaveAsync(History.ToList());
        }
        catch (CalculationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void EvaluateLiveExpression()
    {
        var expression = Expression.Trim();

        if (string.IsNullOrWhiteSpace(expression))
        {
            Result = "0";
            ErrorMessage = string.Empty;
            return;
        }

        try
        {
            var value = _calculatorService.Evaluate(expression, _lastResult);
            Result = _calculatorService.FormatResult(value);
            ErrorMessage = string.Empty;
        }
        catch (CalculationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void Clear()
    {
        Expression = string.Empty;
        Result = "0";
        ErrorMessage = string.Empty;
        _lastResult = null;
        CaretIndex = 0;
    }

    private void DeleteBackward()
    {
        if (string.IsNullOrEmpty(Expression) || CaretIndex <= 0)
        {
            return;
        }

        var removeStart = CaretIndex - 1;
        var removeLength = 1;

        if (TryGetGroupedBinaryOperatorSpanToLeft(CaretIndex, out var groupedOperatorStart, out var groupedOperatorEnd))
        {
            removeStart = groupedOperatorStart;
            removeLength = groupedOperatorEnd - groupedOperatorStart;
        }

        Expression = Expression.Remove(removeStart, removeLength);
        CaretIndex = removeStart;
    }

    private void SetNavigationPage(string? page)
    {
        SelectedNavigationPage = page ?? CalculatorPage;
        ActiveConverterKey = null;
        IsHistoryOpen = false;
    }

    private void OpenTool(string? tool)
    {
        switch (tool?.Trim().ToLowerInvariant())
        {
            case "calculator.standard":
                SelectedCalculatorMode = StandardMode;
                SelectedNavigationPage = CalculatorPage;
                ActiveConverterKey = null;
                break;
            case "calculator.scientific":
                SelectedCalculatorMode = ScientificMode;
                SelectedNavigationPage = CalculatorPage;
                ActiveConverterKey = null;
                break;
            case "converter.number":
                SelectedNavigationPage = ConverterPage;
                ActiveConverterKey = NumberConverterKey;
                break;
            case "converter.memory":
                SelectedNavigationPage = ConverterPage;
                ActiveConverterKey = MemoryConverterKey;
                break;
            case "converter.weight":
                SelectedNavigationPage = ConverterPage;
                ActiveConverterKey = WeightConverterKey;
                break;
            case "converter.volume":
                SelectedNavigationPage = ConverterPage;
                ActiveConverterKey = VolumeConverterKey;
                break;
            case "converter.energy":
                SelectedNavigationPage = ConverterPage;
                ActiveConverterKey = EnergyConverterKey;
                break;
            case "converter.length":
                SelectedNavigationPage = ConverterPage;
                ActiveConverterKey = LengthConverterKey;
                break;
            case "settings":
                SelectedNavigationPage = SettingsPage;
                ActiveConverterKey = null;
                break;
            default:
                SelectedNavigationPage = CalculatorPage;
                ActiveConverterKey = null;
                break;
        }

        IsHistoryOpen = false;
    }

    private void OpenConverter(string? converterKey)
    {
        SelectedNavigationPage = ConverterPage;
        ActiveConverterKey = converterKey;
        IsHistoryOpen = false;
    }

    private async Task SetThemeAsync(string? theme)
    {
        var normalized = NormalizeTheme(theme);
        if (Theme == normalized)
        {
            return;
        }

        Theme = normalized;
        App.ApplyTheme(Theme);
        await SaveCurrentSettingsAsync();
    }

    private async Task SetLiveCalculationAsync(object? value)
    {
        var isEnabled = value switch
        {
            bool booleanValue => booleanValue,
            string stringValue when bool.TryParse(stringValue, out var parsedValue) => parsedValue,
            _ => IsLiveCalculationEnabled
        };

        if (IsLiveCalculationEnabled == isEnabled)
        {
            return;
        }

        IsLiveCalculationEnabled = isEnabled;

        if (IsLiveCalculationEnabled)
        {
            EvaluateLiveExpression();
        }
        else
        {
            ErrorMessage = string.Empty;
        }

        await SaveCurrentSettingsAsync();
    }

    private async Task SaveCurrentSettingsAsync()
    {
        await _settingsService.SaveAsync(new UserSettings
        {
            Theme = Theme,
            LiveCalculationEnabled = IsLiveCalculationEnabled
        });
    }

    private async Task DeleteHistoryEntryAsync(CalculationHistoryItem? item)
    {
        History.Remove(item);
        ClearHistoryCommand.RaiseCanExecuteChanged();
        await _historyService.SaveAsync(History.ToList());
    }

    private void LoadHistoryEntry(CalculationHistoryItem? item)
    {
        if (item is null)
        {
            return;
        }

        _suppressLiveCalculation = true;
        Expression = item.Expression;
        _suppressLiveCalculation = false;

        Result = item.Result;
        ErrorMessage = string.Empty;
        CaretIndex = Expression.Length;

        if (double.TryParse(item.Result, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedResult))
        {
            _lastResult = parsedResult;
        }
    }

    private async Task ClearHistoryAsync()
    {
        History.Clear();
        ClearHistoryCommand.RaiseCanExecuteChanged();
        await _historyService.SaveAsync(History.ToList());
    }

    private void CopyHistoryExpression(CalculationHistoryItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Expression))
        {
            return;
        }

        Clipboard.SetText(item.Expression);
    }

    private void CopyHistoryResult(CalculationHistoryItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Result))
        {
            return;
        }

        Clipboard.SetText(item.Result);
    }

    private void InsertFunction(string functionName)
    {
        var prefix = NeedsImplicitMultiplicationBeforeValue() ? "*" : string.Empty;

        if (functionName.Equals("pow", StringComparison.OrdinalIgnoreCase))
        {
            InsertText($"{prefix}pow(, )", prefix.Length + 4);
            return;
        }

        InsertText($"{prefix}{functionName}(");
    }

    private void InsertConstant(string constant)
    {
        var prefix = NeedsImplicitMultiplicationBeforeValue() ? "*" : string.Empty;
        InsertText($"{prefix}{constant}");
    }

    private void InsertOpenParenthesis()
    {
        var prefix = NeedsImplicitMultiplicationBeforeValue() ? "*" : string.Empty;
        InsertText($"{prefix}(");
    }

    private void InsertNumberOrToken(string token)
    {
        var prefix = NeedsImplicitMultiplicationBeforeNumber() ? "*" : string.Empty;
        InsertText($"{prefix}{token}");
    }

    private void InsertDecimalPoint()
    {
        if (CurrentNumberContainsDecimalPoint())
        {
            return;
        }

        var previous = GetPreviousMeaningfulCharacter();
        if (previous is null || IsBinaryOperator(previous.Value) || previous == '(' || previous == ',')
        {
            InsertText("0.");
            return;
        }

        if (previous == ')' || previous == '%' || char.IsLetter(previous.Value))
        {
            InsertText("*0.");
            return;
        }

        InsertText(".");
    }

    private void InsertArgumentSeparator()
    {
        InsertText(", ");
    }

    private void InsertPercent()
    {
        var previous = GetPreviousMeaningfulCharacter();
        if (previous is null || IsBinaryOperator(previous.Value) || previous == '(' || previous == ',')
        {
            return;
        }

        InsertText("%");
    }

    private void InsertOperator(string op)
    {
        var previous = GetPreviousMeaningfulCharacter();

        if (previous is null)
        {
            if (op == "-")
            {
                InsertText("-");
            }

            return;
        }

        if (IsBinaryOperator(previous.Value) || previous == '(' || previous == ',')
        {
            if (op == "-")
            {
                InsertText("-");
            }

            return;
        }

        InsertText($" {op} ");
    }

    private void InsertText(string text, int? caretOffset = null)
    {
        var insertAt = Math.Clamp(CaretIndex, 0, Expression.Length);
        Expression = Expression.Insert(insertAt, text);
        CaretIndex = insertAt + (caretOffset ?? text.Length);
    }

    private bool NeedsImplicitMultiplicationBeforeValue()
    {
        var previous = GetPreviousMeaningfulCharacter();
        return previous is not null &&
               (char.IsDigit(previous.Value) ||
                char.IsLetter(previous.Value) ||
                previous == ')' ||
                previous == '%' ||
                previous == '.');
    }

    private bool NeedsImplicitMultiplicationBeforeNumber()
    {
        var previous = GetPreviousMeaningfulCharacter();
        return previous is not null && (char.IsLetter(previous.Value) || previous == ')' || previous == '%');
    }

    private bool CurrentNumberContainsDecimalPoint()
    {
        if (string.IsNullOrEmpty(Expression))
        {
            return false;
        }

        var left = CaretIndex - 1;
        while (left >= 0 && (char.IsDigit(Expression[left]) || Expression[left] == '.'))
        {
            left--;
        }

        var right = CaretIndex;
        while (right < Expression.Length && (char.IsDigit(Expression[right]) || Expression[right] == '.'))
        {
            right++;
        }

        if (right <= left + 1)
        {
            return false;
        }

        var token = Expression[(left + 1)..right];
        return token.Contains('.');
    }

    private char? GetPreviousMeaningfulCharacter()
    {
        if (string.IsNullOrEmpty(Expression))
        {
            return null;
        }

        for (var index = Math.Min(CaretIndex - 1, Expression.Length - 1); index >= 0; index--)
        {
            if (!char.IsWhiteSpace(Expression[index]))
            {
                return Expression[index];
            }
        }

        return null;
    }

    private bool TryGetGroupedBinaryOperatorSpanToLeft(int caretIndex, out int start, out int end)
    {
        start = 0;
        end = 0;

        if (string.IsNullOrEmpty(Expression) || caretIndex <= 0)
        {
            return false;
        }

        var probe = caretIndex - 1;
        while (probe >= 0 && char.IsWhiteSpace(Expression[probe]))
        {
            probe--;
        }

        if (probe < 0 || !IsGroupedBinaryOperatorAt(probe))
        {
            return false;
        }

        start = probe;
        while (start > 0 && char.IsWhiteSpace(Expression[start - 1]))
        {
            start--;
        }

        end = probe + 1;
        while (end < Expression.Length && char.IsWhiteSpace(Expression[end]))
        {
            end++;
        }

        return true;
    }

    private bool IsGroupedBinaryOperatorAt(int index)
    {
        if (!IsBinaryOperator(Expression[index]))
        {
            return false;
        }

        var hasWhitespaceBefore = index > 0 && char.IsWhiteSpace(Expression[index - 1]);
        var hasWhitespaceAfter = index + 1 < Expression.Length && char.IsWhiteSpace(Expression[index + 1]);
        return hasWhitespaceBefore && hasWhitespaceAfter;
    }

    private static string NormalizeNavigationPage(string? value)
    {
        if (string.Equals(value, ConverterPage, StringComparison.OrdinalIgnoreCase))
        {
            return ConverterPage;
        }

        if (string.Equals(value, SettingsPage, StringComparison.OrdinalIgnoreCase))
        {
            return SettingsPage;
        }

        return CalculatorPage;
    }

    private static string NormalizeCalculatorMode(string? value) =>
        string.Equals(value, ScientificMode, StringComparison.OrdinalIgnoreCase)
            ? ScientificMode
            : StandardMode;

    private static string? NormalizeConverterKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (string.Equals(value, NumberConverterKey, StringComparison.OrdinalIgnoreCase))
        {
            return NumberConverterKey;
        }

        if (string.Equals(value, MemoryConverterKey, StringComparison.OrdinalIgnoreCase))
        {
            return MemoryConverterKey;
        }

        if (string.Equals(value, WeightConverterKey, StringComparison.OrdinalIgnoreCase))
        {
            return WeightConverterKey;
        }

        if (string.Equals(value, VolumeConverterKey, StringComparison.OrdinalIgnoreCase))
        {
            return VolumeConverterKey;
        }

        if (string.Equals(value, EnergyConverterKey, StringComparison.OrdinalIgnoreCase))
        {
            return EnergyConverterKey;
        }

        if (string.Equals(value, LengthConverterKey, StringComparison.OrdinalIgnoreCase))
        {
            return LengthConverterKey;
        }

        return null;
    }

    private static string NormalizeTheme(string? theme) =>
        string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase)
            ? "light"
            : "dark";

    private static bool IsBinaryOperator(char value) => value is '+' or '-' or '*' or '/' or '^';
}
