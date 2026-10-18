using System.Globalization;
using System.Numerics;

namespace UnitConverter.Conversion;

public sealed record ConversionResult(
    string CategoryId,
    string FromId,
    string ToId,
    decimal InputValue,
    decimal Value,
    string FormattedValue,
    string InputText,
    string FromName,
    string ToName,
    string FromSymbol,
    string ToSymbol,
    string Formula,
    bool IsRounded);

public sealed class ConversionException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}

/// <summary>
/// Converts an invariant-culture number (dot decimal separator, optional exponent; no grouping).
/// All conversion arithmetic is rational until the final decimal result, avoiding intermediate overflow.
/// Display precision is 0–12 decimal places, with midpoint rounding away from zero.
/// Nonzero results that would display as zero use scientific notation with six significant digits.
/// </summary>
public sealed class ConversionService
{
    public ConversionResult Convert(string categoryId, string fromId, string toId, string value, int precision = 6)
    {
        var category = UnitCatalog.FindCategory(categoryId)
            ?? throw new ConversionException("Category", "Choose a valid conversion category.");
        var from = category.Units.FirstOrDefault(unit => unit.Id == fromId)
            ?? throw new ConversionException("From", "Choose a valid starting unit.");
        var to = category.Units.FirstOrDefault(unit => unit.Id == toId)
            ?? throw new ConversionException("To", "Choose a valid destination unit.");
        if (precision is < 0 or > 12)
            throw new ConversionException("Precision", "Choose between 0 and 12 decimal places.");

        var inputText = value?.Trim() ?? "";
        if (inputText.Length == 0)
            throw new ConversionException("Value", "Enter a value to convert.");
        if (inputText.Length > 128 || !decimal.TryParse(inputText, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var input))
            throw new ConversionException("Value", "Enter a number using a dot for decimals, without separators (for example, 12.5).");

        // Decimal parsing can round numbers finer than its 28-place limit to zero.
        // Reject that case instead of silently treating a nonzero measurement as zero.
        if (input == 0m && HasNonzeroSignificand(inputText))
            throw new ConversionException("Value", "This value is too small. Use a magnitude of at least 0.0000000000000000000000000001.");
        if (input != 0m && Rational.FromInput(inputText) != Rational.From(input))
            throw new ConversionException("Value", "This number has more precision than supported. Shorten it to at most 28 decimal places and 29 significant digits.");

        ValidateRange(category.Id, from.Id, input);
        try
        {
            var exact = category.Id == "temperature"
                ? ConvertTemperature(input, from.Id, to.Id)
                : (Rational.From(input) * Rational.From(from.Scale) + Rational.From(from.Offset) - Rational.From(to.Offset))
                  / Rational.From(to.Scale);
            var converted = exact.ToDecimal();
            if (converted == 0m && exact != Rational.From(0m))
                throw new ConversionException("Value", "The converted value is too small for the supported decimal range. Choose a smaller destination unit or a larger input.");
            var rounded = decimal.Round(converted, precision, MidpointRounding.AwayFromZero);
            var scientific = rounded == 0m && converted != 0m;
            var formatted = scientific
                ? converted.ToString("0.#####E+0", CultureInfo.InvariantCulture)
                : rounded.ToString(precision == 0 ? "0" : "0." + new string('#', precision), CultureInfo.InvariantCulture);
            return new(category.Id, from.Id, to.Id, input, converted, formatted, inputText,
                from.Name, to.Name, from.Symbol, to.Symbol, Formula(category.Id, from, to),
                exact != (scientific ? Rational.FromInput(formatted) : Rational.From(rounded)));
        }
        catch (OverflowException)
        {
            throw new ConversionException("Value", "The converted value is too large. Try a smaller number or a larger destination unit.");
        }
    }

    private static void ValidateRange(string category, string from, decimal input)
    {
        if (category != "temperature" && input < 0m)
            throw new ConversionException("Value", "Enter zero or a positive value for this measurement.");
        if (category == "temperature" && input < (from switch { "c" => -273.15m, "f" => -459.67m, _ => 0m }))
            throw new ConversionException("Value", "Temperature cannot be below absolute zero (−273.15 °C, −459.67 °F, or 0 K).");
    }

    private static Rational ConvertTemperature(decimal input, string from, string to)
    {
        var value = Rational.From(input);
        var celsius = from switch
        {
            "f" => (value - Rational.From(32m)) * Rational.From(5m) / Rational.From(9m),
            "k" => value - Rational.From(273.15m),
            _ => value
        };
        return to switch
        {
            "f" => celsius * Rational.From(9m) / Rational.From(5m) + Rational.From(32m),
            "k" => celsius + Rational.From(273.15m),
            _ => celsius
        };
    }

