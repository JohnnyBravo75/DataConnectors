using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using DataConnectors.Common.Extensions;
using DataConnectors.Common.Model;
using DataConnectors.Converters.Model;

namespace DataConnectors.Common.Helper
{
    public static class DataTableHelper
    {
        public static void CreateTableColumns(DataTable table, IList<Field> fields, bool isHeader)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                // default column name, when file has no header line
                string columnName = string.Format("Column{0}", (i + 1));

                if (isHeader && !string.IsNullOrEmpty(fields[i].Name))
                {
                    columnName = fields[i].Name.Truncate(254);
                }

                // add column, when not exists
                if (!string.IsNullOrEmpty(columnName) && !table.Columns.Contains(columnName))
                {
                    table.Columns.Add(new DataColumn(columnName, fields[i].Datatype));
                }
            }
        }

        public static void CreateTableColumns(DataTable table, IList<string> columnNames, bool isHeader)
        {
            var fields = columnNames.Select(columnName => new Field(columnName)).ToList();
            CreateTableColumns(table, fields, isHeader);
        }

        public static void AddTableRow(DataTable table, IList<string> values)
        {
            // check for an empty row
            if (values == null || values.Count == 0)
            {
                return;
            }

            // enough columns existing? (can happen, when there arw rows with differnt number of separators)
            if (table.Columns.Count < values.Count())
            {
                // no, add the missing
                CreateTableColumns(table, values, true);
            }

            // put the whole row into the table
            table.Rows.Add(values.ToArray());
        }

        public static void DisposeTable(DataTable table)
        {
            if (table != null)
            {
                table.Clear();
                table.Dispose();
                table = null;
            }
        }

        public static DataTable ConvertToTable<T>(IList<T> list)
        {
            DataTable table = CreateTable<T>();
            Type entityType = typeof(T);
            var properties = TypeDescriptor.GetProperties(entityType);

            foreach (T item in list)
            {
                DataRow row = table.NewRow();

                foreach (PropertyDescriptor prop in properties)
                {
                    row[prop.Name] = prop.GetValue(item);
                }

                table.Rows.Add(row);
            }

            return table;
        }

        public static IList<T> ConvertToObjects<T>(IList<DataRow> rows)
        {
            IList<T> list = null;

            if (rows != null)
            {
                list = new List<T>();

                foreach (DataRow row in rows)
                {
                    T item = CreateObject<T>(row);
                    list.Add(item);
                }
            }

            return list;
        }

        public static IList<T> ConvertToObjects<T>(DataTable table)
        {
            if (table == null)
            {
                return null;
            }

            var rows = new List<DataRow>();

            foreach (DataRow row in table.Rows)
            {
                rows.Add(row);
            }

            return ConvertToObjects<T>(rows);
        }

        public static T CreateObject<T>(DataRow row, CultureInfo culture = null, IList<ConverterDefinition> converterDefinitions = null)
        {
            T obj = default(T);
            if (row != null)
            {
                if (obj is ExpandoObject)
                {
                    obj = CreateExpandoObject<T>(row, culture, converterDefinitions);
                }
                else
                {
                    obj = CreateTypedObject<T>(row, culture, converterDefinitions);
                }
            }

            return obj;
        }

        private static T CreateTypedObject<T>(DataRow row, CultureInfo culture, IList<ConverterDefinition> converterDefinitions)
        {
            var obj = Activator.CreateInstance<T>();

            var objType = obj.GetType();
            var dataMemberAttrs = objType.GetProperties()
                                        .Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)))
                                        .ToList();

            foreach (DataColumn column in row.Table.Columns)
            {
                bool isRequired = false;

                // get the property
                var property = objType.GetProperty(column.ColumnName.Trim(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (property == null && dataMemberAttrs.Any())
                {
                    // otherwise look for dataMember Attribute
                    property = dataMemberAttrs.FirstOrDefault(p => ((DataMemberAttribute)Attribute.GetCustomAttribute(p, typeof(DataMemberAttribute))).Name == column.ColumnName);
                    if (property != null)
                    {
                        isRequired = ((DataMemberAttribute)Attribute.GetCustomAttribute(property, typeof(DataMemberAttribute))).IsRequired;
                    }
                }

                if (property != null)
                {
                    object value = row[column.ColumnName];

                    if (value == null && isRequired)
                    {
                        // When required, throw
                        throw new NoNullAllowedException(column.ColumnName);
                    }
                    else
                    {
                        // when converters exits convert the value
                        if (converterDefinitions != null)
                        {
                            foreach (var converterDef in converterDefinitions.Where(x => x.FieldName == column.ColumnName))
                            {
                                value = converterDef.Converter.Convert(value, property.PropertyType, converterDef.ConverterParameter, culture);
                            }
                        }

                        object tgtValue = ConvertExtensions.ChangeTypeExtended(value, property.PropertyType, culture);

                        property.SetValue(obj, tgtValue, null);
                    }
                }
            }
            return obj;
        }

        private static T CreateExpandoObject<T>(DataRow row, CultureInfo culture, IList<ConverterDefinition> converterDefinitions)
        {
            T obj = Activator.CreateInstance<T>();

            var expandoDic = (IDictionary<string, object>)(obj as ExpandoObject);

            var dict = row.Table.Columns
                                .Cast<DataColumn>()
                                .ToDictionary(c => c.ColumnName, c => row[c]);

            foreach (var item in dict)
            {
                expandoDic.AddOrUpdate(item.Key, item.Value);
            }

            return obj;
        }

        public static DataTable CreateTable<T>()
        {
            Type entityType = typeof(T);
            var table = new DataTable(entityType.Name);
            var properties = TypeDescriptor.GetProperties(entityType);

            foreach (PropertyDescriptor prop in properties)
            {
                // respect nullable types
                Type type = ConvertExtensions.IsNullable(prop.PropertyType)
                                        ? Nullable.GetUnderlyingType(prop.PropertyType)
                                        : prop.PropertyType;

                table.Columns.Add(prop.Name, type);
            }

            return table;
        }

        /// <summary>
        /// Cleans the name of the column.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <returns></returns>
        public static string CleanColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return columnName;
            }

            return columnName.Replace("(", @"\(")
                             .Replace(")", @"\)")
                             .Replace("[", @"\[")
                             .Replace("]", @"\]")
                             .Replace(".", @"\.")
                             .Replace("/", @"\/")
                             .Replace(@"\", @"\\");
        }
    }
}