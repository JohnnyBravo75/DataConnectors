using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                    Console.ReadLine();

                    // check
                    var targetlineCount = File.ReadLines(this.resultPath + @"DataFormatTest-Fixed.txt").Count();

                    Assert.AreEqual(4, targetlineCount);
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
    }
}