using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DataConnectors.Common.Helper;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using DataConnectors.Converters.Model;

namespace DataConnectors.Adapter
{
    public abstract class DataAdapterBase : IDataAdapterBase
    {
        private ObservableCollection<ConverterDefinition> converterDefinitions = new ObservableCollection<ConverterDefinition>();

        public ObservableCollection<ConverterDefinition> ConverterDefinitions
        {
            get { return this.converterDefinitions; }
            private set { this.converterDefinitions = value; }
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

        private IEnumerable<DataTable> ConvertObjectsToTables<T>(IEnumerable<T> objects, int? blockSize = null)
        {
            var properties = typeof(T).GetProperties();

            var table = DataTableHelper.CreateTable<T>();

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
                        table = DataTableHelper.CreateTable<T>();
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

        public abstract IList<DataColumn> GetAvailableColumns();

        public abstract IList<string> GetAvailableTables();

        public abstract int GetCount();

        public abstract IEnumerable<DataTable> ReadData(int? blockSize = null);

        public abstract bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false);

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