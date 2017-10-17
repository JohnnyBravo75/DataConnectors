using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
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

        public XmlAdapter(string fileName)
        {
            this.ConnectionInfo = new FlatFileConnectionInfo();
            this.FileName = fileName;
        }

        public XmlAdapter(Stream dataStream)
        {
            this.ConnectionInfo = new FlatFileConnectionInfo();
            this.DataStream = dataStream;
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

        public Stream DataStream { get; set; }

        public override IList<DataColumn> GetAvailableColumns()
        {
            return new List<DataColumn>();
        }

        public override IList<string> GetAvailableTables()
        {
            IList<string> userTableList = new List<string>();

            XmlTextReader reader = null;
            if (!string.IsNullOrEmpty(this.FileName))
            {
                reader = new XmlTextReader(this.FileName);
            }
            else if (this.DataStream != null)
            {
                reader = new XmlTextReader(this.DataStream);
            }

            if (reader == null)
            {
                return userTableList;
            }

            var elementList = new List<string>();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    // add opening element to the path list
                    elementList.Add(reader.Name);

                    // build the current path e.g. /main/result/address/street
                    var currentPathBuilder = new StringBuilder();
                    foreach (var element in elementList)
                    {
                        currentPathBuilder.Append("/").Append(element);
                    }
                    var currentPath = currentPathBuilder.ToString();

                    // don´t add doublets, ensure paths are distinct
                    if (!userTableList.Contains(currentPath))
                    {
                        userTableList.Add(currentPath);
                    }
                }

                if (reader.NodeType == XmlNodeType.EndElement || reader.IsEmptyElement)
                {
                    // when end element, remove from the path list
                    var lastElement = elementList.LastOrDefault();
                    if (lastElement == null || lastElement == reader.Name)
                    {
                        elementList.RemoveAt(elementList.Count - 1);
                    }
                }
            }

            reader.Close();
            reader.Dispose();

            return userTableList;
        }

        public override int GetCount()
        {
            int count = 0;

            var xPathIterator = this.CreateXPathIterator();
            if (xPathIterator == null)
            {
                return count;
            }

            // loop through all paths and count them
            while (xPathIterator.MoveNext())
            {
                count++;
            }

            return count;
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

            var xPathIterator = this.CreateXPathIterator();
            if (xPathIterator == null)
            {
                yield return result;
            }

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
                this.ReadConverter.ExecuteConverters(result);

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
            var xmlDoc = new XmlDocument();
            var namespaceMgr = new XmlNamespaceManager(xmlDoc.NameTable);

            if (!string.IsNullOrEmpty(this.FileName))
            {
                DirectoryUtil.CreateDirectoryIfNotExists(Path.GetDirectoryName(this.FileName));

                if (deleteBefore)
                {
                    FileUtil.DeleteFileIfExists(this.FileName);
                }

                if (this.IsNewFile(this.FileName))
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
            XmlWriter writer = null;

            if (!string.IsNullOrEmpty(this.FileName))
            {
                writer = XmlWriter.Create(this.FileName, settings);
            }
            else if (this.DataStream != null)
            {
                writer = XmlWriter.Create(this.DataStream, settings);
            }
            else
            {
                return false;
            }

            xmlDoc.Save(writer);

            writer.Close();
            writer.Dispose();

            return true;
        }

        private XPathNodeIterator CreateXPathIterator()
        {
            XPathNavigator xPathNavigator = null;

            if (!string.IsNullOrEmpty(this.FileName))
            {
                xPathNavigator = new XPathDocument(this.FileName).CreateNavigator();
            }
            else if (this.DataStream != null)
            {
                xPathNavigator = new XPathDocument(this.DataStream).CreateNavigator();
            }
            else
            {
                return null;
            }

            var xPathExpr = xPathNavigator.Compile(this.XPath);

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