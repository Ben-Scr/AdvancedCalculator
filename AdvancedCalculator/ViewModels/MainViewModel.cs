using System.Collections.ObjectModel;
using AdvancedCalculator.Core;
using AdvancedCalculator.Models;
using AdvancedCalculator.Services;

namespace AdvancedCalculator.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly CalculatorService _calculatorService;
    private readonly HistoryService _historyService;
    private readonly SettingsService _settingsService;
    private double? _lastResult;
    private string _expression = string.Empty;
    private string _result = "0";
    private string _errorMessage = string.Empty;
    private int _caretIndex;
    private bool _isHistoryOpen;
    private string _theme = "dark";

    public MainViewModel(
        CalculatorService calculatorService,
        HistoryService historyService,
        SettingsService settingsService,
        HistoryViewModel history)
    {
        _calculatorService = calculatorService;
        _historyService = historyService;
        _settingsService = settingsService;
        History = history;

        EvaluateCommand = new RelayCommand(async _ => await EvaluateAsync());
        ClearCommand = new RelayCommand(_ => Clear());
        BackspaceCommand = new RelayCommand(_ => DeleteBackward());
        ToggleThemeCommand = new RelayCommand(async _ => await ToggleThemeAsync());
        ToggleHistoryCommand = new RelayCommand(_ => IsHistoryOpen = !IsHistoryOpen);
        DeleteHistoryEntryCommand = new RelayCommand(async item => await DeleteHistoryEntryAsync(item as CalculationHistoryItem));
        LoadHistoryEntryCommand = new RelayCommand(item => LoadHistoryEntry(item as CalculationHistoryItem));
        ClearHistoryCommand = new RelayCommand(async _ => await ClearHistoryAsync(), _ => History.HasEntries);

        BuildKeys();
    }

    public ObservableCollection<CalcKey> Keys { get; } = [];

    public HistoryViewModel History { get; }

    public RelayCommand EvaluateCommand { get; }

    public RelayCommand ClearCommand { get; }

    public RelayCommand BackspaceCommand { get; }

    public RelayCommand ToggleThemeCommand { get; }

    public RelayCommand ToggleHistoryCommand { get; }

    public RelayCommand DeleteHistoryEntryCommand { get; }

    public RelayCommand LoadHistoryEntryCommand { get; }

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

    public string ThemeToggleToolTip => Theme == "dark" ? "Helles Design aktivieren" : "Dunkles Design aktivieren";

    public async Task InitializeAsync(UserSettings settings)
    {
        Theme = settings.Theme;

        var historyItems = await _historyService.LoadAsync();
        History.SetEntries(historyItems);
        ClearHistoryCommand.RaiseCanExecuteChanged();
    }

    private void BuildKeys()
    {
        Keys.Clear();

        AddKey("AC", ClearCommand);
        AddKey("DEL", BackspaceCommand);
        AddKey("(", new RelayCommand(_ => InsertOpenParenthesis()));
        AddKey(")", new RelayCommand(_ => InsertText(")")));
        AddKey("%", new RelayCommand(_ => InsertPercent()));

        AddKey("sin", new RelayCommand(_ => InsertFunction("sin")));
        AddKey("cos", new RelayCommand(_ => InsertFunction("cos")));
        AddKey("tan", new RelayCommand(_ => InsertFunction("tan")));
        AddKey("log", new RelayCommand(_ => InsertFunction("log")));
        AddKey("log10", new RelayCommand(_ => InsertFunction("log10")));

        AddKey("sqrt", new RelayCommand(_ => InsertFunction("sqrt")));
        AddKey("pow", new RelayCommand(_ => InsertFunction("pow")));
        AddKey("abs", new RelayCommand(_ => InsertFunction("abs")));
        AddKey("ceil", new RelayCommand(_ => InsertFunction("ceil")));
        AddKey("floor", new RelayCommand(_ => InsertFunction("floor")));

        AddKey("7", new RelayCommand(_ => InsertNumberOrToken("7")));
        AddKey("8", new RelayCommand(_ => InsertNumberOrToken("8")));
        AddKey("9", new RelayCommand(_ => InsertNumberOrToken("9")));
        AddKey("/", new RelayCommand(_ => InsertOperator("/")));
        AddKey("*", new RelayCommand(_ => InsertOperator("*")));

        AddKey("4", new RelayCommand(_ => InsertNumberOrToken("4")));
        AddKey("5", new RelayCommand(_ => InsertNumberOrToken("5")));
        AddKey("6", new RelayCommand(_ => InsertNumberOrToken("6")));
        AddKey("-", new RelayCommand(_ => InsertOperator("-")));
        AddKey("+", new RelayCommand(_ => InsertOperator("+")));

        AddKey("1", new RelayCommand(_ => InsertNumberOrToken("1")));
        AddKey("2", new RelayCommand(_ => InsertNumberOrToken("2")));
        AddKey("3", new RelayCommand(_ => InsertNumberOrToken("3")));
        AddKey("pi", new RelayCommand(_ => InsertConstant("pi")));
        AddKey("e", new RelayCommand(_ => InsertConstant("e")));

        AddKey("0", new RelayCommand(_ => InsertNumberOrToken("0")));
        AddKey(".", new RelayCommand(_ => InsertDecimalPoint()));
        AddKey(",", new RelayCommand(_ => InsertArgumentSeparator()));
        AddKey("Ans", new RelayCommand(_ => InsertConstant("Ans")));
        AddKey("=", EvaluateCommand, isAccent: true);
    }

    private void AddKey(string label, RelayCommand command, bool isAccent = false)
    {
        Keys.Add(new CalcKey
        {
            Label = label,
            Command = command,
            IsAccent = isAccent
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

    private void Clear()
    {
        Expression = string.Empty;
        Result = "0";
        ErrorMessage = string.Empty;
        CaretIndex = 0;
    }

    private void DeleteBackward()
    {
        if (string.IsNullOrEmpty(Expression) || CaretIndex <= 0)
        {
            return;
        }

        var removeLength = 1;
        if (CaretIndex >= 3 &&
            Expression[CaretIndex - 1] == ' ' &&
            IsBinaryOperator(Expression[CaretIndex - 2]) &&
            Expression[CaretIndex - 3] == ' ')
        {
            removeLength = 3;
        }

        Expression = Expression.Remove(CaretIndex - removeLength, removeLength);
        CaretIndex -= removeLength;
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

    private void InsertFunction(string functionName)
    {
        var prefix = NeedsImplicitMultiplication() ? "*" : string.Empty;

        if (functionName.Equals("pow", StringComparison.OrdinalIgnoreCase))
        {
            InsertText($"{prefix}pow(, )", prefix.Length + 4);
            return;
        }

        InsertText($"{prefix}{functionName}(");
    }

    private void InsertConstant(string constant)
    {
        var prefix = NeedsImplicitMultiplication() ? "*" : string.Empty;
        InsertText($"{prefix}{constant}");
    }

    private void InsertOpenParenthesis()
    {
        var prefix = NeedsImplicitMultiplication() ? "*" : string.Empty;
        InsertText($"{prefix}(");
    }

    private void InsertNumberOrToken(string token)
    {
        var prefix = NeedsImplicitMultiplication() ? "*" : string.Empty;
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

        if (previous == ')' || previous == '%')
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

    private bool NeedsImplicitMultiplication()
    {
        var previous = GetPreviousMeaningfulCharacter();
        return previous is not null && (char.IsDigit(previous.Value) || char.IsLetter(previous.Value) || previous == ')' || previous == '%');
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

    private static bool IsBinaryOperator(char value) => value is '+' or '-' or '*' or '/' or '^';
}
