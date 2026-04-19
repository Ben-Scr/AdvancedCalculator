using System.Globalization;

namespace AdvancedCalculator.Services;

public sealed class CalculatorService
{
    private static readonly Dictionary<string, int[]> FunctionArity = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sin"] = [1],
        ["cos"] = [1],
        ["tan"] = [1],
        ["asin"] = [1],
        ["acos"] = [1],
        ["atan"] = [1],
        ["sqrt"] = [1],
        ["log"] = [1, 2],
        ["log10"] = [1],
        ["abs"] = [1],
        ["ceil"] = [1],
        ["floor"] = [1],
        ["round"] = [1, 2],
        ["pow"] = [2]
    };

    private static readonly string[] KnownFunctions = FunctionArity.Keys.OrderBy(static name => name).ToArray();

    public double Evaluate(string expression, double? ans = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return 0d;
        }

        var parser = new Parser(expression, ans);
        return parser.Parse();
    }

    public string FormatResult(double value)
    {
        if (Math.Abs(value) < 1e-12d)
        {
            value = 0d;
        }

        return value.ToString("G15", CultureInfo.InvariantCulture);
    }

    private static int GetLevenshteinDistance(string left, string right)
    {
        var matrix = new int[left.Length + 1, right.Length + 1];

        for (var i = 0; i <= left.Length; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= right.Length; j++)
        {
            matrix[0, j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[left.Length, right.Length];
    }

    private static string BuildUnknownFunctionMessage(string functionName)
    {
        var suggestion = KnownFunctions
            .Select(candidate => new { Candidate = candidate, Distance = GetLevenshteinDistance(functionName, candidate) })
            .OrderBy(item => item.Distance)
            .ThenBy(item => item.Candidate.Length)
            .FirstOrDefault();

        if (suggestion is not null && suggestion.Distance <= 2)
        {
            return $"Unbekannte Funktion: '{functionName}'. Meintest du '{suggestion.Candidate}'?";
        }

        return $"Unbekannte Funktion: '{functionName}'.";
    }

    private static void EnsureFinite(double value, string? functionName = null)
    {
        if (!double.IsNaN(value) && !double.IsInfinity(value))
        {
            return;
        }

        throw new CalculationException(functionName is null
            ? "Ergebnis ist nicht definiert."
            : $"Ungültiger Wert für '{functionName}'.");
    }

    private static double ApplyFunction(string functionName, IReadOnlyList<double> arguments)
    {
        var name = functionName.ToLowerInvariant();

        if (!FunctionArity.TryGetValue(name, out var validArities) || !validArities.Contains(arguments.Count))
        {
            if (validArities is null)
            {
                throw new CalculationException(BuildUnknownFunctionMessage(functionName));
            }

            var expected = validArities.Length == 1
                ? validArities[0].ToString(CultureInfo.InvariantCulture)
                : string.Join(" oder ", validArities.Select(static value => value.ToString(CultureInfo.InvariantCulture)));

            throw new CalculationException($"Funktion '{functionName}' erwartet {expected} Argument(e).");
        }

        double result = name switch
        {
            "sin" => Math.Sin(arguments[0]),
            "cos" => Math.Cos(arguments[0]),
            "tan" => Math.Tan(arguments[0]),
            "asin" => ApplySingleArgumentValidated(functionName, arguments[0], value =>
            {
                if (value is < -1d or > 1d)
                {
                    throw new CalculationException($"Ungültiger Wert für '{functionName}'.");
                }

                return Math.Asin(value);
            }),
            "acos" => ApplySingleArgumentValidated(functionName, arguments[0], value =>
            {
                if (value is < -1d or > 1d)
                {
                    throw new CalculationException($"Ungültiger Wert für '{functionName}'.");
                }

                return Math.Acos(value);
            }),
            "atan" => Math.Atan(arguments[0]),
            "sqrt" => ApplySingleArgumentValidated(functionName, arguments[0], value =>
            {
                if (value < 0d)
                {
                    throw new CalculationException($"Ungültiger Wert für '{functionName}'.");
                }

                return Math.Sqrt(value);
            }),
            "log" => ApplyLog(arguments, functionName),
            "log10" => ApplySingleArgumentValidated(functionName, arguments[0], value =>
            {
                if (value <= 0d)
                {
                    throw new CalculationException($"Ungültiger Wert für '{functionName}'.");
                }

                return Math.Log10(value);
            }),
            "abs" => Math.Abs(arguments[0]),
            "ceil" => Math.Ceiling(arguments[0]),
            "floor" => Math.Floor(arguments[0]),
            "round" => ApplyRound(arguments, functionName),
            "pow" => Math.Pow(arguments[0], arguments[1]),
            _ => throw new CalculationException(BuildUnknownFunctionMessage(functionName))
        };

        EnsureFinite(result, functionName);
        return result;
    }

    private static double ApplyRound(IReadOnlyList<double> arguments, string functionName)
    {
        if (arguments.Count == 1)
        {
            return Math.Round(arguments[0], MidpointRounding.AwayFromZero);
        }

        var digits = arguments[1];
        if (digits < 0d || digits > 15d || Math.Abs(digits - Math.Round(digits)) > 1e-9d)
        {
            throw new CalculationException($"Ungültiger Wert für '{functionName}'.");
        }

        return Math.Round(arguments[0], (int)Math.Round(digits), MidpointRounding.AwayFromZero);
    }

    private static double ApplyLog(IReadOnlyList<double> arguments, string functionName)
    {
        if (arguments[0] <= 0d)
        {
            throw new CalculationException($"Ungültiger Wert für '{functionName}'.");
        }

        if (arguments.Count == 1)
        {
            return Math.Log(arguments[0]);
        }

        if (arguments[1] <= 0d || Math.Abs(arguments[1] - 1d) < 1e-12d)
        {
            throw new CalculationException($"Ungültiger Wert für '{functionName}'.");
        }

        return Math.Log(arguments[0], arguments[1]);
    }

    private static double ApplySingleArgumentValidated(string functionName, double value, Func<double, double> evaluator)
    {
        var result = evaluator(value);
        EnsureFinite(result, functionName);
        return result;
    }

    private sealed class Parser
    {
        private readonly string _expression;
        private readonly double? _ans;
        private int _position;

        public Parser(string expression, double? ans)
        {
            _expression = expression;
            _ans = ans;
        }

        public double Parse()
        {
            SkipWhiteSpace();
            var value = ParseExpression();
            SkipWhiteSpace();

            if (!IsAtEnd)
            {
                if (Current == ')')
                {
                    throw new CalculationException("Unerwartete schließende Klammer ')'.");
                }

                throw new CalculationException($"Unerwartetes Zeichen: '{Current}'.");
            }

            EnsureFinite(value);
            return value;
        }

        private bool IsAtEnd => _position >= _expression.Length;

        private char Current => IsAtEnd ? '\0' : _expression[_position];

        private double ParseExpression()
        {
            var left = ParseTerm();

            while (true)
            {
                SkipWhiteSpace();

                if (Match('+'))
                {
                    left += ParseTerm();
                    continue;
                }

                if (Match('-'))
                {
                    left -= ParseTerm();
                    continue;
                }

                return left;
            }
        }

        private double ParseTerm()
        {
            var left = ParseUnary();

            while (true)
            {
                SkipWhiteSpace();

                if (Match('*'))
                {
                    left *= ParseUnary();
                    EnsureFinite(left);
                    continue;
                }

                if (Match('/'))
                {
                    var divisor = ParseUnary();
                    if (Math.Abs(divisor) < 1e-12d)
                    {
                        throw new CalculationException("Division durch 0 ist nicht erlaubt.");
                    }

                    left /= divisor;
                    EnsureFinite(left);
                    continue;
                }

                return left;
            }
        }

        private double ParseUnary()
        {
            SkipWhiteSpace();

            if (Match('+'))
            {
                return ParseUnary();
            }

            if (Match('-'))
            {
                return -ParseUnary();
            }

            return ParsePower();
        }

        private double ParsePower()
        {
            var left = ParsePostfix();
            SkipWhiteSpace();

            if (!Match('^'))
            {
                return left;
            }

            var exponent = ParseUnary();
            var result = Math.Pow(left, exponent);
            EnsureFinite(result);
            return result;
        }

        private double ParsePostfix()
        {
            var value = ParsePrimary();

            while (true)
            {
                SkipWhiteSpace();

                if (!Match('%'))
                {
                    return value;
                }

                value /= 100d;
            }
        }

        private double ParsePrimary()
        {
            SkipWhiteSpace();

            if (IsAtEnd)
            {
                throw new CalculationException("Der Ausdruck ist unvollständig.");
            }

            if (Match('('))
            {
                var value = ParseExpression();
                SkipWhiteSpace();

                if (!Match(')'))
                {
                    throw new CalculationException("Fehlende schließende Klammer ')'.");
                }

                return value;
            }

            if (Current == ')')
            {
                throw new CalculationException("Unerwartete schließende Klammer ')'.");
            }

            if (char.IsDigit(Current) || Current == '.')
            {
                return ParseNumber();
            }

            if (char.IsLetter(Current))
            {
                return ParseIdentifier();
            }

            throw new CalculationException($"Unerwartetes Zeichen: '{Current}'.");
        }

        private double ParseNumber()
        {
            var start = _position;
            var hasDot = false;

            while (!IsAtEnd && (char.IsDigit(Current) || Current == '.'))
            {
                if (Current == '.')
                {
                    if (hasDot)
                    {
                        Advance();
                        while (!IsAtEnd && (char.IsDigit(Current) || Current == '.'))
                        {
                            Advance();
                        }

                        var invalidNumber = _expression[start.._position];
                        throw new CalculationException($"Ungültige Zahl: '{invalidNumber}'.");
                    }

                    hasDot = true;
                }

                Advance();
            }

            var token = _expression[start.._position];
            if (token == "." || !double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new CalculationException($"Ungültige Zahl: '{token}'.");
            }

            return value;
        }

        private double ParseIdentifier()
        {
            var start = _position;

            while (!IsAtEnd && (char.IsLetterOrDigit(Current) || Current == '_'))
            {
                Advance();
            }

            var identifier = _expression[start.._position];
            SkipWhiteSpace();

            if (Match('('))
            {
                if (!FunctionArity.ContainsKey(identifier))
                {
                    throw new CalculationException(BuildUnknownFunctionMessage(identifier));
                }

                var arguments = new List<double>();
                SkipWhiteSpace();

                if (!Match(')'))
                {
                    while (true)
                    {
                        arguments.Add(ParseExpression());
                        SkipWhiteSpace();

                        if (Match(')'))
                        {
                            break;
                        }

                        if (!Match(','))
                        {
                            throw new CalculationException("Fehlende schließende Klammer ')'.");
                        }

                        SkipWhiteSpace();
                    }
                }

                return ApplyFunction(identifier, arguments);
            }

            if (identifier.Equals("pi", StringComparison.OrdinalIgnoreCase))
            {
                return Math.PI;
            }

            if (identifier.Equals("e", StringComparison.OrdinalIgnoreCase))
            {
                return Math.E;
            }

            if (identifier.Equals("ans", StringComparison.OrdinalIgnoreCase))
            {
                return _ans ?? 0d;
            }

            throw new CalculationException($"Unbekannte Konstante oder Funktion: '{identifier}'.");
        }

        private void SkipWhiteSpace()
        {
            while (!IsAtEnd && char.IsWhiteSpace(Current))
            {
                Advance();
            }
        }

        private bool Match(char character)
        {
            if (Current != character)
            {
                return false;
            }

            Advance();
            return true;
        }

        private void Advance()
        {
            _position++;
        }
    }
}

public sealed class CalculationException : Exception
{
    public CalculationException(string message)
        : base(message)
    {
    }
}
