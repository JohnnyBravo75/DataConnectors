using System;
using System.Xml.Serialization;
using DataConnectors.Common;
using DataConnectors.Common.Helper;

namespace DataConnectors.Converters.Model
{
    [Serializable]
    public class ConverterDefinition : NotifyPropertyChangedBase
    {
        private string converterName = "";
        private string fieldName = "";
        private ConverterBase converter;

        // ***********************Constructors***********************

        public ConverterDefinition()
        {
        }

        public ConverterDefinition(string fieldName, string converterName, string converterParameter)
        {
            this.FieldName = fieldName;
            this.ConverterName = converterName;
            this.converter = GenericFactory.GetInstance<ConverterBase>(this.ConverterName);
            this.ConverterParameter = converterParameter;
        }

        public ConverterDefinition(string fieldName, ConverterBase converter, string converterParameter)
        {
            this.FieldName = fieldName;
            this.Converter = converter;
            this.ConverterParameter = converterParameter;
        }

        // ***********************Properties***********************

        [XmlAttribute]
        public string FieldName
        {
            get { return this.fieldName; }

            set
            {
                if (this.fieldName != value)
                {
                    this.fieldName = value;
                    this.RaisePropertyChanged("FieldName");
                }
            }
        }

        [XmlAttribute]
        public string ConverterName
        {
            get { return this.converterName; }

            set
            {
                if (this.converterName != value)
                {
                    this.converterName = value;
                    this.RaisePropertyChanged("ConverterName");
                }
            }
        }

        [XmlIgnore]
        public ConverterBase Converter
        {
            get
            {
                if (this.converter == null && !string.IsNullOrEmpty(this.ConverterName))
                {
                    this.converter = GenericFactory.GetInstance<ConverterBase>(this.ConverterName);
                }

                return this.converter;
            }

            set
            {
                if (this.converter != value)
                {
                    this.converter = value;
                    this.RaisePropertyChanged("Converter");
                }
            }
        }

        [XmlAttribute]
        public string ConverterParameter { get; set; }
    }
}