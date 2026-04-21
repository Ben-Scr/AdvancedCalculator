using System.Collections.ObjectModel;
using System.Windows;
using AdvancedCalculator.Core;
using AdvancedCalculator.Models;
using AdvancedCalculator.Services;

namespace AdvancedCalculator.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private const string StandardMode = "Standard";
    private const string ScientificMode = "Scientific";
    private const string ConverterMode = "Converters";
    private const string CalculatorsSection = "Calculators";
    private const string ConvertersSection = "Converters";
    private const string SettingsSection = "Settings";

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
    private string _theme = "dark";
    private string _selectedCalculatorMode = StandardMode;
    private string _selectedNavigationSection = CalculatorsSection;
    private string _numberSystemInput = string.Empty;
    private string _selectedNumberBase = "Binary";
    private string _selectedNumberTargetBase = "Decimal";
    private string _numberSystemOutput = "-";
    private string _numberSystemStatus = "Enter a whole number to see all supported bases.";
    private string _memoryInput = string.Empty;
    private string _selectedMemoryUnit = "MB";
    private string _selectedMemoryTargetUnit = "GB";
    private string _memoryOutput = "-";
    private string _memoryStatus = "Enter a value to convert memory sizes.";

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
        ToggleThemeCommand = new RelayCommand(async _ => await ToggleThemeAsync());
        ToggleHistoryCommand = new RelayCommand(_ => IsHistoryOpen = !IsHistoryOpen);
        SetCalculatorModeCommand = new RelayCommand(mode => SetCalculatorMode(mode as string));
        SetNavigationSectionCommand = new RelayCommand(section => SetNavigationSection(section as string));
        DeleteHistoryEntryCommand = new RelayCommand(async item => await DeleteHistoryEntryAsync(item as CalculationHistoryItem));
        LoadHistoryEntryCommand = new RelayCommand(item => LoadHistoryEntry(item as CalculationHistoryItem));
        CopyHistoryExpressionCommand = new RelayCommand(item => CopyHistoryExpression(item as CalculationHistoryItem));
        CopyHistoryResultCommand = new RelayCommand(item => CopyHistoryResult(item as CalculationHistoryItem));
        ClearHistoryCommand = new RelayCommand(async _ => await ClearHistoryAsync(), _ => History.HasEntries);

        BuildStandardKeys();
        BuildScientificKeys();
        InitializeConverters();
    }

    public ObservableCollection<CalcKey> StandardKeys { get; } = [];

    public ObservableCollection<CalcKey> ScientificKeys { get; } = [];

    public ObservableCollection<string> NumberBaseOptions { get; } =
    [
        "Binary",
        "Octal",
        "Decimal",
        "Hexadecimal"
    ];

    public ObservableCollection<string> MemoryUnitOptions { get; } =
    [
        "B",
        "KB",
        "MB",
        "GB",
        "TB"
    ];

    public HistoryViewModel History { get; }

    public RelayCommand EvaluateCommand { get; }

    public RelayCommand ClearCommand { get; }

    public RelayCommand BackspaceCommand { get; }

    public RelayCommand ToggleThemeCommand { get; }

    public RelayCommand ToggleHistoryCommand { get; }

    public RelayCommand SetCalculatorModeCommand { get; }

    public RelayCommand SetNavigationSectionCommand { get; }

    public RelayCommand DeleteHistoryEntryCommand { get; }

    public RelayCommand LoadHistoryEntryCommand { get; }

    public RelayCommand CopyHistoryExpressionCommand { get; }

    public RelayCommand CopyHistoryResultCommand { get; }

    public RelayCommand ClearHistoryCommand { get; }

    public string Expression
    {
        get => _expression;
        set
        {
            if (!SetProperty(ref _expression, value ?? string.Empty))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(ErrorMessage))
            {
                ErrorMessage = string.Empty;
            }

            if (_caretIndex > _expression.Length)
            {
                CaretIndex = _expression.Length;
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
        set => SetProperty(ref _isHistoryOpen, value);
    }

    public string SelectedCalculatorMode
    {
        get => _selectedCalculatorMode;
        private set
        {
            if (SetProperty(ref _selectedCalculatorMode, value))
            {
                OnPropertyChanged(nameof(IsStandardMode));
                OnPropertyChanged(nameof(IsScientificMode));
                OnPropertyChanged(nameof(IsConvertersMode));
                OnPropertyChanged(nameof(IsCalculatorMode));
            }
        }
    }

    public bool IsStandardMode => SelectedCalculatorMode == StandardMode;

    public bool IsScientificMode => SelectedCalculatorMode == ScientificMode;

    public bool IsConvertersMode => SelectedCalculatorMode == ConverterMode;

    public bool IsCalculatorMode => !IsConvertersMode;

    public string SelectedNavigationSection
    {
        get => _selectedNavigationSection;
        private set
        {
            if (SetProperty(ref _selectedNavigationSection, value))
            {
                OnPropertyChanged(nameof(IsCalculatorsSection));
                OnPropertyChanged(nameof(IsConvertersSection));
                OnPropertyChanged(nameof(IsSettingsSection));
            }
        }
    }

    public bool IsCalculatorsSection => SelectedNavigationSection == CalculatorsSection;

    public bool IsConvertersSection => SelectedNavigationSection == ConvertersSection;

    public bool IsSettingsSection => SelectedNavigationSection == SettingsSection;

    public string NumberSystemInput
    {
        get => _numberSystemInput;
        set
        {
            if (SetProperty(ref _numberSystemInput, value ?? string.Empty))
            {
                UpdateNumberSystemResults();
            }
        }
    }

    public string SelectedNumberBase
    {
        get => _selectedNumberBase;
        set
        {
            if (SetProperty(ref _selectedNumberBase, value))
            {
                UpdateNumberSystemResults();
            }
        }
    }

    public string SelectedNumberTargetBase
    {
        get => _selectedNumberTargetBase;
        set
        {
            if (SetProperty(ref _selectedNumberTargetBase, value))
            {
                UpdateNumberSystemResults();
            }
        }
    }

    public string NumberSystemOutput
    {
        get => _numberSystemOutput;
        private set => SetProperty(ref _numberSystemOutput, value);
    }

    public string NumberSystemStatus
    {
        get => _numberSystemStatus;
        private set => SetProperty(ref _numberSystemStatus, value);
    }

    public string MemoryInput
    {
        get => _memoryInput;
        set
        {
            if (SetProperty(ref _memoryInput, value ?? string.Empty))
            {
                UpdateMemoryResults();
            }
        }
    }

    public string SelectedMemoryUnit
    {
        get => _selectedMemoryUnit;
        set
        {
            if (SetProperty(ref _selectedMemoryUnit, value))
            {
                UpdateMemoryResults();
            }
        }
    }

    public string SelectedMemoryTargetUnit
    {
        get => _selectedMemoryTargetUnit;
        set
        {
            if (SetProperty(ref _selectedMemoryTargetUnit, value))
            {
                UpdateMemoryResults();
            }
        }
    }

    public string MemoryOutput
    {
        get => _memoryOutput;
        private set => SetProperty(ref _memoryOutput, value);
    }

    public string MemoryStatus
    {
        get => _memoryStatus;
        private set => SetProperty(ref _memoryStatus, value);
    }

    public string Theme
    {
        get => _theme;
        private set
        {
            if (SetProperty(ref _theme, value))
            {
                OnPropertyChanged(nameof(ThemeToggleGlyph));
                OnPropertyChanged(nameof(ThemeToggleToolTip));
            }
        }
    }

    public string ThemeToggleGlyph => Theme == "dark" ? "\u2600" : "\u263E";

    public string ThemeToggleToolTip => Theme == "dark" ? "Switch to light theme" : "Switch to dark theme";

    public async Task InitializeAsync(UserSettings settings)
    {
        Theme = settings.Theme;

        var historyItems = await _historyService.LoadAsync();
        History.SetEntries(historyItems);
        ClearHistoryCommand.RaiseCanExecuteChanged();
    }

    private void InitializeConverters()
    {
        UpdateNumberSystemResults();
        UpdateMemoryResults();
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
        AddKey(StandardKeys, "^", new RelayCommand(_ => InsertOperator("^")));
        AddKey(StandardKeys, "+", new RelayCommand(_ => InsertOperator("+")));

        AddPlaceholder(StandardKeys);
        AddPlaceholder(StandardKeys);
        AddPlaceholder(StandardKeys);
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

    private void SetCalculatorMode(string? mode)
    {
        if (string.Equals(mode, ScientificMode, StringComparison.OrdinalIgnoreCase))
        {
            SelectedCalculatorMode = ScientificMode;
            SelectedNavigationSection = CalculatorsSection;
            return;
        }

        if (string.Equals(mode, ConverterMode, StringComparison.OrdinalIgnoreCase))
        {
            SelectedCalculatorMode = ConverterMode;
            SelectedNavigationSection = ConvertersSection;
            return;
        }

        SelectedCalculatorMode = StandardMode;
        SelectedNavigationSection = CalculatorsSection;
    }

    private void SetNavigationSection(string? section)
    {
        if (string.Equals(section, ConvertersSection, StringComparison.OrdinalIgnoreCase))
        {
            SelectedNavigationSection = ConvertersSection;
            return;
        }

        if (string.Equals(section, SettingsSection, StringComparison.OrdinalIgnoreCase))
        {
            SelectedNavigationSection = SettingsSection;
            return;
        }

        SelectedNavigationSection = CalculatorsSection;
    }

    private void UpdateNumberSystemResults()
    {
        if (string.IsNullOrWhiteSpace(NumberSystemInput))
        {
            NumberSystemOutput = "-";
            NumberSystemStatus = "Enter a whole number to see all supported bases.";
            return;
        }

        try
        {
            NumberSystemOutput = _converterService.ConvertNumberSystem(NumberSystemInput, SelectedNumberBase, SelectedNumberTargetBase);
            NumberSystemStatus = $"Converting from {SelectedNumberBase} to {SelectedNumberTargetBase}.";
        }
        catch (CalculationException ex)
        {
            NumberSystemOutput = "-";
            NumberSystemStatus = ex.Message;
        }
    }

    private void UpdateMemoryResults()
    {
        if (string.IsNullOrWhiteSpace(MemoryInput))
        {
            MemoryOutput = "-";
            MemoryStatus = "Enter a value to convert memory sizes.";
            return;
        }

        try
        {
            MemoryOutput = _converterService.ConvertMemorySize(MemoryInput, SelectedMemoryUnit, SelectedMemoryTargetUnit);
            MemoryStatus = $"Converting from {SelectedMemoryUnit} to {SelectedMemoryTargetUnit} using 1024-based units.";
        }
        catch (CalculationException ex)
        {
            MemoryOutput = "-";
            MemoryStatus = ex.Message;
        }
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

    private async Task ToggleThemeAsync()
    {
        Theme = Theme == "dark" ? "light" : "dark";
        App.ApplyTheme(Theme);
        await _settingsService.SaveAsync(new UserSettings { Theme = Theme });
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

        Expression = item.Expression;
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

    private static bool IsBinaryOperator(char value) => value is '+' or '-' or '*' or '/' or '^';
}
