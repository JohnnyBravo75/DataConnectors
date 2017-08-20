using System.Collections.Generic;
using System.Data;
using System.Text;
using DataConnectors.Common.Model;
using DataConnectors.Formatters;

namespace DataConnectors.Adapter.FileAdapter
{
    public class FixedTextAdapter : DataAdapterBase
    {
        private readonly FlatFileAdapter fileAdapter = new FlatFileAdapter();

        public FixedTextAdapter()
        {
            this.fileAdapter.ReadFormatter = new FixedLengthToDataTableFormatter();
            this.fileAdapter.WriteFormatter = new DataTableToFixedLengthFormatter();

            // set to the same reference
            (this.fileAdapter.ReadFormatter as FixedLengthToDataTableFormatter).FieldDefinitions = (this.fileAdapter.WriteFormatter as DataTableToFixedLengthFormatter).FieldDefinitions;
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

        public FieldDefinitionList FieldDefinitions
        {
            get { return (this.fileAdapter.ReadFormatter as FixedLengthToDataTableFormatter).FieldDefinitions; }
        }

        public override IEnumerable<DataTable> ReadData(int? blockSize = null)
        {
            return this.fileAdapter.ReadData(blockSize);
        }

        public override bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false)
        {
            return this.fileAdapter.WriteData(tables, deleteBefore);
        }
    }
}