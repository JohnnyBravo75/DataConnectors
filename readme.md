# DataConnectors

* easy reading an writing data 
* supported formats: Csv, Fixed, Sql, Xml, Excel, Access, ADO (Oracle SqlServer, PostGre,...) 
* supports streaming an pipelining 

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
    public string pk { get; set; }

    [DataMember(Name = "genre", IsRequired = true)]
    public string Genre { get; set; }

    public string Track01 { get; set; }
}
```

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


