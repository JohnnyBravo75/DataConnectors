using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Xml.Serialization;

namespace DataConnectors.Adapter.DbAdapter.ConnectionInfos
{
    [Serializable]
    public class SqLiteConnectionInfo : DbConnectionInfoBase
    {
        private string database = "";

        public SqLiteConnectionInfo()
        {
            this.DbProvider = "System.Data.SQLite";
        }

        [XmlAttribute]
        public string Database
        {
            get { return this.database; }
            set { this.database = value; }
        }

        [XmlIgnore]
        public override string ConnectionString
        {
            get
            {
                return "Data Source=" + this.Database + ";Version=3;";
            }
        }

        [XmlIgnore]
        public override Dictionary<string, string> DataTypeMappings
        {
            get
            {
                return new Dictionary<string, string>()
                        {
                            { "System.String", "TEXT" },
                            { "System.DateTime", "TEXT" },
                            { "System.Int32", "INTEGER" },
                            { "System.Boolean", "INTEGER" }
                        };
            }
        }

        [XmlIgnore]
        public override DbProviderFactory DbProviderFactory
        {
            get
            {
                if (this.dbProviderFactory == null)
                {
                    this.dbProviderFactory = SQLiteFactory.Instance;
                }
                return this.dbProviderFactory;
            }
            protected set { this.dbProviderFactory = value; }
        }

        private void RegisterAdoProvider()
        {
            var systemData = ConfigurationManager.GetSection("system.data") as DataSet;
            if (systemData == null)
            {
                return;
            }

            var factoryTableIndex = systemData.Tables.IndexOf("DbProviderFactories");

            if (factoryTableIndex > -1)
            {
                // remove existing provider factory
                var sqlLiteRow = systemData.Tables[factoryTableIndex].Rows.Find("System.Data.SQLite");
                if (sqlLiteRow != null)
                {
                    systemData.Tables[factoryTableIndex].Rows.Remove(sqlLiteRow);
                }
            }
            else
            {
                systemData.Tables.Add("DbProviderFactories");
            }

            // add provider factory with our assembly in it.
            systemData.Tables[factoryTableIndex].Rows.Add(
                "SQLite Data Provider",
                ".NET Framework Data Provider for SQLite",
                "System.Data.SQLite",
                "System.Data.SQLite.SQLiteFactory, System.Data.SQLite"
            );
        }
    }
}