using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using DataConnectors.Common.Extensions;
using DataConnectors.Common.Helper;
using DataConnectors.Converters.Model;

namespace DataConnectors.Converters
{
    public class ValueConvertProcessor
    {
        private ObservableCollection<ValueConverterDefinition> converterDefinitions = new ObservableCollection<ValueConverterDefinition>();

        private CultureInfo defaultCulture = null;

        private readonly ConvertDirections convertDirection;

        public ValueConvertProcessor(ConvertDirections convertDirection)
        {
            this.convertDirection = convertDirection;
        }

        public enum ConvertDirections
        {
            Read,
            Write
        }

        public ObservableCollection<ValueConverterDefinition> ConverterDefinitions
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

        public void ExecuteConverters(DataTable table)
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
                this.ExecuteConverters(row);
            }
        }

        public void ExecuteConverters(object obj)
        {
            if (obj is DataTable)
            {
                this.ExecuteConverters(obj as DataTable);
            }
            else if (obj is DataSet)
            {
                var dataSet = obj as DataSet;
                foreach (DataTable table in dataSet.Tables)
                {
                    this.ExecuteConverters(table);
                }
            }
        }

        protected void ExecuteConverters(DataRow row)
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

            // when converters exists, convert the value
            foreach (var converterDef in this.converterDefinitions)
            {
                // Converter for specific field
                if (!string.IsNullOrEmpty(converterDef.FieldName))
                {
                    switch (this.convertDirection)
                    {
                        case ConvertDirections.Read:
                            row[converterDef.FieldName] = converterDef.Converter.Convert(row[converterDef.FieldName], null, converterDef.ConverterParameter, culture);
                            break;

                        case ConvertDirections.Write:
                            row[converterDef.FieldName] = converterDef.Converter.ConvertBack(row[converterDef.FieldName], null, converterDef.ConverterParameter, culture);
                            break;
                    }
                }
            }
        }
    }
}