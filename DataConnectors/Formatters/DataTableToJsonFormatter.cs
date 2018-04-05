using System.Data;
using Newtonsoft.Json;

namespace DataConnectors.Formatters
{
    public class DataTableToJsonFormatter : FormatterBase
    {
        public override object Format(object data, object existingData = null)
        {
            var table = data as DataTable;

            string json = JsonConvert.SerializeObject(table, Formatting.Indented);

            return json;
        }
    }
}