using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Coditate.Common.Util;
using Simol.Formatters;
using Simol.TestSupport;
using NUnit.Framework;
using AmazonAttribute = Amazon.SimpleDB.Model.Attribute;
using Simol.Security;

namespace Simol.Core
{
    [TestFixture]
    public class SpanUtilsTest
    {
        private SpanUtils spanUtils;

        [SetUp]
        public void SetUp()
        {
            var config = new SimolConfig();
            var encryptor = (AesEncryptor)config.Encryptor;
            encryptor.Key = AesEncryptor.GenerateKey();
            encryptor.IV = AesEncryptor.GenerateIV();
            spanUtils = new SpanUtils(config);
        }

        [Test]
        public void SplitJoin_Random()
        {
            SplitJoinRandom(.3, 10, SpanType.None);
        }

        [Test]
        public void SplitJoin_RandomCompressed()
        {
            SplitJoinRandom(.15, 10, SpanType.Compress);
        }

        [Test]
        public void SplitJoin_RandomEncrypted()
        {
            SplitJoinRandom(.15, 10, SpanType.Encrypt);
        }

        [Test]
        public void SplitJoin_RandomCompressedEncrypted()
        {
            SplitJoinRandom(.15, 10, SpanType.Compress | SpanType.Encrypt);
        }

        private void SplitJoinRandom(double fillPercent, int runs, SpanType span)
        {
            for (int k = 0; k < runs; k++)
            {
                spanUtils.Config.MaxAttributeLength = RandomData.Generator.Next(10, 1500);
                // encoding of random data results in more than 1 byte per character so limit size to less than 100% of byte capacity
                var dataLength =
                    (int)
                    ((spanUtils.Config.MaxAttributeLength - SpanUtils.ChunkIndexLength) *
                     RandomData.Generator.Next(0, SpanUtils.MaxChunks) * fillPercent);
                string data1 = RandomData.String(dataLength);
                List<string> chunks = spanUtils.SplitPropertyValue(data1, span);
                string data2 = spanUtils.JoinAttributeValues(chunks, span);
                Assert.AreEqual(data1, data2);

                // verify that UTF-8 encoded characters don't exceed max allowed size
                foreach (string s in chunks)
                {
                    byte[] b = Encoding.UTF8.GetBytes(s);
                    Assert.GreaterOrEqual(spanUtils.Config.MaxAttributeLength, b.Length);
                }
            }
        }

        [Test]
        public void SplitJoin_Boundaries()
        {
            spanUtils.Config.MaxAttributeLength = 10;
            // dataLength is intentionally the largest prime number under 7000
            int dataLength = 6997;
            string data1 = RandomData.AlphaNumericString(dataLength, true);
            int testCount = 100;
            for (int k = 0; k < testCount; k++)
            {
                List<string> chunks = spanUtils.SplitPropertyValue(data1, SpanType.Span);
                string data2 = spanUtils.JoinAttributeValues(chunks, SpanType.Span);
                Assert.AreEqual(data1, data2);
                spanUtils.Config.MaxAttributeLength++;
            }
        }

        [Test,
         ExpectedException(typeof(SimolDataException),
             ExpectedMessage =
                 "Spanned property overflow. String property with length of 7001 characters requires more than 1000 attributes to store."
             )]
        public void SplitPropertyValue_Overflow()
        {
            spanUtils.Config.MaxAttributeLength = 10;
            int maxAttributeSize = 10;
            int overflowLength = (maxAttributeSize - SpanUtils.ChunkIndexLength) * SpanUtils.MaxChunks + 1;
            string value = RandomData.AlphaNumericString(overflowLength, true);

            spanUtils.SplitPropertyValue(value, SpanType.Span);
        }

        [Test,
         ExpectedException(typeof(SimolConfigurationException),
             ExpectedMessage =
                 "SimolConfig.MaxAttributeLength may not be set to less than 4"
             )]
        public void SplitPropertyValue_InvalidAttributeLength()
        {
            spanUtils.Config.MaxAttributeLength = 0;
            spanUtils.SplitPropertyValue("", SpanType.Span);
        }

        [Test,
         ExpectedException(typeof(SimolConfigurationException),
             ExpectedMessage =
                 "SimolConfig.MaxAttributeLength may not be set to less than 4"
             )]
        public void JoinAttributeValues_InvalidAttributeLength()
        {
            spanUtils.Config.MaxAttributeLength = 0;
            spanUtils.JoinAttributeValues(new List<string> { "1234" }, SpanType.Span);
        }

        [Test,
         ExpectedException(typeof(SimolDataException),
             ExpectedMessage =
                 "Unable to reassemble spanned attributes property. The data may have been corrupted."
             )]
        public void JoinAttributeValues_CorruptedData()
        {
            spanUtils.Config.MaxAttributeLength = 10;
            spanUtils.JoinAttributeValues(new List<string> { "1" }, SpanType.Span);
        }
    }
}