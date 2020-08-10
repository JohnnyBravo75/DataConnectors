using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using DataConnectors.Adapter;
using DataConnectors.Adapter.DbAdapter;
using DataConnectors.Adapter.DbAdapter.ConnectionInfos;
using DataConnectors.Adapter.FileAdapter;
using DataConnectors.Common.Extensions;
using DataConnectors.Common.Helper;
using DataConnectors.Common.Model;
using DataConnectors.Converters;
using DataConnectors.Converters.Model;
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

                    reader.ReadData(1000)
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

                    reader.ReadData(1000)
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
            [ValueConverter(typeof(StringToBooleanAutoConverter))]
            public bool? BoolColumn { get; set; }

            [ValueConverter(typeof(StringToNumberAutoConverter))]
            public int? NumberColumn { get; set; }

            [ValueConverter(typeof(StringToNumberAutoConverter))]
            public float? FloatColumn { get; set; }

            [ValueConverter(typeof(StringToDateTimeAutoConverter))]
            public DateTime? DateColumn { get; set; }

            [ValueConverter(typeof(StringToDateTimeAutoConverter))]
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
            serializer.Save(fileName, dataAdapterBase);
        }

        public DataAdapterBase Load(string fileName)
        {
            var serializer = new XmlSerializerHelper<DataAdapterBase>();
            var dataAdapterBase = serializer.Load(fileName);

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
            adapters.Add(new DbAdapter());

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

                    reader.ReadData(1000)
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

        [TestMethod]
        public void Test_WriteRead_Binary()
        {
            var originalBuffer = new byte[] { 12, 43, 76, 98, 09, 88, 255, 0 };
            byte[] readedBuffer = null;

            using (var writer = new FlatFileAdapter())
            {
                using (var reader = new FlatFileAdapter())
                {
                    writer.FileName = this.resultPath + @"binary.bin";
                    writer.WriteBinaryData(originalBuffer);

                    reader.FileName = writer.FileName;
                    readedBuffer = reader.ReadBinaryData();
                }
            }

            Assert.IsTrue(originalBuffer.SequenceEqual(readedBuffer));
        }

        [TestMethod]
        public void Test_ReadOracle_WriteCsv()
        {
            using (var reader = new DbAdapter())
            {
                reader.ConnectionInfo = new OracleNativeDbConnectionInfo()
                {
                    Database = "***",
                    UserName = "***",
                    Password = "***",
                    Host = "***"
                };
                reader.TableName = "TB_ACTION";

                reader.Connect();

                using (var writer = new CsvAdapter())
                {
                    writer.Encoding = Encoding.UTF8;
                    writer.Separator = ";";
                    writer.Enclosure = "\"";
                    writer.FileName = Path.Combine(this.testDataPath, "TB_ACTION.csv");

                    int lineCount = 0;

                    reader.ReadData(100)
                          .ForEach(x =>
                          {
                              Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                              lineCount += x.Rows.Count;
                          })
                          .Do(x => writer.WriteData(x, false));
                }

                reader.Disconnect();
            }
        }

        [TestMethod]
        public void Test_ReadCsv_WriteSqlite_Art()
        {
            using (var reader = new CsvAdapter())
            {
                reader.FileName = this.testDataPath + @"Master Works of Art.csv";
                reader.Separator = ",";
                reader.Enclosure = "\"";
                reader.TableName = "MasterWorksofArt";
                reader.FieldDefinitions.Add(new FieldDefinition("Artist", "Artist", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("Title", "Title", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("Year (Approximate)", "Year_Approximate", typeof(float), new StringToNumberAutoConverter()));
                reader.FieldDefinitions.Add(new FieldDefinition("Movement", "Movement", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("Total Height (cm)", "Total_Height_cm", typeof(float), new StringToNumberAutoConverter()));
                reader.TableName = "Arts";

                using (var writer = new SqliteAdapter())
                {
                    writer.FileName = this.resultPath + @"Master Works of Art.sqlite";
                    writer.CreateNewFile();

                    if (!writer.Connect())
                    {
                        throw new Exception("No connection");
                    }

                    int lineCount = 0;

                    reader.ReadData(1000)
                         .ForEach(x =>
                         {
                             Debug.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                             lineCount += x.Rows.Count;
                         })
                         .Do(x => writer.WriteData(x));

                    writer.Disconnect();

                    Assert.IsTrue(File.Exists(writer.FileName));
                    Assert.AreEqual(200, lineCount);
                }
            }
        }

        [TestMethod]
        public void Test_ReadCsv_WriteSqlite_Geo()
        {
            using (var reader = new CsvAdapter())
            {
                reader.FileName = @"C:\Temp\AllCountries.txt";
                reader.Separator = "\t";
                reader.Enclosure = "";
                reader.TableName = "AllCountries";
                reader.HasHeader = false;
                reader.FieldDefinitions.Add(new FieldDefinition("", "CtryCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "ZipCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "City", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "State", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "StateCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "Region", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "RegionCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "County", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "CountyCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "Lat", typeof(float), new StringToNumberAutoConverter()));
                reader.FieldDefinitions.Add(new FieldDefinition("", "Lon", typeof(float), new StringToNumberAutoConverter()));
                reader.FieldDefinitions.Add(new FieldDefinition("", "Code1", typeof(string)));

                reader.Encoding = Encoding.UTF8;

                using (var writer = new SqliteAdapter())
                {
                    writer.FileName = @"C:\Temp\AllCountries_Test.sqlite";
                    writer.CreateNewFile();
                    writer.UseTransaction = true;

                    if (!writer.Connect())
                    {
                        throw new Exception("No connection");
                    }

                    int lineCount = 0;

                    reader.ReadData(1000)
                         .ForEach(x =>
                        {
                            Debug.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                            lineCount += x.Rows.Count;
                        })
                         .Do(x => writer.WriteData(x));

                    writer.Disconnect();

                    Assert.IsTrue(File.Exists(writer.FileName));
                }
            }
        }

        [TestMethod]
        public void Test_ReadCsv_WriteCsv_Geo()
        {
            using (var reader = new CsvAdapter())
            {
                reader.FileName = @"C:\Temp\AllCountries.txt";
                reader.Separator = "\t";
                reader.Enclosure = "";
                reader.TableName = "AllCountries";
                reader.HasHeader = false;
                reader.FieldDefinitions.Add(new FieldDefinition("", "CtryCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "ZipCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "City", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "State", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "StateCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "Region", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "RegionCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "County", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "CountyCode", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "Lat", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "Lon", typeof(string)));
                reader.FieldDefinitions.Add(new FieldDefinition("", "Code1", typeof(string)));

                reader.Encoding = Encoding.UTF8;

                using (var writer = new CsvAdapter())
                {
                    writer.FileName = @"C:\Temp\AllCountries_Test.csv";
                    writer.Encoding = Encoding.UTF8;
                    writer.Enclosure = "\"";

                    int lineCount = 0;

                    reader.ReadData(1000)
                         .ForEach(x =>
                         {
                             Debug.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                             lineCount += x.Rows.Count;
                         })
                         .Do(x => writer.WriteData(x));

                    Assert.IsTrue(File.Exists(writer.FileName));
                }
            }
        }

        [TestMethod]
        public void Test_ReadCsv_WriteOracle()
        {
            using (var writer = new DbAdapter())
            {
                writer.ConnectionInfo = new OracleNativeDbConnectionInfo()
                {
                    Database = "***",
                    UserName = "***",
                    Password = "***",
                    Host = "***"
                };
                writer.TableName = "TB_TEST2";

                writer.Connect();

                using (var reader = new CsvAdapter())
                {
                    reader.Encoding = Encoding.UTF8;
                    reader.Separator = ",";
                    reader.Enclosure = "";
                    reader.FileName = @"H:\JEDER\SSc\CsvAnalyser\1500000 Sales Records.csv";
                    reader.CleanColumnName = true;

                    int lineCount = 0;

                    reader.BadDataHandler = (badData) =>
                   {
                   };
                    reader.ReadData(10000)
                          .ForEach(x =>
                          {
                              Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                              lineCount += x.Rows.Count;
                          })
                          .Do(x => writer.WriteData(x, false));
                }

                writer.Disconnect();
            }
        }

        [TestMethod]
        public void Test_ReadXml_WriteOracle_Contract()
        {
            using (var reader = new XmlAdapter())
            {
                reader.FileName = Path.Combine(@"D:\develop\git\Kunden\DAEV\dialogcrm6-webservices-adsuite\AdSuiteModule.Test\Unit\TestFiles\Contracts\Abschlüsse_contr_265500.xml");
                reader.XPath = "/Contracts";

                using (var writer = new DbAdapter())
                {
                    writer.ConnectionInfo = new OracleNativeDbConnectionInfo()
                    {
                        Database = "***",
                        UserName = "***",
                        Password = "***",
                        Host = "***"
                    };
                    writer.TableName = "TB_CONTRACTS";

                    writer.Connect();

                    int lineCount = 0;

                    reader.ReadData(1000)
                         .ForEach(x =>
                         {
                             Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                             lineCount += x.Rows.Count;
                         })
                         .Do(x => writer.WriteData(x));

                    writer.Disconnect();
                }
            }
        }
    }
}
