using System.Collections.Generic;

namespace DataConnectors.Formatters.Model
{
    public interface IHasXmlNameSpaces
    {
        List<XmlNameSpace> XmlNameSpaces { get; set; }
    }
}