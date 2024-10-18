using Microsoft.AspNetCore.Mvc;
using UnitConverter.Models;

namespace UnitConverter.Controllers
{
    public class ConversionController : Controller
    {
        private static readonly Dictionary<string, double> lengthConversionFactors = new Dictionary<string, double>
        {
            { "millimeter", 0.001 },
            { "centimeter", 0.01 },
            { "meter", 1 },
            { "kilometer", 1000 },
            { "inch", 0.0254 },
            { "foot", 0.3048 },
            { "yard", 0.9144 },
            { "mile", 1609.34 }
        };

        private static readonly Dictionary<string, double> weightConversionFactors = new Dictionary<string, double>
        {
            { "milligram", 0.001 },
            { "gram", 1 },
            { "kilogram", 1000 },
            { "ounce", 28.3495 },
            { "pound", 453.592 }
        };

        private static readonly Dictionary<string, double> volumeConversionFactors = new Dictionary<string, double>
        {
            { "milliliter", 0.001 },
            { "liter", 1 },
            { "cubic_meter", 1000 },
            { "gallon", 3.78541 },
            { "quart", 0.946353 },
            { "pint", 0.473176 },
            { "cup", 0.24 },
            { "fluid_ounce", 0.0295735 }
        };

        private static readonly Dictionary<string, double> areaConversionFactors = new Dictionary<string, double>
        {
            { "square_millimeter", 0.000001 },
            { "square_centimeter", 0.0001 },
            { "square_meter", 1 },
            { "square_kilometer", 1000000 },
            { "square_inch", 0.00064516 },
            { "square_foot", 0.092903 },
            { "square_yard", 0.836127 },
            { "acre", 4046.86 },
            { "hectare", 10000 }
        };

        public IActionResult Area()
        {
            return View(new ConversionViewModel { ConversionType = ConversionType.Area });
        }
        
        public IActionResult Length()
        {
            return View(new ConversionViewModel { ConversionType = ConversionType.Length });
        }

        public IActionResult Temperature()
        {
            return View(new ConversionViewModel { ConversionType = ConversionType.Temperature });
        }

        public IActionResult Volume()
        {
            return View(new ConversionViewModel { ConversionType = ConversionType.Volume });
        }

        public IActionResult Weight()
        {
            return View(new ConversionViewModel { ConversionType = ConversionType.Weight });
        }



        [HttpPost]
        public IActionResult Convert(ConversionViewModel model)
        {
            {
                if (ModelState.IsValid)
                {
                    model.Result = ConvertUnits(model.InputValue, model.FromUnit, model.ToUnit, model.ConversionType.ToString());
                }

                return View(model.ConversionType.ToString(), model);
            }
        }

        private double? ConvertUnits(double inputValue, string fromUnit, string toUnit, string conversionType)
        {
            switch (conversionType)
            {
                case "Length":
                    return ConvertLength(inputValue, fromUnit, toUnit);
                case "Weight":
                    return ConvertWeight(inputValue, fromUnit, toUnit);
                case "Temperature":
                    return ConvertTemperature(inputValue, fromUnit, toUnit);
                case "Volume":
                    return ConvertVolume(inputValue, fromUnit, toUnit);
                case "Area":
                    return ConvertArea(inputValue, fromUnit, toUnit);
                default:
                    return null;
            }
        }

        private double? ConvertLength(double inputValue, string fromUnit, string toUnit)
        {
            if (lengthConversionFactors.TryGetValue(fromUnit, out double fromFactor) && lengthConversionFactors.TryGetValue(toUnit, out double toFactor))
            {
                double valueInMeters = inputValue * fromFactor;
                return valueInMeters / toFactor;
            }

            return null;
        }

        private double? ConvertWeight(double inputValue, string fromUnit, string toUnit)
        {
            if (weightConversionFactors.TryGetValue(fromUnit, out double fromFactor) && weightConversionFactors.TryGetValue(toUnit, out double toFactor))
            {
                double valueInGrams = inputValue * fromFactor;
                return valueInGrams / toFactor;
            }

            return null;
        }

        private double? ConvertTemperature(double inputValue, string fromUnit, string toUnit)
        {
            if (fromUnit == toUnit)
            {
                return inputValue;
            }

            double valueInCelsius;

            // Convert from the source unit to Celsius
            switch (fromUnit)
            {
                case "Celsius":
                    valueInCelsius = inputValue;
                    break;
                case "Fahrenheit":
                    valueInCelsius = (inputValue - 32) * 5 / 9;
                    break;
                case "Kelvin":
                    valueInCelsius = inputValue - 273.15;
                    break;
                default:
                    return null;
            }

            // Convert from Celsius to the target unit
            switch (toUnit)
            {
                case "Celsius":
                    return valueInCelsius;
                case "Fahrenheit":
                    return valueInCelsius * 9 / 5 + 32;
                case "Kelvin":
                    return valueInCelsius + 273.15;
                default:
                    return null;
            }
        }

        private double? ConvertVolume(double inputValue, string fromUnit, string toUnit)
        {
            if (volumeConversionFactors.TryGetValue(fromUnit, out double fromFactor) && volumeConversionFactors.TryGetValue(toUnit, out double toFactor))
            {
                double valueInLiters = inputValue * fromFactor;
                return valueInLiters / toFactor;
            }

            return null;
        }

        private double? ConvertArea(double inputValue, string fromUnit, string toUnit)
        {
            if (areaConversionFactors.TryGetValue(fromUnit, out double fromFactor) && areaConversionFactors.TryGetValue(toUnit, out double toFactor))
            {
                double valueInSquareMeters = inputValue * fromFactor;
                return valueInSquareMeters / toFactor;
            }

            return null;
        }
    }
}