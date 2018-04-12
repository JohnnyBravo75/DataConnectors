# DataConnectors

* easy reading an writing data 
* supported formats: Csv, Fixed, Sql, Xml, Excel, Access, ADO (Oracle SqlServer, PostGre,...) 
* supports streaming an pipelining 


## Install

To install DataConnectors use the  Nuget package manager console

    PM> Install-Package DataConnectors.Nuget
+ [DataConnectors.Nuget](https://www.nuget.org/packages/DataConnectors.Nuget/)   

or download release binary
+ [DataConnectors/releases](https://github.com/JohnnyBravo75/DataConnectors/releases)

## Samples

### CSV

Read CSV into DataTable
```csharp
using (var reader = new CsvAdapter())
{
    reader.FileName = @"C.\Temp\cd-Daten.txt";
    reader.Enclosure = "\"";
    reader.Separator = ";";

    var dataTable = reader.ReadAllData();
}
```


Read CSV into a model class and write Fixed

```csharp
public class CdData
{
    [DataField(Name = "pk", IsRequired = true)]
    public string pk { get; set; }

    [DataField(Name = "genre", IsRequired = true)]
    public string Genre { get; set; }

    [DataField(Name = "Track01", IsRequired = true)]
    public string Track01 { get; set; }
}
```

Read CSV and write to Fixed text, stream 30 rows each time
```csharp
using (var reader = new CsvAdapter())
{
    reader.FileName = @"C:\Temp\CdData.txt";
    reader.Enclosure = "\"";
    reader.Separator = ";";

    using (var writer = new FixedTextAdapter())
    {
        writer.FileName = Path.Combine(Path.GetDirectoryName(reader.FileName), "CdData-Fixed.txt");

        int lineCount = 0;

        reader.ReadDataAs<CdData>(30)
              .ForEach(x =>
              {
                  lineCount += 1;
              })
              .Do(x => writer.WriteDataFrom<CdData>(x, false, 30));
    }
}
```

Read CSV from a string
```csharp
string data = @"Name;Address;Gpnr
John;Main Road; 4711
Jeffrey;;4712
Mike;Hauptstr.1;4713";

using (Stream stream = StreamUtil.CreateStream(data))
{
    using (var reader = new CsvAdapter(stream))
    {
        reader.Separator = ";";
        var dataTable = reader.ReadAllData();
    }
}
```

### XML

Read from string with default namespace

```csharp
string xml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<bookstore xmlns=""http://www.contoso.com/books"">
    <book genre=""autobiography"" publicationdate=""1981-03-22"" ISBN=""1-861003-11-0"">
        <title>The Autobiography of Benjamin Franklin</title>
        <author>
            <first-name>Benjamin</first-name>
            <last-name>Franklin</last-name>
        </author>
        <price>8.99</price>
    </book>
    <book genre=""novel"" publicationdate=""1967-11-17"" ISBN=""0-201-63361-2"">
        <title>The Confidence Man</title>
        <author>
            <first-name>Herman</first-name>
            <last-name>Melville</last-name>
        </author>
        <price>11.99</price>
    </book>
    <book genre=""philosophy"" publicationdate=""1991-02-15"" ISBN=""1-861001-57-6"">
        <title>The Gorgias</title>
        <author>
            <name>Plato</name>
        </author>
        <price>9.99</price>
    </book>
</bookstore> ";

var xmlDoc = new XmlDocument();
xmlDoc.LoadXml(xml);

using (var reader = new XmlAdapter(xmlDoc))
{
    reader.XPath = "/bookstore/book";
    reader.AutoExtractNamespaces = true;
    var dataTable = reader.ReadAllData();
}
```

Reading into a model object/POCO
```csharp
public class Book
{
    [DataField(XPath = "/book/title")]
    public string Title { get; set; }

    [DataField(XPath = "/book/@genre")]
    public string Genre { get; set; }

    [DataField(XPath = "/book/@ISBN")]
    public string ISBN { get; set; }

    [DataField(XPath = "/book/@publicationdate")]
    public DateTime PublicationDate { get; set; }

    [DataField(XPath = "/book/author/first-name")]
    public string FirstName { get; set; }

    [DataField(XPath = "/book/author/last-name")]
    public string LastName { get; set; }

    [DataField(XPath = "/book/price")]
    public float Price { get; set; }
}

var xmlDoc = new XmlDocument();
xmlDoc.LoadXml(xml);

using (var reader = new XmlAdapter(xmlDoc))
{
	reader.XPath = "/bookstore/book";
	reader.AutoExtractNamespaces = true;

	var books = reader.ReadAllDataAs<Book>();
}

```

Read RSS feed from the web

```csharp
var request = HttpWebRequest.Create("http://rss.focus.de/fol/XML/rss_folnews.xml") as HttpWebRequest;

if (request != null)
{
    using (var response = request.GetResponse() as HttpWebResponse)
    {
        using (var responseStream = response.GetResponseStream())
        {
            using (var reader = new XmlAdapter(responseStream))
            {
                reader.XPath = "/rss/channel/item";

                foreach (var table in reader.ReadData(30))
                {
                    foreach (DataRow row in table.Rows)
                    {
                        Console.WriteLine(row.ToDictionary<object>().ToFormattedString());
                    }
                }
            }
        }
    }
}
```

## License

[MIT](https://opensource.org/licenses/MIT)

 DataConnectors source and binaries are **free for commercial and non commercial use**.

### Sponsored by

[Jetbrains Resharper](http://www.jetbrains.com/resharper/)

