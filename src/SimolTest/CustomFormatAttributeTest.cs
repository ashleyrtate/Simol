using System;
using Coditate.TestSupport;
using NUnit.Framework;
using Coditate.Common.Util;

namespace Simol
{
    [TestFixture]
    public class CustomFormatAttributeTest
    {
        private class TestFormatter : ITypeFormatter
        {
            public TestFormatter(string first, int second)
            {
                First = first;
                Second = second;
            }

            public int Second { get; set; }

            public string First { get; set; }

            public string ToString(object value)
            {
                throw new NotImplementedException();
            }

            public object ToType(string value, Type expected)
            {
                throw new NotImplementedException();
            }
        }

        [Test,
         ExpectedException(typeof (ArgumentException),
             ExpectedMessage =
                 "Unable to instantiate type formatter 'Simol.CustomFormatAttributeTest+TestFormatter' with '1' constructor argument(s). The argument values were '100'."
             )]
        public void InvalidTypeFormatter()
        {
            new CustomFormatAttribute(typeof (TestFormatter), 100);
        }

        [Test]
        public void FormatterWithConstructorArgs()
        {
            string first = RandomData.AsciiString(10);
            int second = RandomData.Int();
            CustomFormatAttribute formatAttribute = new CustomFormatAttribute(typeof (TestFormatter), first, second);

            TestFormatter formatter = formatAttribute.Formatter as TestFormatter;
            Assert.IsNotNull(formatter);
            Assert.AreEqual(first, formatter.First);
            Assert.AreEqual(second, formatter.Second);
        }
    }
}