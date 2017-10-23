using System;
using System.Text;
using System.Xml.Serialization;
using DataConnectors.Common.Helper;

namespace DataConnectors.Adapter.FileAdapter.ConnectionInfos
{
    [Serializable]
    public class FlatFileConnectionInfo : FileConnectionInfoBase
    {
        private Encoding encoding = Encoding.Default;

        [XmlIgnore]
        public Encoding Encoding
        {
            get { return this.encoding; }
            set { this.encoding = value; }
        }

        [XmlAttribute]
        public string EncodingName
        {
            get { return this.Encoding.WebName; }
            set { this.encoding = EncodingUtil.GetEncodingOrDefault(value); }
        }
    }
}