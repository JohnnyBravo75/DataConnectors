using System;
using System.Xml.Serialization;

namespace DataConnectors.Converters
{
    [XmlInclude(typeof(StringToBooleanAutoConverter))]
    [XmlInclude(typeof(StringToDateTimeAutoConverter))]
    [XmlInclude(typeof(StringToDateTimeFormatConverter))]
    [XmlInclude(typeof(StringToNumberAutoConverter))]
    [XmlInclude(typeof(StringToNumberFormatConverter))]
    [Serializable]
    public abstract class ValueConverterBase
    {
        public virtual object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var formattable = value as IFormattable;
            return formattable == null
                            ? value.ToString()
                            : formattable.ToString(null, culture);
        }
    }
}