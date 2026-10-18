using System.Collections.ObjectModel;

namespace UnitConverter.Conversion;

/// <summary>A unit expressed in the category's base unit: base = value * Scale + Offset.</summary>
public sealed record UnitDefinition(string Id, string Name, string Symbol, decimal Scale, decimal Offset = 0);

public sealed record ConversionCategory(
    string Id,
    string Name,
    string Description,
    string Icon,
    string DefaultFrom,
    string DefaultTo,
    IReadOnlyList<UnitDefinition> Units);

/// <summary>Immutable unit definitions. Volume measures marked US use US customary definitions.</summary>
public static class UnitCatalog
{
    public static IReadOnlyList<ConversionCategory> Categories { get; } = Array.AsReadOnly<ConversionCategory>(
    [
        new("length", "Length", "From millimeters to miles.", "ruler", "m", "ft", Units(
            new("mm", "Millimeter", "mm", 0.001m),
            new("cm", "Centimeter", "cm", 0.01m),
            new("m", "Meter", "m", 1m),
            new("km", "Kilometer", "km", 1000m),
            new("in", "Inch", "in", 0.0254m),
            new("ft", "Foot", "ft", 0.3048m),
            new("yd", "Yard", "yd", 0.9144m),
            new("mi", "Mile", "mi", 1609.344m))),
        new("weight", "Weight", "Everyday weights, precisely converted.", "weight", "kg", "lb", Units(
            new("mg", "Milligram", "mg", 0.000001m),
            new("g", "Gram", "g", 0.001m),
            new("kg", "Kilogram", "kg", 1m),
            new("oz", "Ounce", "oz", 0.028349523125m),
            new("lb", "Pound", "lb", 0.45359237m),
            new("t", "Metric tonne", "t", 1000m))),
        new("temperature", "Temperature", "Celsius, Fahrenheit, and Kelvin.", "thermometer", "c", "f", Units(
            new("c", "Celsius", "°C", 1m, 273.15m),
            // The service evaluates the 5/9 ratio exactly before its final decimal result.
            new("f", "Fahrenheit", "°F", 5m / 9m, 273.15m - 32m * (5m / 9m)),
            new("k", "Kelvin", "K", 1m))),
        new("area", "Area", "Small spaces to wide landscapes.", "area", "m2", "ft2", Units(
            new("mm2", "Square millimeter", "mm²", 0.000001m),
            new("cm2", "Square centimeter", "cm²", 0.0001m),
            new("m2", "Square meter", "m²", 1m),
            new("ha", "Hectare", "ha", 10000m),
            new("km2", "Square kilometer", "km²", 1000000m),
            new("in2", "Square inch", "in²", 0.00064516m),
            new("ft2", "Square foot", "ft²", 0.09290304m),
            new("acre", "Acre", "ac", 4046.8564224m),
            new("mi2", "Square mile", "mi²", 2589988.110336m))),
        new("volume", "Volume", "Metric and US customary measures.", "droplet", "l", "us-gal", Units(
            new("ml", "Milliliter", "mL", 0.001m),
            new("l", "Liter", "L", 1m),
            new("m3", "Cubic meter", "m³", 1000m),
            new("us-tsp", "US teaspoon", "US tsp", 0.00492892159375m),
            new("us-tbsp", "US tablespoon", "US tbsp", 0.01478676478125m),
            new("us-floz", "US fluid ounce", "US fl oz", 0.0295735295625m),
            new("us-cup", "US cup", "US cup", 0.2365882365m),
            new("us-pint", "US pint", "US pt", 0.473176473m),
            new("us-gal", "US gallon", "US gal", 3.785411784m)))
    ]);

    public static ConversionCategory? FindCategory(string? id) =>
        Categories.FirstOrDefault(category => string.Equals(category.Id, id, StringComparison.Ordinal));

    private static ReadOnlyCollection<UnitDefinition> Units(params UnitDefinition[] units) =>
        Array.AsReadOnly(units);
}
