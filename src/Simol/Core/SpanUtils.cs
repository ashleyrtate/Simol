/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Coditate.Common.Util;
using System.IO;
using System.IO.Compression;
using Coditate.Common.IO;

namespace Simol.Core
{
    /// <summary>
    /// Supports operations related to spanning multiple SimpleDb attributes with a single property value.
    /// </summary>
    internal class SpanUtils
    {
        public const int ChunkIndexLength = 3;
        public const int MaxChunks = 1000;
        public const int MinMaxAttributeLength = ChunkIndexLength + 1;

        public SpanUtils(SimolConfig config)
        {
            Config = config;
        }

        public SimolConfig Config { get; private set; }

        /// <summary>
        /// Joins into a single string a list of attribute values that have been split for storage in multiple SimpleDB attributes.
        /// </summary>
        public string JoinAttributeValues(List<string> valueStrings, SpanType span)
        {
            Arg.CheckInRange("valueStrings", valueStrings.Count, 1, MaxChunks);
            if (Config.MaxAttributeLength < MinMaxAttributeLength)
            {
                throw new SimolConfigurationException("SimolConfig.MaxAttributeLength may not be set to less than " +
                                                       MinMaxAttributeLength);
            }
            if (valueStrings.Where(s => s.Length < ChunkIndexLength).Any())
            {
                throw new SimolDataException(
                    "Unable to reassemble spanned attributes property. The data may have been corrupted.");
            }

            var buffer = new StringBuilder(valueStrings.Count * Config.MaxAttributeLength);
            var chunks =
                valueStrings.Select(
                    s => new { Index = s.Substring(0, ChunkIndexLength), Content = s.Substring(ChunkIndexLength) }).
                    OrderBy(
                    s => s.Index).ToList();
            foreach (var chunk in chunks)
            {
                buffer.Append(chunk.Content);
            }

            string value = buffer.ToString();

            if (span > SpanType.Span)
            {
                byte[] valueBytes = Convert.FromBase64String(value);

                if ((span & SpanType.Encrypt) == SpanType.Encrypt)
                {
                    CheckEncryptor();
                    valueBytes = Config.Encryptor.Decrypt(valueBytes);
                }

                if ((span & SpanType.Compress) == SpanType.Compress)
                {
                    var ms = new MemoryStream(valueBytes);
                    var zipStream = new GZipStream(ms, CompressionMode.Decompress, false);

                    var ms2 = new MemoryStream((int)(valueBytes.Length * 3));
                    IOUtils.TransferData(zipStream, ms2);
                    valueBytes = ms2.ToArray();
                }
                value = Encoding.UTF8.GetString(valueBytes);
            }

            return value;
        }

        /// <summary>
        /// Splits a property value into a list of strings for storage in multiple SimpleDB attributes.
        /// </summary>
        public List<string> SplitPropertyValue(string value, SpanType span)
        {
            Arg.CheckNull("value", value);

            if (Config.MaxAttributeLength < MinMaxAttributeLength)
            {
                throw new SimolConfigurationException("SimolConfig.MaxAttributeLength may not be set to less than " +
                                                       MinMaxAttributeLength);
            }

            if (span > SpanType.Span)
            {
                byte[] valueBytes = Encoding.UTF8.GetBytes(value);

                if ((span & SpanType.Compress) == SpanType.Compress)
                {
                    var ms = new MemoryStream((int)(valueBytes.Length * 1.1));
                    var zipStream = new GZipStream(ms, CompressionMode.Compress, false);
                    zipStream.Write(valueBytes, 0, valueBytes.Length);
                    zipStream.Flush();
                    zipStream.Close();

                    valueBytes = ms.ToArray();
                }
                if ((span & SpanType.Encrypt) == SpanType.Encrypt)
                {
                    CheckEncryptor();
                    valueBytes = Config.Encryptor.Encrypt(valueBytes);
                }
                value = Convert.ToBase64String(valueBytes);
            }

            var chunks = new List<string>();
            int maxChunkLength = Config.MaxAttributeLength - ChunkIndexLength;
            int index = 0;
            do
            {
                int nextChunkLength = maxChunkLength;
                if ((span & SpanType.Compress) != SpanType.Compress)
                {
                    // compressed data is base64 encoded so 1 char always equals 1 byte when UTF-8 encoded by SimpleDB
                    nextChunkLength = CalculateNextChunkLength(value, index, maxChunkLength);
                }
                nextChunkLength = Math.Min(nextChunkLength, value.Length - index);

                string chunk = chunks.Count.ToString("000") + value.Substring(index, nextChunkLength);
                chunks.Add(chunk);
                index += nextChunkLength;
            } while (index < value.Length);

            if (chunks.Count > MaxChunks)
            {
                string message =
                    string.Format(
                        "Spanned property overflow. String property with length of {0} characters requires more than {1} attributes to store.",
                        value.Length, MaxChunks);
                throw new SimolDataException(message);
            }

            return chunks;
        }

        /// <summary>
        /// Calculates the number of characters of a string that can fit into 
        /// the specified number of bytes after being UTF-8 encoded.
        /// </summary>
        private int CalculateNextChunkLength(string source, int startIndex, int maxBytes)
        {
            Encoding encoding = Encoding.UTF8;
            int currentIndex = startIndex;
            int byteCount = 0;
            int charCount = 0;
            var c = new char[1];
            while (byteCount < maxBytes && currentIndex < source.Length)
            {
                c[0] = source[currentIndex];
                byteCount += encoding.GetByteCount(c);
                if (byteCount <= maxBytes)
                {
                    charCount++;
                }
                currentIndex++;
            }

            return charCount;
        }

        private void CheckEncryptor()
        {
            if (Config.Encryptor == null)
            {
                string message = "SimolConfig.Encryptor is null. An encryptor must be provided when using the SpanAttribute encryption feature.";
                throw new SimolConfigurationException(message);
            }
        }
    }
}
