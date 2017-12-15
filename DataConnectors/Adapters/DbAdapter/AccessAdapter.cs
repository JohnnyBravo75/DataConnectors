using System.IO;
using System.Xml.Serialization;
using DataConnectors.Adapter.DbAdapter.ConnectionInfos;

namespace DataConnectors.Adapter.DbAdapter
{
    public class AccessAdapter : DbAdapter, IDataAdapterBase
    {
        private string quotePrefix = "[";
        private string quoteSuffix = "]";

        public AccessAdapter()
        {
            this.ConnectionInfo = new AccessConnectionInfo();
        }

        [XmlIgnore]
        protected override string QuotePrefix
        {
            get { return this.quotePrefix; }
            set { this.quotePrefix = value; }
        }

        [XmlIgnore]
        protected override string QuoteSuffix
        {
            get { return this.quoteSuffix; }
            set { this.quoteSuffix = value; }
        }

        [XmlIgnore]
        public string FileName
        {
            get
            {
                if (!(this.ConnectionInfo is AccessConnectionInfo))
                {
                    return string.Empty;
                }
                return (this.ConnectionInfo as AccessConnectionInfo).FileName;
            }
            set { (this.ConnectionInfo as AccessConnectionInfo).FileName = value; }
        }

        public bool CreateNewFile()
        {
            return this.CreateNewFile(this.ConnectionInfo.ConnectionString);
        }

        private bool CreateNewFile(string connectionString)
        {
            if (File.Exists(this.FileName))
            {
                return false;
            }

            // create access database file with the ADO-ActiveX
            ADOX.Catalog catalog = new ADOX.Catalog();
            catalog.Create(connectionString);
            catalog.ActiveConnection.Close();
            return true;
        }
    }
}