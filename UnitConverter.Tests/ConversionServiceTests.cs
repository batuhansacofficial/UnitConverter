using System.Globalization;
using UnitConverter.Conversion;
using Xunit;

namespace UnitConverter.Tests;

public sealed class ConversionServiceTests
{
    private readonly ConversionService service = new();

    [Theory]
    [InlineData("length", "mi", "m", "1", "1609.344")]
    [InlineData("length", "in", "cm", "1", "2.54")]
    [InlineData("length", "yd", "ft", "4", "12")]
    [InlineData("weight", "lb", "kg", "1", "0.45359237")]
    [InlineData("weight", "oz", "g", "1", "28.349523125")]
    [InlineData("weight", "t", "kg", "1", "1000")]
    [InlineData("area", "acre", "m2", "1", "4046.8564224")]
    [InlineData("area", "mi2", "acre", "1", "640")]
    [InlineData("volume", "us-gal", "l", "1", "3.785411784")]
    [InlineData("volume", "us-cup", "us-tbsp", "1", "16")]
    [InlineData("volume", "us-tbsp", "us-tsp", "1", "3")]
    [InlineData("volume", "m3", "l", "1", "1000")]
    public void Converts_reference_values(string category, string from, string to, string input, string expected)
    {
        var result = service.Convert(category, from, to, input, 12);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), result.Value);
    }

    [Theory]
    [InlineData("c", "f", "0", "32")]
    [InlineData("c", "f", "100", "212")]
    [InlineData("f", "c", "32", "0")]
    [InlineData("f", "c", "212", "100")]
    [InlineData("f", "c", "-40", "-40")]
    [InlineData("k", "c", "273.15", "0")]
    [InlineData("k", "f", "273.15", "32")]
    [InlineData("c", "k", "-273.15", "0")]
    [InlineData("f", "k", "-459.67", "0")]
    [InlineData("k", "f", "0", "-459.67")]
    public void Converts_affine_temperature_reference_points_exactly(string from, string to, string input, string expected)
    {
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), service.Convert("temperature", from, to, input).Value);
    }

    [Theory]
    [InlineData("c", "-273.150000000000000000000001")]
    [InlineData("f", "-459.670000000000000000000001")]
    [InlineData("k", "-0.0000000000000000000000000001")]
    public void Rejects_temperatures_even_slightly_below_absolute_zero(string from, string value)
    {
        var error = Assert.Throws<ConversionException>(() => service.Convert("temperature", from, "k", value));
        Assert.Equal("Value", error.Field);
        Assert.Contains("absolute zero", error.Message);
    }

    [Theory]
    [InlineData("length", "m", "ft")]
    [InlineData("weight", "kg", "lb")]
    [InlineData("area", "m2", "acre")]
    [InlineData("volume", "l", "us-gal")]
    public void Rejects_negative_measurements_and_accepts_zero(string category, string from, string to)
    {
        Assert.Equal("Value", Assert.Throws<ConversionException>(() => service.Convert(category, from, to, "-1")).Field);
        Assert.Equal(0m, service.Convert(category, from, to, "0").Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1,5")]
    [InlineData("1,000")]
    [InlineData("1.000,5")]
    [InlineData("1 000")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1e100")]
    [InlineData("1e-29")]
    [InlineData("1.00000000000000000000000000001")]
    [InlineData("<script>alert(1)</script>")]
    public void Rejects_invalid_or_out_of_range_numeric_input(string value)
    {
        Assert.Equal("Value", Assert.Throws<ConversionException>(() => service.Convert("length", "m", "ft", value)).Field);
    }

    [Fact]
    public void Parses_invariant_input_and_formats_invariant_output_under_another_current_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var result = service.Convert("length", "m", "cm", " +1.25e1 ");
            Assert.Equal(12.5m, result.InputValue);
            Assert.Equal("1250", result.FormattedValue);
            Assert.Equal("+1.25e1", result.InputText);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("1.00000000000000000000000000000000000")]
    [InlineData(".1e1")]
    [InlineData("+1.")]
    [InlineData("00001")]
    public void Accepts_equivalent_exact_numeric_forms(string input)
    {
        Assert.Equal(1m, service.Convert("length", "m", "m", input).Value);
    }

    [Fact]
    public void Reports_validation_against_the_relevant_field_and_keeps_units_inside_their_category()
    {
        Assert.Equal("Category", Assert.Throws<ConversionException>(() => service.Convert("unknown", "m", "ft", "1")).Field);
        Assert.Equal("From", Assert.Throws<ConversionException>(() => service.Convert("length", "kg", "ft", "1")).Field);
        Assert.Equal("To", Assert.Throws<ConversionException>(() => service.Convert("length", "m", "kg", "1")).Field);
        Assert.Equal("Precision", Assert.Throws<ConversionException>(() => service.Convert("length", "m", "ft", "1", -1)).Field);
        Assert.Equal("Precision", Assert.Throws<ConversionException>(() => service.Convert("length", "m", "ft", "1", 13)).Field);
    }

    [Fact]
    public void Separates_display_precision_from_the_underlying_result()
    {
        var result = service.Convert("length", "m", "ft", "1", 2);
        Assert.Equal("3.28", result.FormattedValue);
        Assert.True(result.Value > 3.28m);
        Assert.True(result.IsRounded);
        var exact = service.Convert("length", "m", "cm", "1.25", 0);
        Assert.Equal("125", exact.FormattedValue);
        Assert.False(exact.IsRounded);
        Assert.Equal("1", service.Convert("length", "m", "m", "1.25", 0).FormattedValue);
        Assert.Equal("1.3", service.Convert("length", "m", "m", "1.25", 1).FormattedValue);
        Assert.Equal("-1.3", service.Convert("temperature", "c", "c", "-1.25", 1).FormattedValue);
    }

    [Fact]
    public void Preserves_the_maximum_value_when_an_intermediate_base_unit_would_overflow()
    {
        var maximum = decimal.MaxValue.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(decimal.MaxValue, service.Convert("area", "km2", "km2", maximum).Value);
        var result = service.Convert("area", "km2", "mi2", maximum);
        Assert.InRange(result.Value / decimal.MaxValue, 0.38m, 0.39m);
        Assert.Equal(decimal.MaxValue, service.Convert("temperature", "f", "f", maximum).Value);
    }

    [Fact]
    public void Reports_actual_result_overflow_as_input_validation()
    {
        var error = Assert.Throws<ConversionException>(() => service.Convert("length", "km", "mm", decimal.MaxValue.ToString(CultureInfo.InvariantCulture)));
        Assert.Equal("Value", error.Field);
        Assert.Contains("too large", error.Message);
    }

    [Fact]
    public void Rejects_decimal_underflow_instead_of_claiming_exact_zero()
    {
        var error = Assert.Throws<ConversionException>(() => service.Convert("length", "mm", "km", "1e-28", 12));
        Assert.Equal("Value", error.Field);
        Assert.Contains("too small", error.Message);
    }

    [Theory]
    [InlineData("length", "m", "m", "0.000001", "1E-6", false)]
    [InlineData("length", "m", "m", "0.000001234567", "1.23457E-6", true)]
    [InlineData("length", "m", "m", "1e-28", "1E-28", false)]
    [InlineData("temperature", "c", "c", "-0.000001", "-1E-6", false)]
    public void Uses_scientific_notation_when_fixed_precision_would_hide_a_nonzero_result(
        string category, string from, string to, string input, string formatted, bool isRounded)
    {
        var result = service.Convert(category, from, to, input, 2);
        Assert.NotEqual(0m, result.Value);
        Assert.Equal(formatted, result.FormattedValue);
        Assert.Equal(isRounded, result.IsRounded);
    }

    [Fact]
    public void Round_trips_every_unit_pair_within_decimal_precision()
    {
        foreach (var category in UnitCatalog.Categories)
        foreach (var from in category.Units)
        foreach (var to in category.Units)
        {
            const decimal original = 123.456789m;
            var converted = service.Convert(category.Id, from.Id, to.Id, original.ToString(CultureInfo.InvariantCulture));
            var roundTrip = service.Convert(category.Id, to.Id, from.Id, converted.Value.ToString(CultureInfo.InvariantCulture));
            // Converting mm² to mi² produces a very small intermediate decimal;
            // its final decimal-place error is amplified on the return trip.
            Assert.InRange(decimal.Abs(roundTrip.Value - original), 0m, 0.000000000000001m);
        }
    }

    [Fact]
    public void Catalog_has_valid_defaults_positive_scales_unique_ids_and_read_only_collections()
    {
        Assert.Equal(5, UnitCatalog.Categories.Count);
        Assert.Equal(UnitCatalog.Categories.Count, UnitCatalog.Categories.Select(category => category.Id).Distinct().Count());
        Assert.Throws<NotSupportedException>(() => ((IList<ConversionCategory>)UnitCatalog.Categories).Clear());
        foreach (var category in UnitCatalog.Categories)
        {
            Assert.Equal(category.Units.Count, category.Units.Select(unit => unit.Id).Distinct().Count());
            Assert.Contains(category.Units, unit => unit.Id == category.DefaultFrom);
            Assert.Contains(category.Units, unit => unit.Id == category.DefaultTo);
            Assert.All(category.Units, unit => Assert.True(unit.Scale > 0m));
            Assert.Throws<NotSupportedException>(() => ((IList<UnitDefinition>)category.Units).Clear());
        }
        Assert.Null(UnitCatalog.FindCategory("LENGTH"));
        Assert.Null(UnitCatalog.FindCategory(null));
    }
}
