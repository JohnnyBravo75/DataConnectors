using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using DataConnectors.Common.Extensions;
using DataConnectors.Common.Helper;
using DataConnectors.Converters.Model;

namespace DataConnectors.Converters
{
    public class ConvertProcessor
    {
        private ObservableCollection<ConverterDefinition> converterDefinitions = new ObservableCollection<ConverterDefinition>();

        private CultureInfo defaultCulture = null;

        public ObservableCollection<ConverterDefinition> ConverterDefinitions
        {
            get { return this.converterDefinitions; }
            set { this.converterDefinitions = value; }
        }

        public string CultureColumnName { get; set; }

        public CultureInfo DefaultCulture
        {
            get
            {
                if (this.defaultCulture == null)
                {
                    return CultureInfo.InvariantCulture;
                }
                return this.defaultCulture;
            }
            set { this.defaultCulture = value; }
        }

        public void ApplyConverters(DataTable table)
        {
            if (table == null)
            {
                return;
            }

            if (this.converterDefinitions == null || !this.converterDefinitions.Any())
            {
                return;
            }

            foreach (DataRow row in table.Rows)
            {
                this.ApplyConverters(row);
            }
        }

        public void ApplyConverters(object obj)
        {
            if (obj is DataTable)
            {
                this.ApplyConverters(obj as DataTable);
            }
            else if (obj is DataSet)
            {
                var dataSet = obj as DataSet;
                foreach (DataTable table in dataSet.Tables)
                {
                    this.ApplyConverters(table);
                }
            }
        }

        protected void ApplyConverters(DataRow row)
        {
            if (row == null)
            {
                return;
            }

            if (this.converterDefinitions == null || !this.converterDefinitions.Any())
            {
                return;
            }

            string cultureString = "";

            // Is the culture in a column?
            if (!string.IsNullOrEmpty(this.CultureColumnName))
            {
                cultureString = row[this.CultureColumnName].ToStringOrEmpty();
            }

            var culture = CultureUtil.GetCultureFromString(cultureString);
            if (culture == null)
            {
                culture = this.DefaultCulture;
            }

            // when converters exists convert the value
            foreach (var converterDef in this.converterDefinitions)
            {
                row[converterDef.FieldName] = converterDef.Converter.Convert(row[converterDef.FieldName], null, converterDef.ConverterParameter, culture);
            }
        }
    }
}