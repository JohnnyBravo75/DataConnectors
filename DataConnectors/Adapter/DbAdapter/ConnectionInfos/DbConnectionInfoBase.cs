﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Xml.Serialization;

namespace DataConnectors.Adapter.DbAdapter.ConnectionInfos
{
    [Serializable]
    public class DbConnectionInfoBase : ConnectionInfoBase
    {
        private Dictionary<string, string> dataTypeMappings = new Dictionary<string, string>();
        private string connectionString = "";
        private string userName = "";
        private string dbProvider = "System.Data.OleDb";
        protected DbProviderFactory dbProviderFactory;

        [XmlIgnore]
        public virtual Dictionary<string, string> DataTypeMappings
        {
            get { return this.dataTypeMappings; }
            protected set { this.dataTypeMappings = value; }
        }

        [XmlIgnore]
        public virtual string ConnectionString
        {
            get { return this.connectionString; }
            protected set { this.connectionString = value; }
        }

        [XmlIgnore]
        public string DbProvider
        {
            get { return this.dbProvider; }
            set { this.dbProvider = value; }
        }

        [XmlAttribute]
        public string UserName
        {
            get { return this.userName; }
            set { this.userName = value; }
        }

        [XmlIgnore]
        public virtual DbProviderFactory DbProviderFactory
        {
            get
            {
                if (this.dbProviderFactory == null)
                {
                    this.dbProviderFactory = DbProviderFactories.GetFactory(this.DbProvider);
                }
                return this.dbProviderFactory;
            }
            protected set { this.dbProviderFactory = value; }
        }
    }
}