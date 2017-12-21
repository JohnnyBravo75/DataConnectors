using System;
using System.Collections.Generic;
using System.Data;
using DataConnectors.Converters;

namespace DataConnectors.Adapter
{
    public interface IDataAdapterBase : IDisposable
    {
        //ConnectionInfoBase ConnectionInfo { get; set; }

        //bool Connect();

        //bool Disconnect();

        //bool IsConnected { get; }

        ValueConvertProcessor ReadConverter { get; set; }

        ValueConvertProcessor WriteConverter { get; set; }

        IList<DataColumn> GetAvailableColumns();

        IList<string> GetAvailableTables();

        int GetCount();

        DataTable ReadAllData();

        IEnumerable<Dictionary<string, object>> ReadAllDataAs();

        IEnumerable<TObj> ReadAllDataAs<TObj>() where TObj : class;

        IEnumerable<DataTable> ReadData(int? blockSize = default(int?));

        IEnumerable<Dictionary<string, object>> ReadDataAs(int? blockSize = default(int?));

        IEnumerable<TObj> ReadDataAs<TObj>(int? blockSize = default(int?)) where TObj : class;

        void WriteAllData(DataTable table, bool deleteBefore = false);

        bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false);

        bool WriteDataFrom<TObj>(IEnumerable<TObj> objects, bool deleteBefore = false, int? blockSize = default(int?)) where TObj : class;
    }
}