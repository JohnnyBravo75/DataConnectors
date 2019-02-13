using System;
using System.Xml.Serialization;

namespace DataConnectors.Common.Model
{
    [Serializable]
    public class Field : NotifyPropertyChangedBase
    {
        private Type dataType = typeof(string);
        private int length = -1;
        private string name = string.Empty;
        private string formatMask = "";

        // ***********************Constructors***********************

        /// <summary>
        /// Initializes a new instance of the <see cref="Field" /> class.
        /// </summary>
        public Field()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Field" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public Field(string name)
        {
            this.Name = name;
        }

        public Field(string name, int length)
        {
            this.Name = name;
            this.Length = length;
        }

        public Field(string name, int length, Type dataType = null)
        {
            this.Name = name;
            this.Length = length;
            this.Datatype = dataType;
        }

        // ***********************Properties***********************

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [XmlAttribute]
        public string Name
        {
            get { return this.name; }

            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    this.RaisePropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        [XmlAttribute]
        public int Length
        {
            get { return this.length; }

            set
            {
                if (this.length != value)
                {
                    this.length = value;
                    this.RaisePropertyChanged("Length");
                }
            }
        }

        [XmlAttribute(AttributeName = "DataType")]
        public string DataTypeString
        {
            get
            {
                if (this.dataType == null)
                {
                    return null;
                }

                return this.dataType.FullName;
            }
            set
            {
                if (value == null)
                {
                    this.dataType = null;
                }
                else
                {
                    this.dataType = Type.GetType(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the datatype in string form (e.g "System.String")
        /// </summary>
        /// <value>
        /// The datatype.
        /// </value>
        [XmlIgnore]
        public Type Datatype
        {
            get { return this.dataType; }

            set
            {
                if (this.dataType != value)
                {
                    this.dataType = value;
                    this.RaisePropertyChanged("Datatype");
                }
            }
        }

        /// <summary>
        /// Gets or sets the format mask.
        /// </summary>
        /// <value>
        /// The format mask.
        /// </value>
        [XmlAttribute]
        public string FormatMask
        {
            get { return this.formatMask; }

            set
            {
                if (this.formatMask != value)
                {
                    this.formatMask = value;
                    this.RaisePropertyChanged("FormatMask");
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}