using System;
using System.Data.SQLite;
using DataConnectors.Adapter.DbAdapter.ConnectionInfos;

namespace DataConnectors.Adapter.DbAdapter
{
    public class SqliteAdapter : DbAdapter, IDataAdapterBase
    {
        public SqliteAdapter()
        {
            this.ConnectionInfo = new SqLiteConnectionInfo();
        }

        public string FileName
        {
            get { return (this.ConnectionInfo as SqLiteConnectionInfo).Database; }
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