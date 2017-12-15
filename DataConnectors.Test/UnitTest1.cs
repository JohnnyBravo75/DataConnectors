using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using DataConnectors.Adapter;
using DataConnectors.Adapter.DbAdapter;
using DataConnectors.Adapter.FileAdapter;
using DataConnectors.Common.Extensions;
using DataConnectors.Common.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataConnectors.Test
{
    [TestClass]
    public class UnitTest1
    {
        private string testDataPath = @"..\..\TestData\";

        private string resultPath = Environment.ExpandEnvironmentVariables(@"%TEMP%\TestResultData\");

        [TestInitialize]
        public void TestInitialize()
        {
            DirectoryUtil.CreateDirectoryIfNotExists(this.resultPath);
            DirectoryUtil.ClearDirectory(this.resultPath);
        }

        [TestMethod]
        public void Test_Csv_To_Fixed()
        {
            using (var reader = new CsvAdapter())
            {
                reader.FileName = Path.Combine(this.testDataPath, @"cd-Daten.txt");
                reader.Enclosure = "\"";
                reader.Separator = ";";

                using (var writer = new FixedTextAdapter())
                {
                    writer.FileName = Path.Combine(this.resultPath, "cd-Daten-Fixed.txt");

                    int lineCount = 0;

                    reader.ReadData(30)
                          .ForEach(x =>
                          {
                              Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                              lineCount += x.Rows.Count;
                          })
                          .Do(x => writer.WriteData(x));
                }
            }

            // check
            var targetlineCount = File.ReadLines(this.resultPath + @"cd-Daten-Fixed.txt").Count();

            Assert.AreEqual(97, targetlineCount);
        }

        [TestMethod]
        public void Test_ReadXml_WriteCsv_Address()
        {
            using (var reader = new XmlAdapter())
            {
                reader.FileName = Path.Combine(this.testDataPath, @"GetAddressResponse.xml");
                reader.XPath = "/GetAddressResponse/GetAddressResult/result/address";

                using (var writer = new CsvAdapter())
                {
                    writer.FileName = Path.Combine(this.resultPath, @"flatxml.csv");

                    int lineCount = 0;

                    reader.ReadData(30)
                         .ForEach(x =>
                         {
                             Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                             lineCount += x.Rows.Count;
                         })
                         .Do(x => writer.WriteData(x));
                }
            }

            // check
            var targetlineCount = File.ReadLines(this.resultPath + @"flatxml.csv").Count();

            Assert.AreEqual(3, targetlineCount);
        }

        [TestMethod]
        public void Test_Formats_To_Obj_Conversion()
        {
            var watch = new Stopwatch();

            using (var reader = new CsvAdapter())
            {
                reader.FileName = Path.Combine(this.testDataPath, @"DataFormats.txt");
                reader.Enclosure = "\"";
                reader.Separator = ";";

                using (var writer = new FixedTextAdapter())
                {
                    writer.FileName = Path.Combine(this.resultPath, @"DataFormats-Fixed.txt");

                    watch.Start();
                    int lineCount = 0;

                    reader.ReadDataAs<DataFormatTest>(30)
                          .ForEach(x =>
                          {
                              Console.WriteLine(x.ToString());
                              lineCount += 1;
                          })
                          .Do(x => writer.WriteDataFrom<DataFormatTest>(x, false, 30));

                    watch.Stop();
                    Console.WriteLine("lineCount=" + lineCount + ", Time=" + watch.Elapsed);

                    // check
                    var targetlineCount = File.ReadLines(this.resultPath + @"DataFormats-Fixed.txt").Count();

                    Assert.AreEqual(6, targetlineCount);
                }
            }
        }

        public class DataFormatTest
        {
            public bool? BoolColumn { get; set; }

            public int? NumberColumn { get; set; }

            public float? FloatColumn { get; set; }

            public DateTime? DateColumn { get; set; }

            public DateTime? DatetimeColumn { get; set; }

            public string StringColumn { get; set; }

            public override string ToString()
            {
                return this.ToPropertyString();
            }
        }

        public void Save(string fileName, DataAdapterBase dataAdapterBase)
        {
            var serializer = new XmlSerializerHelper<DataAdapterBase>();
            serializer.FileName = fileName;
            serializer.Save(dataAdapterBase);
        }

        public DataAdapterBase Load(string fileName)
        {
            var serializer = new XmlSerializerHelper<DataAdapterBase>();
            serializer.FileName = fileName;
            var dataAdapterBase = serializer.Load();

            return dataAdapterBase;
        }

        [TestMethod]
        public void Test_Serialize_LoadSave()
        {
            var adapters = new List<DataAdapterBase>();
            adapters.Add(new CsvAdapter());
            adapters.Add(new FixedTextAdapter());
            adapters.Add(new XmlAdapter());
            adapters.Add(new ExcelNativeAdapter());
            adapters.Add(new Excel2007NativeAdapter());
            adapters.Add(new AccessAdapter());

            int i = 0;
            foreach (var adapter in adapters)
            {
                var fileName = string.Format("C:\\temp\\{0}_{1}.xml", i, adapter.GetType().Name);
                this.Save(fileName, adapter);
                var loadedAdapter = this.Load(fileName);
                i++;
            }
        }

        [TestMethod]
        public void Test_Csv_Excel_Csv()
        {
            using (var csvReader = new CsvAdapter())
            using (var excelWriter = new ExcelNativeAdapter())
            using (var excelReader = new ExcelNativeAdapter())
            using (var csvWriter = new CsvAdapter())
            {
                csvReader.Enclosure = "\"";
                csvReader.FileName = this.testDataPath + @"cd-Daten.txt";
                var csvData = csvReader.ReadAllData();

                excelWriter.FileName = Path.Combine(this.resultPath, "cd-Daten.xls");
                excelWriter.SheetName = "Tabelle1";
                excelWriter.CreateNewFile();
                excelWriter.Connect();
                excelWriter.WriteAllData(csvData);
                excelWriter.Disconnect();

                excelReader.FileName = excelWriter.FileName;
                excelReader.SheetName = excelWriter.SheetName;
                excelReader.Connect();
                var accessData = excelReader.ReadAllData();

                csvWriter.Encoding = csvReader.Encoding;
                csvWriter.Enclosure = csvReader.Enclosure;
                csvWriter.FileName = this.resultPath + @"cd-Daten_ExcelRoundtrip.csv";
                csvWriter.WriteAllData(accessData);

                if (!FileUtil.CompareFiles(csvReader.FileName, csvWriter.FileName))
                {
                    throw new Exception("Original and copied file do not match");
                }
            }
        }

        [TestMethod]
        public void Test_Csv_Access_Csv()
        {
            DirectoryUtil.ClearDirectory(this.resultPath);

            using (var csvReader = new CsvAdapter())
            using (var accessWriter = new AccessAdapter())
            using (var accessReader = new AccessAdapter())
            using (var csvWriter = new CsvAdapter())
            {
                csvReader.Enclosure = "";
                csvReader.FileName = this.testDataPath + @"StringData.csv";
                csvReader.AutoDetectEncoding();
                var csvData = csvReader.ReadAllData();

                accessWriter.FileName = Path.Combine(this.resultPath, "cd-Daten.mdb");
                accessWriter.TableName = "Tabelle1";
                accessWriter.CreateNewFile();
                accessWriter.Connect();
                accessWriter.WriteAllData(csvData);
                accessWriter.Disconnect();

                accessReader.FileName = accessWriter.FileName;
                accessReader.TableName = accessWriter.TableName;
                accessReader.Connect();
                var accessData = accessReader.ReadAllData();
                accessReader.Disconnect();

                csvWriter.Encoding = csvReader.Encoding;
                csvWriter.Enclosure = csvReader.Enclosure;
                csvWriter.FileName = this.resultPath + @"cd-Daten_AccessRoundtrip.csv";
                csvWriter.WriteAllData(accessData);

                if (!FileUtil.CompareFiles(csvReader.FileName, csvWriter.FileName))
                {
                    throw new Exception("Original and copied file do not match");
                }
            }
        }

        [TestMethod]
        public void Test_CreateAdapterDynamic()
        {
            var csvAdapter = GenericFactory.GetInstance<DataAdapterBase>("CsvAdapter");

            Assert.IsInstanceOfType(csvAdapter, typeof(CsvAdapter));
        }

        [TestMethod]
        public void Test_Rss_Focus()
        {
            DataTable rssFeeds = null;
            var request = WebRequest.Create("http://rss.focus.de/fol/XML/rss_folnews.xml") as HttpWebRequest;

            if (request != null)
            {
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        using (var reader = new XmlAdapter(responseStream))
                        {
                            reader.XPath = "/rss/channel/item";

                            rssFeeds = reader.ReadAllData();
                        }
                    }
                }
            }

            Assert.IsNotNull(rssFeeds);
            Assert.IsTrue(rssFeeds.Rows.Count > 0);
        }

        [TestMethod]
        public void Test_ReadXml_WriteSqlite_Address()
        {
            using (var reader = new XmlAdapter())
            {
                reader.FileName = this.testDataPath + @"GetAddressResponse.xml";
                reader.XPath = "/GetAddressResponse/GetAddressResult/result/address";

                using (var writer = new SqliteAdapter())
                {
                    writer.FileName = this.resultPath + @"flatxml.sqlite";
                    writer.CreateNewFile();

                    if (!writer.Connect())
                    {
                        throw new Exception("No connection");
                    }

                    int lineCount = 0;

                    reader.ReadData(30)
                         .ForEach(x =>
                         {
                             Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                             lineCount += x.Rows.Count;
                         })
                         .Do(x => writer.WriteData(x));

                    writer.Disconnect();

                    Assert.IsTrue(File.Exists(writer.FileName));
                    Assert.AreEqual(2, lineCount);
                }
            }
        }
    }
}