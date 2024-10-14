# Unit Converter

This is a .NET 8 web application for converting units of measurement, such as temperature, volume, area, and weight. The application is built using ASP.NET Core MVC and C# 12.0.<br>
Project Idea: https://roadmap.sh/projects/unit-converter

## Features

- Convert between different units of temperature (Celsius, Fahrenheit, Kelvin).
- Convert between different units of volume (milliliter, liter, cubic meter, gallon, quart, pint, cup, fluid ounce).
- Convert between different units of area (square millimeter, square centimeter, square meter, square kilometer, square inch, square foot, square yard, acre, hectare).
- Convert between different units of weight (milligram, gram, kilogram, ounce, pound).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Getting Started

### Clone the Repository
```bash
git clone https://github.com/yourusername/unit-converter.git
```

### Build and Run the Application

1. Restore the dependencies:
```bash
dotnet restore
```

2. Build the application:
```bash
dotnet build
```

3. Run the application:
```bash
dotnet run
```


4. Open your browser and navigate to `https://localhost:7169` to see the application in action.

## Project Structure

- `Controllers/`: Contains the MVC controllers for handling HTTP requests.
- `Models/`: Contains the data models used in the application.
- `Views/`: Contains the Razor views for rendering the UI.
- `wwwroot/`: Contains static files such as CSS, JavaScript, and images.
- `Program.cs`: The entry point of the application.

## Usage

### Temperature Conversion

1. Navigate to one of the ***Conversion*** pages.
2. Enter the value to be converted.
3. Select the units to convert from and to.
4. Click the **Convert** button to see the result.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any changes.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/batuhansacofficial/UnitConverter?tab=MIT-1-ov-file) file for details.
