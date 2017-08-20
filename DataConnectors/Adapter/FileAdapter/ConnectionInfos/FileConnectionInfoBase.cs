using System;
using System.Xml.Serialization;

namespace DataConnectors.Adapter.FileAdapter.ConnectionInfos
{
    [Serializable]
    public class FileConnectionInfoBase : ConnectionInfoBase
    {
        private string fileName = "";

        [XmlAttribute]
        public string FileName
        {
            get { return this.fileName; }
            set { this.fileName = value; }
        }
    }
}