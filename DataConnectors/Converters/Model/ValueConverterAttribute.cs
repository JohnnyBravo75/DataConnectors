using System;

namespace DataConnectors.Converters.Model
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ValueConverterAttribute : Attribute
    {
        public ValueConverterAttribute(Type converterType, string converterParameter = "")
        {
            this.ConverterType = converterType;
            this.ConverterParameter = converterParameter;
        }

        public Type ConverterType { get; private set; }

        public string ConverterParameter { get; private set; }
    }
}