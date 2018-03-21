using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace DataConnectors.Converters
{
    public class BooleanConverter : ValueConverterBase
    {
        private readonly List<string> trueValues = new List<string>()
        {  "1",
           "true",
           "yes",
           "y"
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