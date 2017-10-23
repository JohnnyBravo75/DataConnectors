namespace DataConnectors.Adapter
{
    using System.Data.Common;
    using DataConnectors.Common.Helper;

    public class DataAdapterFactory : GenericFactory
    {
        public static DataAdapter GetInstance(string typeName)
        {
            return GenericFactory.GetInstance<DataAdapter>(typeName);
        }
    }
}