    private static string Formula(string category, UnitDefinition from, UnitDefinition to)
    {
        if (from.Id == to.Id)
            return $"{to.Symbol} = {from.Symbol}";
        if (category == "temperature")
            return (from.Id, to.Id) switch
            {
                ("c", "f") => "°F = (°C × 9 ÷ 5) + 32",
                ("f", "c") => "°C = (°F − 32) × 5 ÷ 9",
                ("c", "k") => "K = °C + 273.15",
                ("k", "c") => "°C = K − 273.15",
                ("f", "k") => "K = (°F − 32) × 5 ÷ 9 + 273.15",
                _ => "°F = (K − 273.15) × 9 ÷ 5 + 32"
            };

        // Show the ratio of exact base-unit definitions rather than a rounded multiplier.
        var sourceScale = from.Scale.ToString("0.############################", CultureInfo.InvariantCulture);
        var targetScale = to.Scale.ToString("0.############################", CultureInfo.InvariantCulture);
        return to.Scale == 1m
            ? $"{to.Symbol} = {from.Symbol} × {sourceScale}"
            : $"{to.Symbol} = {from.Symbol} × {sourceScale} ÷ {targetScale}";
    }

    private static bool HasNonzeroSignificand(string value)
    {
        foreach (var character in value)
        {
            if (character is 'e' or 'E') break;
            if (character is >= '1' and <= '9') return true;
        }
        return false;
    }

    /// <summary>Small exact-rational helper for decimal input and unit factors.</summary>
    private readonly struct Rational : IEquatable<Rational>
    {
        private static readonly BigInteger Maximum = (BigInteger.One << 96) - 1;
        private readonly BigInteger numerator;
        private readonly BigInteger denominator;

        private Rational(BigInteger numerator, BigInteger denominator)
        {
            var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
            this.numerator = numerator / divisor;
            this.denominator = denominator / divisor;
        }

        public static Rational From(decimal value)
        {
            var bits = decimal.GetBits(value);
            var magnitude = (BigInteger)(uint)bits[0] | (BigInteger)(uint)bits[1] << 32 | (BigInteger)(uint)bits[2] << 64;
            if ((bits[3] & int.MinValue) != 0) magnitude = -magnitude;
            return new(magnitude, BigInteger.Pow(10, (bits[3] >> 16) & 0xff));
        }

        public static Rational FromInput(string input)
        {
            // Numeric syntax and decimal range have already been validated by TryParse.
            var exponentIndex = input.IndexOfAny(['e', 'E']);
            var mantissa = exponentIndex < 0 ? input : input[..exponentIndex];
            var exponent = exponentIndex < 0 ? 0 : int.Parse(input[(exponentIndex + 1)..], CultureInfo.InvariantCulture);
            var decimalIndex = mantissa.IndexOf('.');
            var decimalPlaces = decimalIndex < 0 ? 0 : mantissa.Length - decimalIndex - 1;
            var digits = BigInteger.Parse(mantissa.Replace(".", ""), CultureInfo.InvariantCulture);
            var power = exponent - decimalPlaces;
            return power >= 0
                ? new(digits * BigInteger.Pow(10, power), BigInteger.One)
                : new(digits, BigInteger.Pow(10, -power));
        }

        public decimal ToDecimal()
        {
            var magnitude = BigInteger.Abs(numerator);
            if (magnitude > Maximum * denominator) throw new OverflowException();

            for (var scale = 28; scale >= 0; scale--)
            {
                var quotient = BigInteger.DivRem(magnitude * BigInteger.Pow(10, scale), denominator, out var remainder);
                // The stored decimal uses nearest-even, matching decimal arithmetic.
                var midpoint = (remainder * 2).CompareTo(denominator);
                if (midpoint > 0 || (midpoint == 0 && !quotient.IsEven)) quotient++;
                if (quotient > Maximum) continue;
                return new decimal((int)(uint)(quotient & uint.MaxValue),
                    (int)(uint)((quotient >> 32) & uint.MaxValue),
                    (int)(uint)((quotient >> 64) & uint.MaxValue), numerator.Sign < 0, (byte)scale);
            }
            throw new OverflowException();
        }

        public static Rational operator +(Rational left, Rational right) =>
            new(left.numerator * right.denominator + right.numerator * left.denominator, left.denominator * right.denominator);
        public static Rational operator -(Rational left, Rational right) =>
            new(left.numerator * right.denominator - right.numerator * left.denominator, left.denominator * right.denominator);
        public static Rational operator *(Rational left, Rational right) =>
            new(left.numerator * right.numerator, left.denominator * right.denominator);
        public static Rational operator /(Rational left, Rational right) =>
            new(left.numerator * right.denominator, left.denominator * right.numerator);
        public bool Equals(Rational other) => numerator * other.denominator == other.numerator * denominator;
        public override bool Equals(object? obj) => obj is Rational other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(numerator, denominator);
        public static bool operator ==(Rational left, Rational right) => left.Equals(right);
        public static bool operator !=(Rational left, Rational right) => !left.Equals(right);
    }
}
