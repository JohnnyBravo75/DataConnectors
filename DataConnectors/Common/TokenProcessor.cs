﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using DataConnectors.Common.Extensions;

namespace DataConnectors.Common
{
    public class TokenProcessor
    {
        public static string ReplaceTokens(string str, IDictionary<string, object> parameters = null)
        {
            string replacedStr = str;
            bool wasReplaced = ReplaceTokens(str, parameters, out replacedStr);
            return replacedStr;
        }

        public static bool ReplaceTokens(string str, IDictionary<string, object> parameters, out string replacedStr)
        {
            replacedStr = str;
            bool wasReplaced = false;

            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    if (replacedStr.Contains("{" + parameter.Key + "}"))
                    {
                        replacedStr = replacedStr.Replace("{" + parameter.Key + "}", parameter.Value.ToStringOrEmpty());
                        wasReplaced = true;
                    }
                }
            }

            foreach (var dateFmt in DateFormats)
            {
                if (replacedStr.Contains("{" + dateFmt + "}"))
                {
                    replacedStr = replacedStr.Replace("{" + dateFmt + "}", DateTime.Now.ToString(dateFmt));
                    wasReplaced = true;
                }
            }

            return wasReplaced;
        }

        public static string ReplaceToken(string str, string tokenName, object tokenValue)
        {
            var dict = new Dictionary<string, object>()
            {
                { tokenName, tokenValue }
            };

            return ReplaceTokens(str, dict);
        }

        private static List<string> dateFormats = null;

        private static List<string> DateFormats
        {
            get
            {
                if (dateFormats == null)
                {
                    dateFormats = new List<string>()
                        {
                            "yyyy", "yy",
                            "MM", "MMM", "MMMM",
                            "dd", "ddd", "dddd",
                            "d","D","f","F",
                            "g","G","m","M",
                            "s","t","T","o","O",
                            "y","Y","u","U",
                            "yyyyMMdd", "yyMM", "yyMMdd", "yyyy-MM-dd"
                        };

                    dateFormats.AddRange(DateTimeFormatInfo.CurrentInfo.GetAllDateTimePatterns('d'));

                    var currentCulture = Thread.CurrentThread.CurrentCulture;

                    dateFormats.AddRange(currentCulture.DateTimeFormat.GetAllDateTimePatterns());
                }

                return dateFormats;
            }
        }
    }
}