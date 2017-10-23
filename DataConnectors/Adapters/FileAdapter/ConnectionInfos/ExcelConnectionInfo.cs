using System;
using System.Xml.Serialization;

namespace DataConnectors.Adapter.FileAdapter.ConnectionInfos
{
    [Serializable]
    public class ExcelConnectionInfo : FileConnectionInfoBase
    {
        private string sheetName = "";

        [XmlAttribute]
        public string SheetName
        {
            get { return this.sheetName; }
            set { this.sheetName = value; }
        }
    }
}