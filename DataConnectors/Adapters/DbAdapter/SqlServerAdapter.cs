using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DataConnectors.Adapter.DbAdapter
{
    public class SqlServerAdapter : DbAdapter, IDataAdapterBase
    {
        public override bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false)
        {
            if (!this.IsConnected)
            {
                this.Connect();
            }

            using (var cmd = this.connection.CreateCommand() as SqlCommand)
            {
                //var transaction = cmd.Connection.BeginTransaction();

                //cmd.Transaction = transaction;
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

                    try
                    {
                        // make sure to enable triggers
                        // more on triggers in next post
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy
                            (
                            this.connection as SqlConnection,
                            SqlBulkCopyOptions.TableLock |
                            SqlBulkCopyOptions.FireTriggers |
                            SqlBulkCopyOptions.UseInternalTransaction,
                            null
                            ))
                        {
                            // set the destination table name
                            bulkCopy.DestinationTableName = table.TableName;

                            // column mapping when required
                            foreach (DataColumn col in table.Columns)
                            {
                                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                            }

                            // write the data in the "dataTable"
                            bulkCopy.WriteToServer(table);
                        }

                        //if (transaction != null &&
                        //    transaction.Connection != null &&
                        //    transaction.Connection.State == ConnectionState.Open)
                        //{
                        //    transaction.Commit();
                        //}
                    }
                    finally
                    {
                        //if (transaction != null &&
                        //    transaction.Connection != null &&
                        //    transaction.Connection.State == ConnectionState.Open)
                        //{
                        //    transaction.Commit();
                        //}
                    }

                    tblCount++;
                }
            }

            return true;
        }
    }
}