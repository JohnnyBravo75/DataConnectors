using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using DataConnectors.Common.Extensions;
using DataConnectors.Common.Model;
using DataConnectors.Formatters.Model;

namespace DataConnectors.Formatters
{
    public class DataTableToCsvFormatter : FormatterBase
    {
        private FieldDefinitionList fieldDefinitions = new FieldDefinitionList();

        public DataTableToCsvFormatter()
        {
            this.FormatterOptions.Add(new FormatterOption() { Name = "Separator", Value = ";" });
            this.FormatterOptions.Add(new FormatterOption() { Name = "Enclosure", Value = "" });
        }

        public FieldDefinitionList FieldDefinitions
        {
            get { return this.fieldDefinitions; }
            set { this.fieldDefinitions = value; }
        }

        [XmlIgnore]
        public string Separator
        {
            get { return this.FormatterOptions.GetValue<string>("Separator"); }
            set { this.FormatterOptions.SetOrAddValue("Separator", value); }
        }

        [XmlIgnore]
        public string Enclosure
        {
            get { return this.FormatterOptions.GetValue<string>("Enclosure"); }
            set { this.FormatterOptions.SetOrAddValue("Enclosure", value); }
        }

        public override object Format(object data, object existingData = null)
        {
            string separator = this.FormatterOptions.GetValue<string>("Separator");
            string enclosure = this.FormatterOptions.GetValue<string>("Enclosure");
            bool hasHeader = true;

            var table = data as DataTable;
            var headerLine = existingData as string;

            var lines = new List<string>();
            if (table != null)
            {
                if (string.IsNullOrEmpty(headerLine))
                {
                    // generate header line
                    var columnNames = this.CreateCsvColumns(table);
                    var line = this.BuildLine(columnNames, separator[0], enclosure);
                    lines.Add(line);
                }

                foreach (DataRow row in table.Rows)
                {
                    // generate data line
                    var fields = this.CreateCsvFields(table, row);
                    var line = this.BuildLine(fields, separator[0], enclosure);
                    lines.Add(line);
                }
            }

            return lines;
        }

        /// <summary>
        /// Builds a line out of the given fields
        /// </summary>
        /// <param name="fields">The fields.</param>
        /// <param name="separator">The separator.</param>
        /// <param name="enclosure">if set to <c>true</c> [quoted].</param>
        /// <returns>a line as string</returns>
        private string BuildLine(string[] fields, char separator, string enclosure)
        {
            var line = new StringBuilder();
            bool quoted = !string.IsNullOrEmpty(enclosure);

            for (int i = 0; i < fields.Length; i++)
            {
                if (quoted)
                {
                    line.Append(enclosure);
                }

                line.Append(fields[i]);

                if (quoted)
                {
                    line.Append(enclosure);
                }

                if (i < fields.Length - 1)
                {
                    line.Append(separator);
                }
            }

            return line.ToString();
        }

        /// <summary>
        /// Creates the export columns of a table (depending on the fielddefintions).
        /// </summary>
        /// <param name="table">The table.</param>
        /// <returns>array of the columns</returns>
        private string[] CreateCsvColumns(DataTable table)
        {
            int numberOfColumns = this.FieldDefinitions.Count == 0
                                                    ? table.Columns.Count
                                                    : this.FieldDefinitions.Count;

            int numberOfActiveColumns = this.FieldDefinitions.Count == 0
                                                    ? table.Columns.Count
                                                    : this.FieldDefinitions.Count(x => x.IsActive);

            string[] headers = new string[numberOfActiveColumns];

            int columnIndex = 0;
            // loop all defined columns
            for (int i = 0; i < numberOfColumns; i++)
            {
                if (this.FieldDefinitions.Count == 0)
                {
                    // when no fielddefintion, take every column
                    headers[columnIndex] = table.Columns[i].ToString();
                    columnIndex++;
                }
                else
                {
                    var fieldDef = this.FieldDefinitions[i];
                    if (fieldDef.IsActive && fieldDef.DataSourceField != null)
                    {
                        // set the index (for faster array access, not alwyas searching by name)
                        fieldDef.DataSourceFieldIndex = i;
                        string columnName = fieldDef.TableField.Name;
                        fieldDef.TableFieldIndex = table.Columns.IndexOf(columnName);
                        headers[columnIndex] = this.FieldDefinitions.First(x => x.TableField.Name == columnName).DataSourceField.Name;
                        columnIndex++;
                        // headers[i] = this.FieldDefinitions.MapToDataSourceFieldName(columnName);
                    }
                }
            }

            return headers;
        }

        /// <summary>
        /// Splits a datarow in a an array of fields (depending on the fielddefintions).
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="row">The row.</param>
        /// <returns></returns>
        private string[] CreateCsvFields(DataTable table, DataRow row)
        {
            int numberOfColumns = this.FieldDefinitions.Count == 0 ? table.Columns.Count
                                                                   : this.FieldDefinitions.Count;

            int numberOfActiveColumns = this.FieldDefinitions.Count == 0 ? table.Columns.Count
                                                                         : this.FieldDefinitions.Count(x => x.IsActive);

            var fields = new string[numberOfActiveColumns];

            int columnIndex = 0;
            // loop all defined columns
            for (int i = 0; i < numberOfColumns; i++)
            {
                if (this.FieldDefinitions.Count == 0)
                {
                    // when no fielddefintion, take every column
                    fields[columnIndex] = row.ItemArray[i].ToStringOrEmpty();
                    columnIndex++;
                }
                else
                {
                    // get the mapped column in the table
                    var fieldDef = this.FieldDefinitions[i];
                    if (fieldDef.IsActive && fieldDef.DataSourceField != null)
                    {
                        fields[columnIndex] = row.ItemArray[fieldDef.TableFieldIndex].ToStringOrEmpty();
                        columnIndex++;
                    }
                }
            }

            return fields;
        }
    }
}