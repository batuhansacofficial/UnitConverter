namespace UnitConverter.Models
{
    public enum ConversionType
    {
        Area,
        Length,
        Volume,
        Temperature,
        Weight
    }

    public class ConversionViewModel
    {
        public double InputValue { get; set; }
        public string FromUnit { get; set; }
        public string ToUnit { get; set; }
        public double? Result { get; set; }
        public ConversionType ConversionType { get; set; }
    }
}