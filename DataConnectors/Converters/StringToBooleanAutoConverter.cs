using System;
using System.Collections.Generic;

namespace DataConnectors.Converters
{
    public class StringToBooleanAutoConverter : ValueConverterBase
    {
        private readonly List<string> trueValues = new List<string>()
        {  "1",
           "true",
           "t",
           "yes",
           "y",
           "x"
        };

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool result = false;

            string strValue = (value as string);

            if (strValue != null)
            {
                // Special handling for Boolean
                strValue = strValue.Trim().ToLower();

                result = this.trueValues.Contains(strValue);
            }

            return result;
        }
    }
}