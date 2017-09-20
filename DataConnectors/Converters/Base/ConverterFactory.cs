namespace DataConnectors.Converters.Base
{
    using DataConnectors.Common.Helper;

    public class ConverterFactory : GenericFactory
    {
        public static ConverterBase GetInstance(string typeName)
        {
            return GenericFactory.GetInstance<ConverterBase>(typeName);
        }
    }
}