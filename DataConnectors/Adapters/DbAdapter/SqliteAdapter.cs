using System;
using System.Data.SQLite;
using System.Xml.Serialization;
using DataConnectors.Adapter.DbAdapter.ConnectionInfos;

namespace DataConnectors.Adapter.DbAdapter
{
    public class SqliteAdapter : DbAdapter, IDataAdapterBase
    {
        private string quotePrefix = "\"";
        private string quoteSuffix = "\"";

        public SqliteAdapter()
        {
            this.ConnectionInfo = new SqLiteConnectionInfo();
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

        public string FileName
        {
            get
            {
                if (!(this.ConnectionInfo is SqLiteConnectionInfo))
                {
                    return string.Empty;
                }
                return (this.ConnectionInfo as SqLiteConnectionInfo).Database;
            }
            set { (this.ConnectionInfo as SqLiteConnectionInfo).Database = value; }
        }

        public bool CreateNewFile()
        {
            return this.CreateNewFile(this.FileName);
        }

        private bool CreateNewFile(string fileName)
        {
            //if (File.Exists(fileName))
            //{
            //    return false;
            //}

            SQLiteConnection.CreateFile(fileName);

            return true;
        }

        public override bool Connect()
        {
            this.Disconnect();

            try
            {
                if (this.ConnectionInfo == null)
                {
                    throw new ArgumentNullException("DbConnectionInfo");
                }

                this.Connection = new SQLiteConnection();
                this.Connection.ConnectionString = this.ConnectionInfo.ConnectionString;

                this.Connection.Open();
            }
            catch (Exception ex)
            {
                // Set the connection to null if it was created.
                this.Connection = null;

                throw;
            }

            return this.Connection != null;
        }
    }
}