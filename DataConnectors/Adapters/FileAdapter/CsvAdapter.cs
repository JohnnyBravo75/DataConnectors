using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using DataConnectors.Converters;
using DataConnectors.Formatters;

namespace DataConnectors.Adapter.FileAdapter
{
    public class CsvAdapter : DataAdapterBase
    {
        private readonly FlatFileAdapter fileAdapter = new FlatFileAdapter();

        public CsvAdapter()
        {
            this.fileAdapter.ReadFormatter = new CsvToDataTableFormatter() { Separator = ";" };
            this.fileAdapter.WriteFormatter = new DataTableToCsvFormatter() { Separator = ";" };
        }

        public CsvAdapter(string filenName) : this()
        {
            this.FileName = filenName;
        }

        public CsvAdapter(Stream dataStream) : this()
        {
            this.DataStream = dataStream;
        }

        public Encoding Encoding
        {
            get
            {
                return this.fileAdapter.Encoding;
            }

            set
            {
                this.fileAdapter.Encoding = value;
            }
        }

        public string FileName
        {
            get
            {
                return this.fileAdapter.FileName;
            }

            set
            {
                this.fileAdapter.FileName = value;
            }
        }

        public Stream DataStream
        {
            get
            {
                return this.fileAdapter.DataStream;
            }
            set
            {
                this.fileAdapter.DataStream = value;
            }
        }

        public string Separator
        {
            get
            {
                if (!(this.fileAdapter.ReadFormatter is CsvToDataTableFormatter))
                {
                    return string.Empty;
                }

                return (this.fileAdapter.ReadFormatter as CsvToDataTableFormatter).Separator;
            }
            set
            {
                (this.fileAdapter.ReadFormatter as CsvToDataTableFormatter).Separator = value;
                (this.fileAdapter.WriteFormatter as DataTableToCsvFormatter).Separator = value;
            }
        }

        public string Enclosure
        {
            get
            {
                if (!(this.fileAdapter.ReadFormatter is CsvToDataTableFormatter))
                {
                    return string.Empty;
                }

                return (this.fileAdapter.ReadFormatter as CsvToDataTableFormatter).Enclosure;
            }
            set
            {
                (this.fileAdapter.ReadFormatter as CsvToDataTableFormatter).Enclosure = value;
                (this.fileAdapter.WriteFormatter as DataTableToCsvFormatter).Enclosure = value;
            }
        }

        public ValueConvertProcessor ReadConverter
        {
            get { return this.fileAdapter.ReadConverter; }
            set { this.fileAdapter.ReadConverter = value; }
        }

        public ValueConvertProcessor WriteConverter
        {
            get { return this.fileAdapter.WriteConverter; }
            set { this.fileAdapter.WriteConverter = value; }
        }

        public override IList<DataColumn> GetAvailableColumns()
        {
            return this.fileAdapter.GetAvailableColumns();
        }

        public override IList<string> GetAvailableTables()
        {
            return this.fileAdapter.GetAvailableTables();
        }

        public override int GetCount()
        {
            return this.fileAdapter.GetCount();
        }

        public override IEnumerable<DataTable> ReadData(int? blockSize = null)
        {
            return this.fileAdapter.ReadData(blockSize);
        }

        public override bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false)
        {
            return this.fileAdapter.WriteData(tables, deleteBefore);
        }

        public override void Dispose()
        {
            if (this.fileAdapter != null)
            {
                this.fileAdapter.Dispose();
            }
        }
    }
}