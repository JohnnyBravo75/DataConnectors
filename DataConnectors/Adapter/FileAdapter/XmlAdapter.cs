using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using DataConnectors.Adapter.FileAdapter.ConnectionInfos;
using DataConnectors.Common.Helper;
using DataConnectors.Formatters;
using DataConnectors.Formatters.Model;

namespace DataConnectors.Adapter.FileAdapter
{
    public class XmlAdapter : DataAdapterBase
    {
        private FormatterBase readFormatter = new XPathToDataTableFormatter();
        private FormatterBase writeFormatter = new DataTableToXPathFormatter();

        private List<XmlNameSpace> xmlNameSpaces = new List<XmlNameSpace>();
        private string xPath;
        private FileConnectionInfoBase connectionInfo;

        public XmlAdapter()
        {
            this.ConnectionInfo = new FlatFileConnectionInfo();
        }

        [XmlElement]
        public List<XmlNameSpace> XmlNameSpaces
        {
            get { return this.xmlNameSpaces; }
            set { this.xmlNameSpaces = value; }
        }

        [XmlElement]
        public FormatterBase ReadFormatter
        {
            get { return this.readFormatter; }
            set { this.readFormatter = value; }
        }

        [XmlElement]
        public FormatterBase WriteFormatter
        {
            get { return this.writeFormatter; }
            set { this.writeFormatter = value; }
        }

        [XmlElement]
        public FileConnectionInfoBase ConnectionInfo
        {
            get { return this.connectionInfo; }
            set { this.connectionInfo = value; }
        }

        [XmlAttribute]
        public string FileName
        {
            get { return (this.ConnectionInfo as FlatFileConnectionInfo).FileName; }
            set { (this.ConnectionInfo as FlatFileConnectionInfo).FileName = value; }
        }

        [XmlAttribute]
        public string XPath
        {
            get { return this.xPath; }
            set { this.xPath = value; }
        }

        public override IEnumerable<DataTable> ReadData(int? blockSize = null)
        {
            foreach (object dataObj in this.ReadDataObjects<object>(blockSize))
            {
                if (dataObj is DataSet)
                {
                    foreach (DataTable table in (dataObj as DataSet).Tables)
                    {
                        yield return table;
                    }
                }
                else if (dataObj is DataTable)
                {
                    yield return dataObj as DataTable;
                }
                else
                {
                    yield return new DataTable();
                }
            }
        }

        private IEnumerable<TObj> ReadDataObjects<TObj>(int? blockSize = null) where TObj : class
        {
            DataSet dataSet = new DataSet();
            TObj result = default(TObj);
            int readedRows = 0;

            var xPathIterator = this.CreateXPathIterator(this.FileName, this.XPath);

            // when formatter supports namespaces, and has no, add to them
            if (this.ReadFormatter is IHasXmlNameSpaces && (this.ReadFormatter as IHasXmlNameSpaces).XmlNameSpaces.Count == 0)
            {
                (this.ReadFormatter as IHasXmlNameSpaces).XmlNameSpaces = this.XmlNameSpaces;
            }

            while (xPathIterator.MoveNext())
            {
                string xml = xPathIterator.Current.OuterXml;

                if (readedRows == 0)
                {
                    dataSet = new DataSet();
                }

                object tmpResult = this.ReadFormatter.Format(xml, dataSet);

                result = tmpResult as TObj;

                readedRows++;

                if (blockSize.HasValue && (blockSize > 0 && readedRows >= blockSize))
                {
                    readedRows = 0;
                    yield return result;
                }
            }

            if (readedRows > 0)
            {
                yield return result;
            }
        }

        public override bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false)
        {
            var fileName = this.FileName;

            DirectoryUtil.CreateDirectoryIfNotExists(Path.GetDirectoryName(fileName));

            if (deleteBefore)
            {
                FileUtil.DeleteFileIfExists(fileName);
            }

            var xmlDoc = new XmlDocument();
            var namespaceMgr = new XmlNamespaceManager(xmlDoc.NameTable);

            var isNewFile = this.IsNewFile(fileName);

            if (isNewFile)
            {
                // add Declaration
                XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(docNode);

                // create base Path
                XPathUtil.CreateXPath(xmlDoc, this.XPath);
            }
            else
            {
                xmlDoc.Load(this.FileName);
            }

            foreach (DataTable table in tables)
            {
                var xmlLines = this.WriteFormatter.Format(table, null) as IEnumerable<string>;

                int writtenRows = 0;
                int rowIdx = 0;

                if (xmlLines != null)
                {
                    foreach (var xmlLine in xmlLines)
                    {
                        var lastNode = xmlDoc.SelectSingleNode(this.XPath + "[last()]", namespaceMgr);

                        if (lastNode != null)
                        {
                            // Append xml to the last node
                            var xmlDocFragment = xmlDoc.CreateDocumentFragment();
                            xmlDocFragment.InnerXml = xmlLine;
                            lastNode.AppendChild(xmlDocFragment);

                            writtenRows++;
                        }

                        rowIdx++;
                    }
                }
            }

            var settings = new XmlWriterSettings { Indent = true };
            using (XmlWriter writer = XmlWriter.Create(this.FileName, settings))
            {
                xmlDoc.Save(writer);
                writer.Close();
            }

            return true;
        }

        private XPathNodeIterator CreateXPathIterator(string file, string xPath)
        {
            var xPathNavigator = new XPathDocument(file).CreateNavigator();
            var xPathExpr = xPathNavigator.Compile(xPath);

            var nsMgr = new XmlNamespaceManager(new NameTable());
            foreach (var xmlNameSpace in this.XmlNameSpaces)
            {
                nsMgr.AddNamespace(xmlNameSpace.Prefix, xmlNameSpace.NameSpace);
            }

            xPathExpr.SetContext(nsMgr);

            var xPathIterator = xPathNavigator.Select(xPathExpr);
            return xPathIterator;
        }

        private bool IsNewFile(string fileName)
        {
            // new File?
            if (!string.IsNullOrEmpty(fileName) && !File.Exists(fileName))
            {
                return true;
            }

            return false;
        }
    }
}