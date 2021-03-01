using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Facturae
{
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
    }

    public static class Utils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <param name="omitXmlDeclaration"></param>
        /// <returns></returns>
        public static XmlDocument SerializeToXmlDocument(object input, Type type, bool omitXmlDeclaration)
        {
            return SerializeToXmlDocument(input, type, omitXmlDeclaration, new XmlSerializerNamespaces());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <param name="omitXmlDeclaration"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        public static XmlDocument SerializeToXmlDocument(object input, Type type, bool omitXmlDeclaration, XmlSerializerNamespaces ns)
        {
            XmlSerializer ser = new XmlSerializer(type);

            XmlDocument xd = null;

            using (MemoryStream memStm = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = omitXmlDeclaration
                };

                XmlWriter writer = XmlWriter.Create(memStm, settings);

                ser.Serialize(writer, input, ns);
                memStm.Position = 0;

                XmlReaderSettings readersettings = new XmlReaderSettings();
                readersettings.IgnoreWhitespace = true;

                using var xtr = XmlReader.Create(memStm, readersettings);
                xd = new XmlDocument();
                xd.Load(xtr);
            }

            return xd;
        }


        /// <summary>
        /// Serialize class to string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Serialize<T>(this T value, bool omitXmlDeclaration = false)
        {

            if (value == null)
            {
                return string.Empty;
            }
            var serializer = new XmlSerializer(typeof(T));

            using var stringWriter = new Utf8StringWriter();
            var settings = new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = omitXmlDeclaration,
                Encoding = new UTF8Encoding(false)
            };

            using var writer = XmlWriter.Create(stringWriter, settings);
            serializer.Serialize(writer, value);
            return stringWriter.ToString();            
        }

        /// <summary>
        /// Serialize class to byte array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] SerializeToByteArrayUTF8<T>(this T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Parameter can not be null");
            }

            byte[] bytes;

            var serializer = new XmlSerializer(typeof(T));

            MemoryStream memoryStream = new MemoryStream();
            using (var xmlTextWriterSerialize = new XmlTextWriter(memoryStream, Encoding.UTF8))
            {
                serializer.Serialize(xmlTextWriterSerialize, value);
                memoryStream = (MemoryStream)xmlTextWriterSerialize.BaseStream;

                bytes = memoryStream.ToArray();
            }

            memoryStream.Close();
            memoryStream.Dispose();


            return bytes;

        }

        /// <summary>
        /// Serialize class to byte array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] SerializeToByteArray<T>(this T value, bool omitXmlDeclaration = false)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Parameter can not be null");
            }


            var serializer = new XmlSerializer(typeof(T));

            using var memoryStream = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = omitXmlDeclaration,
                Encoding = new UTF8Encoding()
            };

            using var writer = XmlWriter.Create(memoryStream, settings);
            serializer.Serialize(writer, value);
            return memoryStream.ToArray();

        }


        /// <summary>
        /// Deserialize xml string to object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static T DeserializeXML<T>(this string xmlString)
        {
            T returnValue = default;

            var serializer = new XmlSerializer(typeof(T));

            using (var reader = new StringReader(xmlString))
            {
                object result = serializer.Deserialize(reader);

                if (result is T t)
                {
                    returnValue = t;
                }

                reader.Close();
            }

            return returnValue;
        }

        /// <summary>
        /// Returns byte array from string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] GetBytesUTF8<T>(this T data)
        {
            byte[] utf8EncodedXml;
            var serializer = new XmlSerializer(typeof(T));
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamWriter streamWriter = new StreamWriter(memoryStream, System.Text.Encoding.UTF8))
            {
                serializer.Serialize(streamWriter, data);
                utf8EncodedXml = memoryStream.ToArray();
            }

            return utf8EncodedXml;
        }

        /// <summary>
        /// Returns byte array from string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] GetBytes(this string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Get string from byte array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string GetString(this byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}
