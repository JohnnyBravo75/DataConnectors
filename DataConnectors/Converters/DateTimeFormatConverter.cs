using System;
using System.Globalization;

namespace DataConnectors.Converters
{
    public class DateTimeFormatConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                if (parameter is string && !string.IsNullOrEmpty(parameter as string))
                {
                    DateTime returnDate;
                    DateTime.TryParseExact(value as string, parameter as string, culture, DateTimeStyles.None, out returnDate);
                }
            }
            return value;
        }
    }
}