using System.Globalization;
using System.Numerics;
using BenScr.Converter;

namespace AdvancedCalculator.Services;

public sealed class ConverterService
{
    private static readonly IReadOnlyDictionary<string, int> NumberBases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["Binary"] = 2,
        ["Octal"] = 8,
        ["Decimal"] = 10,
        ["Hexadecimal"] = 16
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

    public string ConvertMemorySize(string input, string sourceUnit, string targetUnit)
    {
        var sourceMemoryUnit = ParseMemoryUnit(sourceUnit);
        var targetMemoryUnit = ParseMemoryUnit(targetUnit);
        var parsedValue = ParseMemoryValue(input);
        var converter = new UnitConverter<MemoryUnit, ulong>(sourceMemoryUnit, parsedValue);
        return FormatDouble(converter.To(targetMemoryUnit));
    }

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

    private static MemoryUnit ParseMemoryUnit(string sourceUnit)
    {
        if (Enum.TryParse<MemoryUnit>(sourceUnit, ignoreCase: true, out var unit))
        {
            return unit;
        }

        throw new CalculationException($"Unsupported memory unit: '{sourceUnit}'.");
    }

    private static double ParseMemoryValue(string input)
    {
        var normalized = (input ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(',', '.');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new CalculationException("Enter a memory size to convert.");
        }

        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new CalculationException("Enter a valid memory size.");
        }

        if (value < 0d)
        {
            throw new CalculationException("Memory size cannot be negative.");
        }

        return value;
    }

    private static string FormatDouble(double value)
    {
        if (Math.Abs(value) < 1e-12d)
        {
            value = 0d;
        }

        return value.ToString("0.############", CultureInfo.InvariantCulture);
    }

    private enum MemoryUnit : ulong
    {
        B = 1UL,
        KB = 1024UL,
        MB = 1024UL * 1024UL,
        GB = 1024UL * 1024UL * 1024UL,
        TB = 1024UL * 1024UL * 1024UL * 1024UL
    }
}
