using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using DataConnectors.Adapter.DbAdapter.ConnectionInfos;
using DataConnectors.Common.Extensions;
using Oracle.ManagedDataAccess.Client;

namespace DataConnectors.Adapter.DbAdapter
{
    public class OracleAdapter : DbAdapter, IDataAdapterBase
    {
        private string BuildInsertSql(string tableName, List<string> columnNames)
        {
            // build the commandtext
            var sqlColumns = new StringBuilder();
            var sqlValues = new StringBuilder();
            int colIdx = 0;

            foreach (var columnName in columnNames)
            {
                if (colIdx > 0)
                {
                    sqlColumns.Append(",");
                    sqlValues.Append(",");
                }

                sqlColumns.Append(this.QuoteIdentifier(columnName));
                sqlValues.Append(":" + columnName);
                colIdx++;
            }

            var query = new StringBuilder();
            query.Append("INSERT INTO " + tableName);
            query.Append(" (");
            query.Append(sqlColumns);
            query.Append(" )");
            query.Append(" VALUES ");
            query.Append("(");
            query.Append(sqlValues);
            query.Append(")");

            return query.ToString();
        }

        public override bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false)
        {
            if (!this.IsConnected)
            {
                this.Connect();
            }

            using (var cmd = this.connection.CreateCommand() as OracleCommand)
            {
                var transaction = cmd.Connection.BeginTransaction();

                cmd.Transaction = transaction;
                int tblCount = 0;

                foreach (DataTable table in tables)
                {
                    if (!string.IsNullOrEmpty(this.TableName))
                    {
                        table.TableName = this.TableName;
                    }

                    // check in the first run...
                    if (tblCount == 0)
                    {
                        // create a table when not exists
                        if (!this.ExistsTable(table.TableName))
                        {
                            this.CreateTable(table, withContraints: false);
                        }
                        // delete all before
                        else if (deleteBefore)
                        {
                            this.DeleteData();
                        }
                    }

                    var columnNames = new List<string>();
                    foreach (DataColumn column in table.Columns)
                    {
                        columnNames.Add(column.ColumnName);
                    }

                    try
                    {
                        if (string.IsNullOrEmpty(cmd.CommandText))
                        {
                            cmd.CommandText = this.BuildInsertSql(table.TableName, columnNames);
                            cmd.BindByName = true;
                        }

                        cmd.ArrayBindCount = table.Rows.Count;

                        // add all parameters
                        cmd.Parameters.Clear();

                        object cellValue = "";

                        foreach (var columnName in columnNames)
                        {
                            var bulkValues = new List<object>();

                            // all values from one column
                            foreach (DataRow row in table.Rows)
                            {
                                cellValue = row[columnName].ToStringOrEmpty();
                                if (string.IsNullOrEmpty(cellValue as string))
                                {
                                    cellValue = DBNull.Value;
                                }

                                bulkValues.Add(cellValue);
                            }

                            cmd.Parameters.Add((":" + columnName), OracleDbType.Varchar2, bulkValues.ToArray(), ParameterDirection.Input);
                        }

                        // execute
                        cmd.ExecuteNonQuery();

                        if (transaction != null &&
                            transaction.Connection != null &&
                            transaction.Connection.State == ConnectionState.Open)
                        {
                            transaction.Commit();
                        }
                    }
                    catch (OracleException ex)
                    {
                        if (ex.Errors != null)
                        {
                            foreach (OracleError error in ex.Errors)
                            {
                                // erster Error ist immer eine DML-Array Error, d.h. wenn ein Fehler auftritt,
                                // bekommt man also 2 Errors, zuerst immer den gernerischen DML-Array Error und dann den wirklichen Error
                                // den ersten einfach Ignorieren... danach sind die Indexe verschoben und die Erroranzhal stimmt nicht, überall +1 deswegen
                                if (error.ArrayBindIndex == -1)
                                {
                                    continue;
                                }

                                var row = new Dictionary<string, object>();

                                foreach (OracleParameter parameter in cmd.Parameters)
                                {
                                    if (parameter.Value is long[])
                                    {
                                        var idArray = (parameter.Value as long[]);
                                        row.Add(parameter.ParameterName.TrimStart(':'), idArray[error.ArrayBindIndex]);
                                    }
                                    else if (parameter.Value is string[])
                                    {
                                        var strArray = (parameter.Value as string[]);
                                        row.Add(parameter.ParameterName.TrimStart(':'), strArray[error.ArrayBindIndex]);
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (transaction != null &&
                            transaction.Connection != null &&
                            transaction.Connection.State == ConnectionState.Open)
                        {
                            transaction.Commit();
                        }
                    }

                    tblCount++;
                }
            }

            return true;
        }
    }
}