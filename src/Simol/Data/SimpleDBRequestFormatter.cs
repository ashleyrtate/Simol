/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Amazon.SimpleDB.Model;

namespace Simol.Data
{
    /// <summary>
    /// Formatter which encodes/decodes SimpleDB request objects for storage in
    /// the SimolSystem table.
    /// </summary>
    internal class SimpleDBRequestFormatter : ITypeFormatter
    {
        public string ToString(object value)
        {
            var serializer = new XmlSerializer(value.GetType());
            var writer = new StringWriter();

            serializer.Serialize(writer, value);

            return writer.ToString();
        }

        public object ToType(string valueString, Type expected)
        {
            var sr = new StringReader(valueString);
            var settings = new XmlReaderSettings
                {
                    // must disable Xml character checking to allow null characters (x00) we use to mark null values in SimpleDb
                    CheckCharacters = false
                };
            XmlReader reader = XmlReader.Create(sr, settings);
            do
            {
                reader.Read();
            } while (reader.NodeType != XmlNodeType.Element);
            string requestTypeStr = reader.LocalName;

            Type requestType = GetRequestType(requestTypeStr);
            var serializer = new XmlSerializer(requestType);

            return serializer.Deserialize(reader);
        }

        public static Type GetRequestType(string typeName)
        {
            switch (typeName)
            {
                case "PutAttributesRequest":
                    return typeof (PutAttributesRequest);
                case "BatchPutAttributesRequest":
                    return typeof (BatchPutAttributesRequest);
                case "DeleteAttributesRequest":
                    return typeof (DeleteAttributesRequest);
                case "BatchDeleteAttributesRequest":
                    return typeof(BatchDeleteAttributesRequest);
                default:
                    string message = string.Format("Request type '{0}' is not supported", typeName);
                    throw new InvalidOperationException(message);
            }
        }
    }
}