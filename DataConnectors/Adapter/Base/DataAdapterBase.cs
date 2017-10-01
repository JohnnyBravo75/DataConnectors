using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DataConnectors.Common.Helper;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using DataConnectors.Common.Extensions;
using DataConnectors.Converters.Model;

namespace DataConnectors.Adapter
{
    public abstract class DataAdapterBase : IDataAdapterBase
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

        public abstract IList<DataColumn> GetAvailableColumns();

        public abstract IList<string> GetAvailableTables();

        public abstract int GetCount();

        public abstract IEnumerable<DataTable> ReadData(int? blockSize = null);

        public abstract bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false);

        protected DataRow ApplyConverters(DataRow row)
        {
            if (row == null)
            {
                return null;
            }

            if (this.converterDefinitions == null || !this.converterDefinitions.Any())
            {
                return row;
            }

            string cultureString = "";

            if (!string.IsNullOrEmpty(this.CultureColumnName))
            {
                cultureString = row[this.CultureColumnName].ToStringOrEmpty();
            }

            var culture = EnvironmentUtil.GetCultureFromString(cultureString);
            if (culture == null)
            {
                culture = this.DefaultCulture;
            }

            // when converters exits convert the value
            foreach (var converterDef in this.converterDefinitions)
            {
                row[converterDef.FieldName] = converterDef.Converter.Convert(row[converterDef.FieldName], null, converterDef.ConverterParameter, culture);
            }

            return row;
        }

        private IEnumerable<Dictionary<string, object>> ConvertTablesToDictionaries(IEnumerable<DataTable> tables)
        {
            foreach (DataTable table in tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    Dictionary<string, object> dict = row.Table.Columns
                                                                .Cast<DataColumn>()
                                                                .ToDictionary(c => c.ColumnName, c => row[c]);

                    yield return dict;
                }
            }
        }

        private IEnumerable<TObj> ConvertTablesToObjects<TObj>(IEnumerable<DataTable> tables, CultureInfo culture = null)
        {
            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }

            foreach (DataTable table in tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    TObj obj = DataTableHelper.CreateObject<TObj>(row, culture, this.ConverterDefinitions);

                    yield return obj;
                }
            }
        }

        private IEnumerable<DataTable> ConvertObjectsToTables<TObj>(IEnumerable<TObj> objects, int? blockSize = null)
        {
            var properties = typeof(TObj).GetProperties();

            var table = DataTableHelper.CreateTable<TObj>();

            if (objects != null)
            {
                int count = 0;
                foreach (object o in objects)
                {
                    var row = table.NewRow();
                    foreach (PropertyInfo prop in properties)
                    {
                        var val = prop.GetValue(o, null);

                        row[prop.Name] = (val != null
                                                ? val
                                                : DBNull.Value);
                    }

                    table.Rows.Add(row);

                    if (blockSize.HasValue && count == blockSize.Value)
                    {
                        yield return table;

                        count = 0;
                        table = DataTableHelper.CreateTable<TObj>();
                    }
                }
            }

            yield return table;
        }

        public virtual void Dispose()
        {
        }

        public virtual DataTable ReadAllData()
        {
            return this.ReadData().FirstOrDefault();
        }

        public virtual IEnumerable<TObj> ReadDataAs<TObj>(int? blockSize = null) where TObj : class
        {
            return this.ConvertTablesToObjects<TObj>(this.ReadData(blockSize));
        }

        public IEnumerable<Dictionary<string, object>> ReadDataAs(int? blockSize = null)
        {
            return this.ConvertTablesToDictionaries(this.ReadData(blockSize));
        }

        public virtual bool WriteDataFrom<TObj>(IEnumerable<TObj> objects, bool deleteBefore = false, int? blockSize = null) where TObj : class
        {
            return this.WriteData(this.ConvertObjectsToTables<TObj>(objects, blockSize), deleteBefore);
        }
    }
}