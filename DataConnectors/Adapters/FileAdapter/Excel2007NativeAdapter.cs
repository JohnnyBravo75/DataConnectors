﻿using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml.Serialization;
using DataConnectors.Adapter.FileAdapter.ConnectionInfos;
using DataConnectors.Common.Helper;
using OfficeOpenXml;

namespace DataConnectors.Adapter.FileAdapter
{
    public class Excel2007NativeAdapter : DataAdapterBase, IDataAdapterBase
    {
        // ***********************Fields***********************

        private ExcelPackage excelPackage;

        protected int importRowIndex = 0;
        protected StreamReader importReader;

        protected int exportRowIndex = 0;
        protected StreamWriter exportWriter;

        // ***********************Constructors***********************

        public Excel2007NativeAdapter()
        {
            this.ConnectionInfo = new ExcelConnectionInfo();
        }

        private ConnectionInfoBase connectionInfo;

        public ConnectionInfoBase ConnectionInfo
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

        [XmlAttribute]
        public string FileName
        {
            get
            {
                if (!(this.ConnectionInfo is ExcelConnectionInfo))
                {
                    return string.Empty;
                }
                return (this.ConnectionInfo as ExcelConnectionInfo).FileName;
            }
            set { (this.ConnectionInfo as ExcelConnectionInfo).FileName = value; }
        }

        [XmlAttribute]
        public string SheetName
        {
            get
            {
                if (!(this.ConnectionInfo is ExcelConnectionInfo))
                {
                    return string.Empty;
                }
                return (this.ConnectionInfo as ExcelConnectionInfo).SheetName;
            }
            set { (this.ConnectionInfo as ExcelConnectionInfo).SheetName = value; }
        }

        [XmlIgnore]
        public bool IsConnected { get; protected set; }

        // ***********************Functions***********************

        public bool Connect()
        {
            this.Disconnect();

            if (string.IsNullOrEmpty(this.FileName))
            {
                return false;
            }

            //if (!File.Exists(this.fileName))
            //{
            //    return false;
            //}

            this.excelPackage = new ExcelPackage(new FileInfo(this.FileName));

            if (this.excelPackage != null)
            {
                this.IsConnected = true;
            }

            return this.IsConnected;
        }

        public void CreateNewFile()
        {
            this.excelPackage = new ExcelPackage(new FileInfo(this.FileName));

            ExcelWorksheet sheet = this.excelPackage.Workbook.Worksheets[this.SheetName];

            if (sheet == null)
            {
                sheet = this.excelPackage.Workbook.Worksheets.Add(this.SheetName);
            }

            this.excelPackage.Save();
        }

        public bool Disconnect()
        {
            if (this.excelPackage != null)
            {
                this.excelPackage.Dispose();
                this.excelPackage = null;
            }

            if (this.excelPackage == null)
            {
                this.IsConnected = false;
            }

            this.startX = 0;
            this.startY = 1;

            return (this.excelPackage == null);
        }

        public override void Dispose()
        {
            this.Disconnect();
        }

        public override IList<DataColumn> GetAvailableColumns()
        {
            var tableColumnList = new List<DataColumn>();

            if (this.excelPackage == null)
            {
                return tableColumnList;
            }

            ExcelWorksheet sheet = this.excelPackage.Workbook.Worksheets[this.SheetName];

            if (sheet == null)
            {
                return tableColumnList;
            }

            // read the headers
            int colCnt = sheet.Dimension.End.Column;
            for (int x = 0; x < colCnt; x++)
            {
                string cellValue = sheet.Cells[1, 1 + x].Value.ToString();

                if (string.IsNullOrEmpty(cellValue))
                {
                    break;
                }

                var field = new DataColumn(cellValue);
                tableColumnList.Add(field);
            }

            return tableColumnList;
        }

        public override IList<string> GetAvailableTables()
        {
            IList<string> userTableList = new List<string>();

            if (this.excelPackage == null)
            {
                return userTableList;
            }

            foreach (var sheet in this.excelPackage.Workbook.Worksheets)
            {
                userTableList.Add(sheet.Name);
            }

            return userTableList;
        }

