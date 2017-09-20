﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using DataConnectors.Adapter.DbAdapter;
using DataConnectors.Adapter.DbAdapter.ConnectionInfos;
using DataConnectors.Adapter.FileAdapter;
using DataConnectors.Common.Extensions;
using DataConnectors.Formatters;

namespace DataConnectors.Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //TNSNamesReader tnsNamesReader = new TNSNamesReader();
            //string oraHome = tnsNamesReader.GetOracleHomes().FirstOrDefault();

            //var fff = tnsNamesReader.LoadTNSNames(oraHome);

            Sample_ReadOracle_WriteCsv();
        }

        public static void Sample_Csv_To_Fixed()
        {
            string sampleDataPath = @"..\..\Samples\";
            var watch = new Stopwatch();

            using (var reader = new CsvAdapter())
            {
                reader.FileName = sampleDataPath + @"cd-Daten.txt";
                reader.Enclosure = "\"";
                reader.Separator = ";";

                using (var writer = new FixedTextAdapter())
                {
                    writer.FileName = Path.Combine(Path.GetDirectoryName(reader.FileName), "cd-Daten-Fixed.txt");

                    watch.Start();
                    int lineCount = 0;

                    reader.ReadDataAs<CdDaten>(30)
                          .ForEach(x =>
                          {
                              lineCount += 1;
                          })
                          .Do(x => writer.WriteDataFrom<CdDaten>(x, false, 30));

                    watch.Stop();
                    Console.WriteLine("lineCount=" + lineCount + ", Time=" + watch.Elapsed);
                    Console.ReadLine();
                }
            }
        }

        public static void Sample_ReadXml_WriteCsv_Address()
        {
            string sampleDataPath = @"..\..\Samples\";
            var watch = new Stopwatch();

            using (var reader = new XmlAdapter())
            {
                reader.FileName = sampleDataPath + @"GetAddressResponse.xml";
                reader.XPath = "/GetAddressResponse/GetAddressResult/result/address";

                using (var writer = new CsvAdapter())
                {
                    writer.FileName = sampleDataPath + @"flatxml.csv";

                    watch.Start();
                    int lineCount = 0;

                    reader.ReadData(30)
                         .ForEach(x =>
                         {
                             Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                             lineCount += x.Rows.Count;
                         })
                         .Do(x => writer.WriteData(x));

                    watch.Stop();
                    Console.WriteLine("lineCount=" + lineCount + ", Time=" + watch.Elapsed);
                    Console.ReadLine();
                }
            }
        }

        public static void Sample_ReadXml_WriteCsvs_Kunden()
        {
            string sampleDataPath = @"..\..\Samples\";
            var watch = new Stopwatch();

            using (var reader = new XmlAdapter())
            {
                reader.FileName = sampleDataPath + @"kunden.xml";
                reader.XPath = "/adre/kunde";
                reader.ReadFormatter = new XmlToDataSetFormatter();
                using (var writer = new CsvAdapter())
                {
                    writer.FileName = "";

                    watch.Start();
                    int lineCount = 0;

                    reader.ReadData(30)
                         .ForEach(x =>
                         {
                             writer.FileName = sampleDataPath + x.TableName + ".csv";
                             Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                             lineCount += x.Rows.Count;
                         })
                         .Do(x => writer.WriteData(x));

                    watch.Stop();
                    Console.WriteLine("lineCount=" + lineCount + ", Time=" + watch.Elapsed);
                    Console.ReadLine();
                }
            }
        }

        public class CdDaten
        {
            public string pk { get; set; }

            [DataMember(Name = "genre", IsRequired = true)]
            public string Genre { get; set; }

            public string Track01 { get; set; }
        }

        public static void Sample_DateFormats()
        {
            string sampleDataPath = @"..\..\Samples\";
            var watch = new Stopwatch();

            using (var reader = new CsvAdapter())
            {
                reader.FileName = sampleDataPath + @"DataFormats.txt";
                reader.Enclosure = "\"";
                reader.Separator = ";";

                using (var writer = new FixedTextAdapter())
                {
                    writer.FileName = Path.Combine(Path.GetDirectoryName(reader.FileName), "DataFormats-Fixed.txt");

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

        public static void Sample_Csv_To_Sqlite()
        {
            string sampleDataPath = @"..\..\Samples\";
            var watch = new Stopwatch();

            using (var reader = new CsvAdapter())
            {
                reader.FileName = sampleDataPath + @"cd-Daten.txt";
                reader.Enclosure = "\"";
                reader.Separator = ";";

                using (var writer = new SqliteAdapter())
                {
                    writer.FileName = sampleDataPath + @"cd-Daten.sqlite";
                    writer.CreateNewFile();

                    if (!writer.Connect())
                    {
                        throw new Exception("No connection");
                    }

                    watch.Start();
                    int lineCount = 0;

                    reader.ReadDataAs<CdDaten>(30)
                          .ForEach(x =>
                          {
                              lineCount += 1;
                          })
                          .Do(x => writer.WriteDataFrom<CdDaten>(x, false, 30));

                    writer.Disconnect();

                    watch.Stop();
                    Console.WriteLine("lineCount=" + lineCount + ", Time=" + watch.Elapsed);
                    Console.ReadLine();
                }
            }
        }

        public static void Sample_ReadXml_WriteSqlite_Address()
        {
            string sampleDataPath = @"..\..\Samples\";
            var watch = new Stopwatch();

            using (var reader = new XmlAdapter())
            {
                reader.FileName = sampleDataPath + @"GetAddressResponse.xml";
                reader.XPath = "/GetAddressResponse/GetAddressResult/result/address";

                using (var writer = new SqliteAdapter())
                {
                    writer.FileName = sampleDataPath + @"flatxml.sqlite";
                    writer.CreateNewFile();

                    if (!writer.Connect())
                    {
                        throw new Exception("No connection");
                    }

                    watch.Start();
                    int lineCount = 0;

                    reader.ReadData(30)
                         .ForEach(x =>
                         {
                             Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                             lineCount += x.Rows.Count;
                         })
                         .Do(x => writer.WriteData(x));

                    writer.Disconnect();

                    watch.Stop();
                    Console.WriteLine("lineCount=" + lineCount + ", Time=" + watch.Elapsed);
                    Console.ReadLine();
                }
            }
        }

        public static void Sample_ReadOracle_WriteCsv()
        {
            string sampleDataPath = @"..\..\Samples\";
            var watch = new Stopwatch();

            using (var reader = new DbAdapter())
            {
                reader.ConnectionInfo = new OracleNativeDbConnectionInfo()
                {
                    Database = "TESTDB01",
                    UserName = "USER01",
                    Password = "***",
                    Host = "COMPUTER01"
                };
                reader.TableName = "TB_DATA";
                reader.Connect();

                using (var writer = new CsvAdapter())
                {
                    writer.FileName = Path.Combine(sampleDataPath, "TB_DATA.csv");

                    watch.Start();
                    int lineCount = 0;

                    reader.ReadData(30)
                          .ForEach(x =>
                          {
                              Console.WriteLine("Tablename=" + x.TableName + ", Count=" + x.Rows.Count);
                              lineCount += x.Rows.Count;
                          })
                          .Do(x => writer.WriteData(x, false));

                    watch.Stop();
                    Console.WriteLine("lineCount=" + lineCount + ", Time=" + watch.Elapsed);
                    Console.ReadLine();
                }

                reader.Disconnect();
            }
        }
    }
}