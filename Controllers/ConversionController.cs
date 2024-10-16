﻿using Microsoft.AspNetCore.Mvc;
using UnitConverter.Models;

namespace UnitConverter.Controllers
{
    public class ConversionController : Controller
    {
        public IActionResult Length()
        {
            return View(new ConversionViewModel { ConversionType = "Length" });
        }

        public IActionResult Weight()
        {
            return View(new ConversionViewModel { ConversionType = "Weight" });
        }

        public IActionResult Temperature()
        {
            return View(new ConversionViewModel { ConversionType = "Temperature" });
        }

        public IActionResult Volume()
        {
            return View(new ConversionViewModel { ConversionType = "Volume" });
        }

        public IActionResult Area()
        {
            return View(new ConversionViewModel { ConversionType = "Area" });
        }

        [HttpPost]
        public IActionResult Convert(ConversionViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Result = ConvertUnits(model.InputValue, model.FromUnit, model.ToUnit, model.ConversionType);
            }

            return View(model.ConversionType, model);
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
            var conversionFactors = new Dictionary<string, double>
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

            if (conversionFactors.ContainsKey(fromUnit) && conversionFactors.ContainsKey(toUnit))
            {
                double valueInMeters = inputValue * conversionFactors[fromUnit];
                return valueInMeters / conversionFactors[toUnit];
            }

            return null;
        }

        private double? ConvertWeight(double inputValue, string fromUnit, string toUnit)
        {
            var conversionFactors = new Dictionary<string, double>
            {
                { "milligram", 0.001 },
                { "gram", 1 },
                { "kilogram", 1000 },
                { "ounce", 28.3495 },
                { "pound", 453.592 }
            };

            if (conversionFactors.ContainsKey(fromUnit) && conversionFactors.ContainsKey(toUnit))
            {
                double valueInGrams = inputValue * conversionFactors[fromUnit];
                return valueInGrams / conversionFactors[toUnit];
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
            var conversionFactors = new Dictionary<string, double>
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

            if (conversionFactors.ContainsKey(fromUnit) && conversionFactors.ContainsKey(toUnit))
            {
                double valueInLiters = inputValue * conversionFactors[fromUnit];
                return valueInLiters / conversionFactors[toUnit];
            }

            return null;
        }

        private double? ConvertArea(double inputValue, string fromUnit, string toUnit)
        {
            var conversionFactors = new Dictionary<string, double>
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

            if (conversionFactors.ContainsKey(fromUnit) && conversionFactors.ContainsKey(toUnit))
            {
                double valueInSquareMeters = inputValue * conversionFactors[fromUnit];
                return valueInSquareMeters / conversionFactors[toUnit];
            }

            return null;
        }
    }
}