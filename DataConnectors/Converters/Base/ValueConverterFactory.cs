namespace DataConnectors.Converters.Base
{
    using DataConnectors.Common.Helper;

    public class ValueConverterFactory : GenericFactory
    {
        public static ValueConverterBase GetInstance(string typeName)
        {
            return GenericFactory.GetInstance<ValueConverterBase>(typeName);
        }
    }
}