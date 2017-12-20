using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Serialization;
using DataConnectors.Common.Extensions;
using DataConnectors.Formatters.Model;

namespace DataConnectors.Formatters
{
    public class DataTableToTemplateFormatter : FormatterBase
    {
        public DataTableToTemplateFormatter()
        {
            this.FormatterOptions.Add(new FormatterOption() { Name = "Template", Value = "" });
        }

        [XmlIgnore]
        public string Template
        {
            get { return this.FormatterOptions.GetValue<string>("Template"); }
            set { this.FormatterOptions.SetOrAddValue("Template", value); }
        }

        public override object Format(object data, object existingData = null)
        {
            string template = this.FormatterOptions.GetValue<string>("Template");

            var table = data as DataTable;
            var headerLine = existingData as string;

            var lines = new List<string>();
            if (table != null)
            {
                if (string.IsNullOrEmpty(headerLine))
                {
                    // generate header line
                    var columnNames = table.GetColumnNames().ToArray();

                    headerLine = template;
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        var columnName = table.Columns[i].ColumnName;
                        var value = columnNames[i].ToStringOrEmpty();

                        headerLine = headerLine.Replace(columnName, value.ToStringOrEmpty());
                    }

                    lines.Add(headerLine);
                }

                foreach (DataRow row in table.Rows)
                {
                    // generate data line
                    var fields = row.ItemArray.Select(field => field.ToString()).ToArray();

                    var line = template;
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var columnName = table.Columns[i].ColumnName;
                        var value = row[i].ToStringOrEmpty();

                        line = line.Replace("{" + columnName + "}", value);
                    }

                    lines.Add(line);
                }
            }

            return lines;
        }
    }
}