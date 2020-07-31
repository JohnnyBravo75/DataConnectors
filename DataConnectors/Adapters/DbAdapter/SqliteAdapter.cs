using System;
using System.Data.Common;
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
            this.UseTransaction = true;
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

            this.Connect();
            this.SetEncoding("UTF-8");
            this.Disconnect();

            return true;
        }

        private void SetEncoding(string encoding = "UTF-8", DbConnection connection = null)
        {
            if (connection == null)
            {
                connection = this.Connection;
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA encoding = \"{encoding}\"";
                cmd.ExecuteNonQuery();
            }
        }

        public override bool Connect()
        {
            this.Disconnect();
            this.Connection = this.OpenConnection(this.ConnectionInfo);

            this.SetEncoding("UTF-8");

            return this.Connection != null;
        }

        private DbConnection OpenConnection(DbConnectionInfoBase connectionInfo)
        {
            DbConnection connection = null;
            try
            {
                if (connectionInfo == null)
                {
                    throw new ArgumentNullException("DbConnectionInfo");
                }

                connection = new SQLiteConnection();
                connection.ConnectionString = connectionInfo.ConnectionString;

                connection.Open();
            }
            catch (Exception ex)
            {
                // Set the connection to null if it was created.
                connection = null;

                throw;
            }

            return connection;
        }
    }
}