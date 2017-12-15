using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DataConnectors.Common.Helper;
using System;
using System.Globalization;
using System.Xml.Serialization;
using DataConnectors.Adapter.FileAdapter;
using DataConnectors.Converters;

namespace DataConnectors.Adapter
{
    [XmlInclude(typeof(CsvAdapter))]
    [XmlInclude(typeof(FixedTextAdapter))]
    [XmlInclude(typeof(XmlAdapter))]
    [XmlInclude(typeof(FlatFileAdapter))]
    [XmlInclude(typeof(ExcelNativeAdapter))]
    [XmlInclude(typeof(Excel2007NativeAdapter))]
    [Serializable]
    public abstract class DataAdapterBase : IDataAdapterBase
    {
        private ValueConvertProcessor readConverter = new ValueConvertProcessor(ValueConvertProcessor.ConvertDirections.Read);

        private ValueConvertProcessor writeConverter = new ValueConvertProcessor(ValueConvertProcessor.ConvertDirections.Write);

        public ValueConvertProcessor ReadConverter
        {
            get { return this.readConverter; }
            set { this.readConverter = value; }
        }

        public ValueConvertProcessor WriteConverter
        {
            get { return this.writeConverter; }
            set { this.writeConverter = value; }
        }

        public abstract IList<DataColumn> GetAvailableColumns();

        public abstract IList<string> GetAvailableTables();

        public abstract int GetCount();

        public abstract IEnumerable<DataTable> ReadData(int? blockSize = null);

        public abstract bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false);

        protected IEnumerable<Dictionary<string, object>> ConvertTablesToDictionaries(IEnumerable<DataTable> tables)
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

        protected IEnumerable<TObj> ConvertTablesToObjects<TObj>(IEnumerable<DataTable> tables, CultureInfo culture = null)
        {
            if (culture == null)
            {
                culture = CultureInfo.InvariantCulture;
            }

            foreach (DataTable table in tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    TObj obj = DataTableHelper.CreateObject<TObj>(row, culture, this.ReadConverter.ConverterDefinitions);

                    yield return obj;
                }
            }
        }

        protected IEnumerable<DataTable> ConvertObjectsToTables<TObj>(IEnumerable<TObj> objects, int? blockSize = null)
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

        public abstract void Dispose();

        public virtual DataTable ReadAllData()
        {
            return this.ReadData().FirstOrDefault();
        }

        public virtual IEnumerable<TObj> ReadAllDataAs<TObj>() where TObj : class
        {
            return this.ReadDataAs<TObj>().ToList();
        }

        public virtual IEnumerable<Dictionary<string, object>> ReadAllDataAs()
        {
            return this.ReadDataAs().ToList();
        }

        public virtual IEnumerable<TObj> ReadDataAs<TObj>(int? blockSize = null) where TObj : class
        {
            return this.ConvertTablesToObjects<TObj>(this.ReadData(blockSize));
        }

        public virtual IEnumerable<Dictionary<string, object>> ReadDataAs(int? blockSize = null)
        {
            return this.ConvertTablesToDictionaries(this.ReadData(blockSize));
        }

        public virtual bool WriteDataFrom<TObj>(IEnumerable<TObj> objects, bool deleteBefore = false, int? blockSize = null) where TObj : class
        {
            return this.WriteData(this.ConvertObjectsToTables<TObj>(objects, blockSize), deleteBefore);
        }

        public virtual void WriteAllData(DataTable table)
        {
            var list = new List<DataTable>();
            list.Add(table);
            this.WriteData(list);
        }
    }
}