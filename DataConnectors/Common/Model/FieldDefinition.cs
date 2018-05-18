﻿using System;
using DataConnectors.Converters;

namespace DataConnectors.Common.Model
{
    /// <summary>
    /// Fielddefinition, which maps the external datasource field (datasourcefield) to the internal field (tablefield)
    /// </summary>
    public class FieldDefinition : NotifyPropertyChangedBase, IDisposable
    {
        // ***********************Fields***********************

        private Field dataSourceField = new Field();
        private Field tableField = new Field();

        private int dataSourceFieldIndex = -1;
        private int tableFieldIndex = -1;
        private bool isActive = true;

        private ValueConverterBase valueConverter;

        // ***********************Constructors***********************

        public FieldDefinition()
        {
        }

        public FieldDefinition(Field dataSourceField, ValueConverterBase valueConverter = null)
        {
            this.dataSourceField = dataSourceField;
            this.tableField = new Field()
            {
                Name = dataSourceField.Name,
                Length = dataSourceField.Length,
                Datatype = dataSourceField.Datatype
            };

            this.valueConverter = valueConverter;
        }

        public FieldDefinition(Field dataSourceField, Field tableField, ValueConverterBase valueConverter = null)
        {
            this.dataSourceField = dataSourceField;
            this.tableField = tableField;

            this.valueConverter = valueConverter;
        }

        public FieldDefinition(string dataSourceFieldName, string tableFieldName = "", Type type = null, ValueConverterBase valueConverter = null)
        {
            if (type == null)
            {
                type = typeof(string);
            }

            this.dataSourceField = new Field()
            {
                Name = dataSourceFieldName,
                Datatype = type
            };

            this.tableField = new Field()
            {
                Name = (!string.IsNullOrEmpty(tableFieldName) ? tableFieldName : dataSourceFieldName),
                Datatype = type
            };

            this.valueConverter = valueConverter;
        }

        // ***********************Properties***********************

        public bool IsActive
        {
            get { return this.isActive; }
            set
            {
                if (this.isActive != value)
                {
                    this.isActive = value;
                    this.RaisePropertyChanged("IsActive");
                }
            }
        }

        /// <summary>
        /// Gets or sets the external datasourcefield in the datasource (file, database,..).
        /// </summary>
        /// <value>
        /// The adapter field.
        /// </value>
        public Field DataSourceField
        {
            get { return this.dataSourceField; }
            set { this.dataSourceField = value; }
        }

        /// <summary>
        /// Gets or sets the index/position of the external datasourcefield in the datasource.
        /// This can be used for direct index access, not always searching by name (brings performance gain).
        /// </summary>
        public int DataSourceFieldIndex
        {
            get { return this.dataSourceFieldIndex; }
            set { this.dataSourceFieldIndex = value; }
        }

        /// <summary>
        /// Gets or sets the internal table field.
        /// </summary>
        /// <value>
        /// The table field.
        /// </value>
        public Field TableField
        {
            get { return this.tableField; }
            set { this.tableField = value; }
        }

        /// <summary>
        /// Gets or sets the internal table field index.
        /// </summary>
        /// <value>
        /// The index of the table field.
        /// </value>
        public int TableFieldIndex
        {
            get { return this.tableFieldIndex; }
            set { this.tableFieldIndex = value; }
        }

        public ValueConverterBase ValueConverter
        {
            get { return this.valueConverter; }
            set { this.valueConverter = value; }
        }

        // ***********************Functions***********************

        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return (this.dataSourceField != null ? this.dataSourceField.ToString() : "") + " <-> " + (this.tableField != null ? this.tableField.ToString() : "");
        }
    }
}