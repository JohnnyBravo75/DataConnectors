using System;
using System.Collections.Generic;
using System.Data;

namespace DataConnectors.Adapter
{
    public interface IDataAdapterBase : IDisposable
    {
        //ConnectionInfoBase ConnectionInfo { get; set; }

        //bool Connect();

        //bool Disconnect();

        //IList<DataColumn> GetAvailableColumns();

        //IList<string> GetAvailableTables();

        //bool IsConnected { get; }

        //int GetCount();

        IEnumerable<DataTable> ReadData(int? blockSize = null);

        bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false);

        IEnumerable<TObj> ReadDataAs<TObj>(int? blockSize = null) where TObj : class;

        IEnumerable<Dictionary<string, object>> ReadDataAs(int? blockSize = null);
    }
}