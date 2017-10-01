using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using DataConnectors.Adapter.FileAdapter.ConnectionInfos;
using DataConnectors.Common.Helper;
using DataConnectors.Formatters;

namespace DataConnectors.Adapter.FileAdapter
{
    public class FlatFileAdapter : DataAdapterBase
    {
        private FormatterBase readFormatter = new FlatFileToDataTableFormatter();
        private FormatterBase writeFormatter = new DefaultFormatter();

        private string recordSeperator = Environment.NewLine;

        private FileConnectionInfoBase connectionInfo;

        public FileConnectionInfoBase ConnectionInfo
        {
            get
            {
                return this.connectionInfo;
            }
            set
            {
                this.connectionInfo = value;
            }
        }

        public FlatFileAdapter()
        {
            this.ConnectionInfo = new FlatFileConnectionInfo();
        }

        public string RecordSeperator
        {
            get { return this.recordSeperator; }
            set { this.recordSeperator = value; }
        }

        public FormatterBase ReadFormatter
        {
            get { return this.readFormatter; }
            set { this.readFormatter = value; }
        }

        public FormatterBase WriteFormatter
        {
            get { return this.writeFormatter; }
            set { this.writeFormatter = value; }
        }

        [XmlAttribute]
        public string FileName
        {
            get { return (this.ConnectionInfo as FlatFileConnectionInfo).FileName; }
            set { (this.ConnectionInfo as FlatFileConnectionInfo).FileName = value; }
        }

        [XmlAttribute]
        public Encoding Encoding
        {
            get { return (this.ConnectionInfo as FlatFileConnectionInfo).Encoding; }
            set { (this.ConnectionInfo as FlatFileConnectionInfo).Encoding = value; }
        }

        private bool IsNewFile(string fileName)
        {
            // new File?
            if (!string.IsNullOrEmpty(fileName) && !File.Exists(fileName))
            {
                return true;
            }

            return false;
        }

        public override IEnumerable<DataTable> ReadData(int? blockSize = null)
        {
            DataTable headerTable = null;
            var lines = new List<string>();
            using (var reader = new StreamReader(this.FileName, this.Encoding))
            {
                int readedRows = 0;
                int rowIdx = 0;
                DataTable table = new DataTable();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    lines.Add(line);
                    rowIdx++;

                    //if (this.skipRows > 0 && rowIdx < this.skipRows)
                    //{
                    //    continue;
                    //}

                    // first row (header?)
                    if (readedRows == 0)
                    {
                        DataTableHelper.DisposeTable(table);

                        table = headerTable != null
                            ? headerTable.Clone()
                            : null;
                    }

                    readedRows++;

                    if (blockSize.HasValue && blockSize > 0 && readedRows >= blockSize)
                    {
                        table = this.ReadFormatter.Format(lines, table) as DataTable;
                        if (table != null)
                        {
                            table.TableName = Path.GetFileNameWithoutExtension(this.FileName);

                            if (headerTable == null)
                            {
                                headerTable = table.Clone();
                                lines.Clear();
                                continue;
                            }
                        }
                        else
                        {
                            table = new DataTable();
                        }

                        lines.Clear();
                        readedRows = 0;

                        yield return table;
                    }
                }

                if (readedRows > 0 || table == null)
                {
                    table = this.ReadFormatter.Format(lines, table) as DataTable;
                    if (table != null)
                    {
                        table.TableName = Path.GetFileNameWithoutExtension(this.FileName);
                    }
                    else
                    {
                        table = new DataTable();
                    }

                    yield return table;
                }

                DataTableHelper.DisposeTable(table);
            }
        }

        public override IList<DataColumn> GetAvailableColumns()
        {
            IList<DataColumn> tableColumnList = new List<DataColumn>();

            var header = this.ReadData(1).FirstOrDefault();

            if (header != null)
            {
                foreach (DataColumn column in header.Columns)
                {
                    var field = new DataColumn(column.ColumnName);
                    tableColumnList.Add(field);
                }
            }

            return tableColumnList;
        }

        public override IList<string> GetAvailableTables()
        {
            IList<string> userTableList = new List<string>();

            if (string.IsNullOrEmpty(this.FileName))
            {
                return userTableList;
            }

            if (File.Exists(this.FileName))
            {
                userTableList.Add(this.FileName);
            }

            return userTableList;
        }

        public override int GetCount()
        {
            int count = 0;

            if (string.IsNullOrEmpty(this.FileName))
            {
                return count;
            }

            using (TextReader reader = new StreamReader(this.FileName))
            {
                while (reader.ReadLine() != null)
                {
                    count++;
                }
            }

            return count;
        }

        //public void WriteBinaryData(object data, bool deleteBefore = false)
        //{
        //    var fileName = this.FileName;

        //    if (string.IsNullOrEmpty(fileName))
        //    {
        //        return;
        //    }

        //    DirectoryUtil.CreateDirectoryIfNotExists(Path.GetDirectoryName(fileName));

        //    if (deleteBefore)
        //    {
        //        FileUtil.DeleteFileIfExists(fileName);
        //    }

        //    if (data is byte[])
        //    {
        //        using (FileStream stream = new FileStream(fileName, FileMode.Create))
        //        {
        //            using (BinaryWriter writer = new BinaryWriter(stream))
        //            {
        //                writer.Write(data as byte[]);
        //                writer.Close();
        //            }
        //        }
        //    }
        //    else
        //    {
        //        using (var writer = new StreamWriter(fileName, true, this.Encoding))
        //        {
        //            writer.Write(data);
        //            writer.Close();
        //        }
        //    }
        //}

        public override bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false)
        {
            string lastFileName = "";
            string fileName = "";
            bool isNewFile = true;
            StreamWriter writer = null;

            foreach (DataTable table in tables)
            {
                if (writer == null || lastFileName != this.FileName)
                {
                    fileName = this.FileName;

                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = table.TableName;
                    }

                    if (string.IsNullOrEmpty(fileName))
                    {
                        return false;
                    }

                    DirectoryUtil.CreateDirectoryIfNotExists(Path.GetDirectoryName(fileName));

                    if (deleteBefore)
                    {
                        FileUtil.DeleteFileIfExists(fileName);
                    }

                    isNewFile = this.IsNewFile(fileName);

                    if (writer != null)
                    {
                        writer.Flush();
                        writer.Close();
                        writer.Dispose();
                    }

                    writer = new StreamWriter(fileName, !isNewFile, this.Encoding);

                    lastFileName = fileName;
                }

                writer.NewLine = this.recordSeperator;

                var lines = this.WriteFormatter.Format(table) as IEnumerable<string>;

                int writtenRows = 0;
                int rowIdx = 0;

                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        if (!isNewFile && rowIdx == 0)
                        {
                            // skip header when it is no new fileName
                            rowIdx++;
                            continue;
                        }

                        writer.WriteLine(line);

                        if (writtenRows % 100 == 0)
                        {
                            writer.Flush();
                        }

                        writtenRows++;
                        rowIdx++;
                    }

                    writer.Flush();
                }

                isNewFile = this.IsNewFile(fileName);
            }

            if (writer != null)
            {
                writer.Close();
                writer.Dispose();
            }

            return true;
        }

        public Encoding AutoDetectEncoding(string fileName)
        {
            Encoding encoding = Encoding.Default;

            try
            {
                using (Stream reader = File.OpenRead(fileName))
                {
                    encoding = EncodingUtil.DetectEncoding(reader);
                }
            }
            catch (Exception ex)
            {
                encoding = Encoding.Default;
            }

            return encoding;
        }
    }
}