namespace DataConnectors.Formatters
{
    public class DefaultFormatter : FormatterBase
    {
        public override object Format(object data, object existingData = null)
        {
            return data;
        }
    }
}