using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.ServiceModel;
using System.Xml.Serialization;
using DataConnectors.Common.Extensions;
using DataConnectors.Common.Helper;
using DataConnectors.Formatters.Model;

namespace DataConnectors.Formatters
{
    [Serializable]
    [ServiceKnownType("GetKnownTypes", typeof(KnownTypesProvider))]
    public abstract class FormatterBase
    {
        private FormatterOptions formatterOptions = new FormatterOptions();
        private CultureInfo defaultCulture = null;
        private List<string> errorData = new List<string>();

        [Browsable(false)]
        public List<string> ErrorData
        {
            get { return this.errorData; }
            protected set { this.errorData = value; }
        }

        [XmlArray("Options", IsNullable = false)]
        public FormatterOptions FormatterOptions
        {
            get { return this.formatterOptions; }
            set { this.formatterOptions = value; }
        }

        [XmlAttribute]
        public string Culture
        {
            get
            {
                return this.DefaultCulture.ToStringOrEmpty();
            }
            set
            {
                this.DefaultCulture = new CultureInfo(value);
            }

        }

        [XmlIgnore]
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

        public virtual object Format(object data, object existingData = null)
        {
            return data;
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}