        public override int GetCount()
        {
            int count = 0;
            if (!this.IsConnected)
            {
                return count;
            }

            ExcelWorksheet sheet = this.excelPackage.Workbook.Worksheets[this.SheetName];

            if (sheet.Dimension == null)
            {
                return 0;
            }

            count = sheet.Dimension.End.Row;
            return count;
        }

        public void ResetToStart()
        {
            this.importRowIndex = 0;
            if (this.importReader != null)
            {
                this.importReader.BaseStream.Position = 0;
                this.importReader.DiscardBufferedData();
            }

            this.exportRowIndex = 0;
            if (this.exportWriter != null)
            {
                this.exportWriter.BaseStream.Position = 0;
            }
        }

        public override IEnumerable<DataTable> ReadData(int? blockSize = null)
        {
            this.ResetToStart();

            bool hasHeader = true;
            DataRow tableRow = null;
            DataTable table = null;
            string tableName = this.SheetName;

            if (!this.IsConnected)
            {
                yield return table;
            }

            // create a new datatable
            table = new DataTable(tableName);
            table.TableName = tableName;

            if (this.excelPackage == null)
            {
                yield return table;
            }

            ExcelWorksheet sheet = this.excelPackage.Workbook.Worksheets[tableName];

            if (sheet == null)
            {
                yield return table;
            }

            int rowCnt = sheet.Dimension.End.Row;
            int colCnt = sheet.Dimension.End.Column;
            int rowsRead = 0;
            //int y = 0;

            // loop the rows
            for (int y = 0; y < rowCnt; y++)
            {
                y = this.importRowIndex;

                // first row?
                if (y == 0)
                {
                    // read the headers and create the columns
                    for (int x = 0; x < colCnt; x++)
                    {
                        object cellValue = sheet.Cells[1, 1 + x].Value;

                        if (cellValue == null)
                        {
                            break;
                        }

                        if (string.IsNullOrEmpty(cellValue.ToString()))
                        {
                            break;
                        }

                        table.Columns.Add(cellValue.ToString(), typeof(string));
                    }
                }

                if (!(y == 0 && hasHeader))
                {
                    tableRow = table.NewRow();

                    // loop the columns
                    for (int x = 0; x < table.Columns.Count; x++)
                    {
                        tableRow[x] = sheet.Cells[1 + y, 1 + x].Value;
                    }

                    table.Rows.Add(tableRow);
                }

                rowsRead++;
                this.importRowIndex++;

                if (blockSize.HasValue && y % blockSize == 0)
                {
                    yield return table;

                    // create new table with the columns and destroy the old table
                    var headerTable = table.Clone();
                    DataTableHelper.DisposeTable(table);
                    table = headerTable;
                }
            }

            yield return table;
        }

        public override bool WriteData(IEnumerable<DataTable> tables, bool deleteBefore = false)
        {
            string tableName = this.SheetName;

            ExcelWorksheet sheet = this.excelPackage.Workbook.Worksheets[tableName];

            if (sheet == null)
            {
                sheet = this.excelPackage.Workbook.Worksheets.Add(tableName);
            }

            int i = 0;
            foreach (DataTable table in tables)
            {
                this.startY = deleteBefore && i == 0 ? 0 : this.GetCount();

                bool hasCreatedHeader = false;

                if (this.startY == 0)
                {
                    // create header row
                    for (int x = 0; x < table.Columns.Count; x++)
                    {
                        string columnName = table.Columns[x].ToString();
                        sheet.Cells[1 + this.startY, 1 + this.startX + x].Value = columnName;
                    }
                    hasCreatedHeader = true;
                }

                // loops through data
                for (int y = 0; y < table.Rows.Count; y++)
                {
                    for (int x = 0; x < table.Columns.Count; x++)
                    {
                        string columnName = table.Columns[x].ToString();
                        sheet.Cells[1 + this.startY + y + (hasCreatedHeader ? 1 : 0), 1 + this.startX + x].Value = table.Rows[y][columnName].ToString();
                    }
                }

                i++;
            }

            // Save ExcelDocument
            this.excelPackage.Save();

            if (this.excelPackage.Stream != null)
            {
                this.excelPackage.Stream.Flush();
            }

            return true;
        }

        private int startY = 0;
        private int startX = 0;
    }
}