namespace DataConnectors.Common.Helper
{
    using System.IO;
    using System.Text;

    public static class StreamUtil
    {
        public static Stream CreateStream(string str, Encoding encoding = null)
        {
            var stream = new MemoryStream();

            var writer = encoding != null
                                ? new StreamWriter(stream, encoding)
                                : new StreamWriter(stream);

            writer.Write(str);
            writer.Flush();

            stream.Position = 0;
            return stream;
        }
    }
}