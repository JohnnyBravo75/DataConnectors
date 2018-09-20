using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataConnectors.Common.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Dumps the specified object in a json format. Helpfull when loggin an object information is needed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x">The x.</param>
        public static void DumpToConsole<T>(this T x)
        {
            if (x != null)
            {
                string json = JsonConvert.SerializeObject(x, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        /// <summary>
        ///Dumps the specified object in a json format to a string. Helpfull when loggin an object information is needed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x">The x.</param>
        /// <returns></returns>
        public static string DumpToString<T>(this T x)
        {
            if (x == null)
            {
                return string.Empty;
            }

            try
            {
                string json = JsonConvert.SerializeObject(x, Formatting.Indented);
                return json;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// To the property string.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static string ToPropertyString(this object obj)
        {
            var type = obj.GetType();

            var props = type.GetProperties();
            var sb = new StringBuilder();
            foreach (var prop in props)
            {
                if (prop.CanRead)
                {
                    if (prop.GetIndexParameters().Length == 0)
                    {
                        sb.AppendLine(prop.Name + ": " + prop.GetValue(obj, null));
                    }
                }
            }
            return sb.ToString();
        }
    }
}