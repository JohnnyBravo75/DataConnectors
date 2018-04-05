namespace DataConnectors.Converters.Base
{
    using Common.Helper;

    public class ValueConverterFactory : GenericFactory
    {
        public static ValueConverterBase GetInstance(string typeName)
        {
            return GenericFactory.GetInstance<ValueConverterBase>(typeName);
        }
    }
}