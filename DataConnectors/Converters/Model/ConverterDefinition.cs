namespace DataConnectors.Converters.Model
{
    public class ConverterDefinition
    {
        public ConverterDefinition(string fieldName, ConverterBase converter, string converterParameter)
        {
            this.FieldName = fieldName;
            this.Converter = converter;
            this.ConverterParameter = converterParameter;
        }

        public string FieldName { get; set; }

        public ConverterBase Converter { get; set; }

        public string ConverterParameter { get; set; }
    }
}