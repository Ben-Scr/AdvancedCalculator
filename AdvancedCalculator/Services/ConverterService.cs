using System.Globalization;
using System.Numerics;

namespace BenScr.AdvancedCalculator.Services;

public enum MeasurementConverterKind
{
    Memory,
    Weight,
    Volume,
    Energy,
    Length
}

public sealed class ConverterService
{
    private static readonly IReadOnlyDictionary<string, int> NumberBases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["Binary"] = 2,
        ["Octal"] = 8,
        ["Decimal"] = 10,
        ["Hexadecimal"] = 16
    };

    private static readonly IReadOnlyDictionary<MeasurementConverterKind, IReadOnlyDictionary<string, double>> MeasurementUnitFactors =
        new Dictionary<MeasurementConverterKind, IReadOnlyDictionary<string, double>>
        {
            [MeasurementConverterKind.Memory] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["B"] = 1d,
                ["KB"] = 1024d,
                ["MB"] = 1024d * 1024d,
                ["GB"] = 1024d * 1024d * 1024d,
                ["TB"] = 1024d * 1024d * 1024d * 1024d
            },
            [MeasurementConverterKind.Weight] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["mg"] = 0.001d,
                ["g"] = 1d,
                ["kg"] = 1000d,
                ["t"] = 1_000_000d,
                ["oz"] = 28.349523125d,
                ["lb"] = 453.59237d,
                ["st"] = 6350.29318d
            },
            [MeasurementConverterKind.Volume] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ml"] = 0.001d,
                ["cl"] = 0.01d,
                ["dl"] = 0.1d,
                ["l"] = 1d,
                ["m³"] = 1000d,
                ["tsp"] = 0.00492892159375d,
                ["tbsp"] = 0.01478676478125d,
                ["fl oz"] = 0.0295735295625d,
                ["cup"] = 0.2365882365d,
                ["pt"] = 0.473176473d,
                ["qt"] = 0.946352946d,
                ["gal"] = 3.785411784d
            },
            [MeasurementConverterKind.Energy] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["J"] = 1d,
                ["kJ"] = 1000d,
                ["MJ"] = 1_000_000d,
                ["cal"] = 4.184d,
                ["kcal"] = 4184d,
                ["Wh"] = 3600d,
                ["kWh"] = 3_600_000d,
                ["eV"] = 1.602176634e-19d,
                ["BTU"] = 1055.05585262d
            },
            [MeasurementConverterKind.Length] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["mm"] = 0.001d,
                ["cm"] = 0.01d,
                ["dm"] = 0.1d,
                ["m"] = 1d,
                ["km"] = 1000d,
                ["in"] = 0.0254d,
                ["ft"] = 0.3048d,
                ["yd"] = 0.9144d,
                ["mi"] = 1609.344d,
                ["nmi"] = 1852d
            }
        };

    public string ConvertNumberSystem(string input, string sourceBase, string targetBase)
    {
        var parsedValue = ParseNumberValue(input, sourceBase);

        if (!NumberBases.TryGetValue(targetBase, out var targetRadix))
        {
            throw new CalculationException($"Unsupported base: '{targetBase}'.");
        }

        return FormatBigInteger(parsedValue, targetRadix);
    }

    public double ParseMeasurementInput(string input, MeasurementConverterKind converterKind)
    {
        var normalized = (input ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(',', '.');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new CalculationException($"Enter a {GetMeasurementPrompt(converterKind)} to convert.");
        }

        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new CalculationException($"Enter a valid {GetMeasurementPrompt(converterKind)}.");
        }

        if (value < 0d)
        {
            throw new CalculationException($"{GetMeasurementTitle(converterKind)} cannot be negative.");
        }

        return value;
    }

    public double ConvertMeasurement(double value, MeasurementConverterKind converterKind, string sourceUnit, string targetUnit)
    {
        var unitFactors = ResolveMeasurementUnits(converterKind);

        if (!unitFactors.TryGetValue(sourceUnit, out var sourceFactor))
        {
            throw new CalculationException($"Unsupported unit: '{sourceUnit}'.");
        }

        if (!unitFactors.TryGetValue(targetUnit, out var targetFactor))
        {
            throw new CalculationException($"Unsupported unit: '{targetUnit}'.");
        }

        return (value * sourceFactor) / targetFactor;
    }

    public string FormatMeasurementValue(double value)
    {
        if (Math.Abs(value) < 1e-12d)
        {
            value = 0d;
        }

        return value.ToString("G15", CultureInfo.InvariantCulture);
    }

    private static IReadOnlyDictionary<string, double> ResolveMeasurementUnits(MeasurementConverterKind converterKind)
    {
        if (MeasurementUnitFactors.TryGetValue(converterKind, out var units))
        {
            return units;
        }

        throw new CalculationException("Unsupported converter.");
    }

    private static string GetMeasurementPrompt(MeasurementConverterKind converterKind) =>
        converterKind switch
        {
            MeasurementConverterKind.Memory => "memory size",
            MeasurementConverterKind.Weight => "weight",
            MeasurementConverterKind.Volume => "volume",
            MeasurementConverterKind.Energy => "power or energy value",
            MeasurementConverterKind.Length => "length",
            _ => "value"
        };

    private static string GetMeasurementTitle(MeasurementConverterKind converterKind) =>
        converterKind switch
        {
            MeasurementConverterKind.Memory => "Memory size",
            MeasurementConverterKind.Weight => "Weight",
            MeasurementConverterKind.Volume => "Volume",
            MeasurementConverterKind.Energy => "Power or energy value",
            MeasurementConverterKind.Length => "Length",
            _ => "Value"
        };

    private static BigInteger ParseNumberValue(string input, string sourceBase)
    {
        if (!NumberBases.TryGetValue(sourceBase, out var radix))
        {
            throw new CalculationException($"Unsupported base: '{sourceBase}'.");
        }

        var normalized = (input ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new CalculationException("Enter a whole number to convert.");
        }

        var sign = 1;
        if (normalized[0] is '+' or '-')
        {
            sign = normalized[0] == '-' ? -1 : 1;
            normalized = normalized[1..];
        }

        normalized = RemoveKnownPrefix(normalized, radix);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new CalculationException("Enter a whole number to convert.");
        }

        if (radix == 10)
        {
            if (!BigInteger.TryParse(
                    sign < 0 ? $"-{normalized}" : normalized,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var decimalValue))
            {
                throw new CalculationException("Enter a valid decimal whole number.");
            }

            return decimalValue;
        }

        var value = BigInteger.Zero;

        foreach (var character in normalized)
        {
            var digit = ParseDigit(character);
            if (digit >= radix)
            {
                throw new CalculationException($"Enter a valid {sourceBase.ToLowerInvariant()} whole number.");
            }

            value = (value * radix) + digit;
        }

        return sign < 0 ? -value : value;
    }

    private static string RemoveKnownPrefix(string value, int radix)
    {
        if (radix == 2 && value.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            return value[2..];
        }

        if (radix == 8 && value.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
        {
            return value[2..];
        }

        if (radix == 16 && value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return value[2..];
        }

        return value;
    }

    private static int ParseDigit(char value)
    {
        if (value is >= '0' and <= '9')
        {
            return value - '0';
        }

        if (value is >= 'a' and <= 'f')
        {
            return 10 + (value - 'a');
        }

        if (value is >= 'A' and <= 'F')
        {
            return 10 + (value - 'A');
        }

        throw new CalculationException("Enter a valid whole number for the selected base.");
    }

    private static string FormatBigInteger(BigInteger value, int radix)
    {
        if (radix == 10)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        if (value.IsZero)
        {
            return "0";
        }

        var isNegative = value.Sign < 0;
        value = BigInteger.Abs(value);
        var digits = new Stack<char>();

        while (value > BigInteger.Zero)
        {
            value = BigInteger.DivRem(value, radix, out var remainder);
            var digit = (int)remainder;
            digits.Push(digit < 10 ? (char)('0' + digit) : (char)('A' + (digit - 10)));
        }

        var result = new string(digits.ToArray());
        return isNegative ? $"-{result}" : result;
    }
}
