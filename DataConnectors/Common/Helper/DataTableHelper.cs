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
using DataConnectors.Converters;
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
                else
                {
                    table.Columns.Add(new DataColumn(columnName + "_" + i, fields[i].Datatype));
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
            if (table.Columns.Count < values.Count)
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

        public static T CreateObject<T>(DataRow row, CultureInfo culture = null, IList<ValueConverterDefinition> converterDefinitions = null)
        {
            T obj = default(T);
            if (row != null)
            {
                if (typeof(T) == typeof(ExpandoObject))
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

        private static T CreateTypedObject<T>(DataRow row, CultureInfo culture, IEnumerable<ValueConverterDefinition> converterDefinitions)
        {
            var obj = Activator.CreateInstance<T>();

            var objType = obj.GetType();
            var dataMemberProps = objType.GetProperties()
                                        .Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)))
                                        .ToList();

            var dataFieldsProps = objType.GetProperties()
                                        .Where(p => Attribute.IsDefined(p, typeof(DataFieldAttribute)))
                                        .ToList();

            foreach (DataColumn column in row.Table.Columns)
            {
                bool isRequired = false;

                // get the property
                var property = objType.GetProperty(column.ColumnName.Trim(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (property == null && dataFieldsProps.Any())
                {
                    // look for [DataField] attribute
                    property = dataFieldsProps.FirstOrDefault(p => ((DataFieldAttribute)Attribute.GetCustomAttribute(p, typeof(DataFieldAttribute))).Name == column.ColumnName);
                    if (property != null)
                    {
                        var dataFieldAttr = ((DataFieldAttribute)Attribute.GetCustomAttribute(property, typeof(DataFieldAttribute)));
                        if (dataFieldAttr != null)
                        {
                            isRequired = dataFieldAttr.IsRequired;
                        }
                    }
                }

                if (property == null && dataMemberProps.Any())
                {
                    // otherwise look for [DataMember] attribute
                    property = dataMemberProps.FirstOrDefault(p => ((DataMemberAttribute)Attribute.GetCustomAttribute(p, typeof(DataMemberAttribute))).Name == column.ColumnName);
                    if (property != null)
                    {
                        var dataMemberAttr = ((DataMemberAttribute)Attribute.GetCustomAttribute(property, typeof(DataMemberAttribute)));
                        if (dataMemberAttr != null)
                        {
                            isRequired = dataMemberAttr.IsRequired;
                        }
                    }
                }

                if (property != null)
                {
                    object value = row[column.ColumnName];

                    if (value == null && isRequired)
                    {
                        // When required but null, throw
                        throw new NoNullAllowedException(column.ColumnName);
                    }
                    else
                    {
                        // when converters exists, convert the value
                        if (converterDefinitions != null && converterDefinitions.Any())
                        {
                            foreach (var converterDef in converterDefinitions.Where(x => x.FieldName == column.ColumnName))
                            {
                                var converter = converterDef.Converter;
                                value = converter.Convert(value, property.PropertyType, converterDef.ConverterParameter, culture);
                            }
                        }
                        else
                        {
                            // lookup a converter attribute
                            var converterAttr = ((ValueConverterAttribute)Attribute.GetCustomAttribute(property, typeof(ValueConverterAttribute)));
                            if (converterAttr != null)
                            {
                                var converter = Activator.CreateInstance(converterAttr.ConverterType) as ValueConverterBase;
                                if (converter != null)
                                {
                                    value = converter.Convert(value, property.PropertyType, converterAttr.ConverterParameter, culture);
                                }
                            }
                        }

                        try
                        {
                            object tgtValue = ConvertExtensions.ChangeTypeExtended(value, property.PropertyType, culture);

                            property.SetValue(obj, tgtValue, null);
                        }
                        catch (Exception ex)
                        {
                            throw new FormatException(string.Format("The value '{0}' cannot be converted to the target type '{1}' with the given culture '{2}'", value, property.PropertyType, culture?.DisplayName));
                        }
                    }
                }
            }
            return obj;
        }

        private static T CreateExpandoObject<T>(DataRow row, CultureInfo culture, IList<ValueConverterDefinition> converterDefinitions)
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

        public static string CleanTableName(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return tableName;
            }

            //return tableName.Replace("(", @"\(")
            //    .Replace(")", @"\)")
            //    .Replace("[", @"\[")
            //    .Replace("]", @"\]")
            //    .Replace(".", @"\.")
            //    .Replace("/", @"\/")
            //    .Replace(" ", @"_")
            //    .Replace(@"\", @"\\");

            string replacement = "";
            return tableName.Replace("(", @"_")
                       .Replace(")", replacement)
                       .Replace("[", replacement)
                       .Replace("]", replacement)
                       .Replace(".", replacement)
                       .Replace("/", replacement)
                       .Replace(" ", replacement)
                       .Replace(@"\", replacement)
                       .Replace(@"-", replacement)
                       .Replace(@"%", replacement)
                       .Replace(@"*", replacement);
        }

        public static IList<string> CleanColumnNames(IList<string> columnNames)
        {
            var cleanedColumns = new List<string>();

            if (columnNames == null)
            {
                return cleanedColumns;
            }

            for (int i = 0; i < columnNames.Count; i++)
            {
                cleanedColumns.Add(CleanColumnName(columnNames[i]));
            }

            return cleanedColumns;
        }

        public static string CleanColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return columnName;
            }

            //return columnName.Replace("(", @"\(")
            //                 .Replace(")", @"\)")
            //                 .Replace("[", @"\[")
            //                 .Replace("]", @"\]")
            //                 .Replace(".", @"\.")
            //                 .Replace("/", @"\/")
            //                 .Replace(@"\", @"\\");

            string replacement = "";
            return columnName.Replace("(", @"_")
                       .Replace(")", replacement)
                       .Replace("[", replacement)
                       .Replace("]", replacement)
                       .Replace(".", replacement)
                       .Replace("/", replacement)
                       .Replace(" ", replacement)
                       .Replace(@"\", replacement)
                       .Replace(@"-", replacement)
                       .Replace(@"%", replacement)
                       .Replace(@"*", replacement);
        }
    }
}