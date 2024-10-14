namespace UnitConverter.Models
{
    public class ConversionViewModel
    {
        public double InputValue { get; set; }
        public string FromUnit { get; set; }
        public string ToUnit { get; set; }
        public double? Result { get; set; }
        public string ConversionType { get; set; } // Length, Weight, Temperature
    }
}
