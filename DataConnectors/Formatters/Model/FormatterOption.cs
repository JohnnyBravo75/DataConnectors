using System.Xml.Serialization;

namespace DataConnectors.Formatters.Model
{
    [XmlType(TypeName = "Option")]
    public class FormatterOption
    {
        private string name = "";
        private object value = "";

        [XmlAttribute]
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public object Value
        {
            get { return this.value; }
            set { this.value = value; }
        }
    }
}