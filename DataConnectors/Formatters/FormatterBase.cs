using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ServiceModel;
using System.Xml.Serialization;
using DataConnectors.Common.Helper;
using DataConnectors.Formatters.Model;

namespace DataConnectors.Formatters
{
    [Serializable]
    [ServiceKnownType("GetKnownTypes", typeof(KnownTypesProvider))]
    public abstract class FormatterBase
    {
        private FormatterOptions formatterOptions = new FormatterOptions();

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