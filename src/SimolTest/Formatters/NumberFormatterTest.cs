using System;
using NUnit.Framework;

namespace Simol.Formatters
{
    [TestFixture]
    public class NumberFormatterTest
    {
        [Test]
        public void GetFormatString_NoOffset()
        {
            byte[] wholeDigits = {1, 3, 10};
            byte[] decimalDigits = {0, 5, 8};

            string[] expectedFormats = {"0", "000.#####", "0000000000.########"};

            for (int k = 0; k < wholeDigits.Length; k++)
            {
                var formatter = new NumberFormatter
                    {
                        WholeDigits = wholeDigits[k],
                        DecimalDigits = decimalDigits[k],
                        ApplyOffset = false
                    };
                Assert.AreEqual(expectedFormats[k], formatter.Format);
            }
        }

        [Test]
        public void GetFormatString_WithOffset()
        {
            string expectedFormat = "00000000000";
            var formatter = new NumberFormatter
                {
                    WholeDigits = 10,
                    DecimalDigits = 0,
                    ApplyOffset = true
                };
            Assert.AreEqual(expectedFormat, formatter.Format);
        }

        [Test]
        public void GetOffsetAmount()
        {
            byte[] wholeDigits = {1, 3, 10};
            decimal[] expectedOffsets = {10, 1000, 10000000000};


            for (int k = 0; k < wholeDigits.Length; k++)
            {
                var formatter = new NumberFormatter
                    {
                        WholeDigits = wholeDigits[k]
                    };
                Assert.AreEqual(expectedOffsets[k], formatter.OffsetAmount);
            }
        }

        [Test]
        public void FormatOverflowValues()
        {
            object[] numbers = new object[] {double.NaN, float.NaN, double.NegativeInfinity, float.NegativeInfinity, double.PositiveInfinity, float.PositiveInfinity};

            // test without offset
            var formatter = new NumberFormatter
                {
                    ApplyOffset = false
                };
            foreach (object n1 in numbers)
            {
                string formatted = formatter.ToString(n1);
                object n2 = formatter.ToType(formatted, n1.GetType());

                Assert.AreEqual(n1, n2);
            }

            // test with offset
            formatter.ApplyOffset = true;
            foreach (object n1 in numbers)
            {
                string formatted = formatter.ToString(n1);
                object n2 = formatter.ToType(formatted, n1.GetType());

                Assert.AreEqual(n1, n2);
            }
        }

        [Test, ExpectedException(typeof(OverflowException))]
        public void ToString_Overflow()
        {
            var formatter = new NumberFormatter();

            formatter.ToString(double.MaxValue);
        }

        [Test, ExpectedException(typeof(FormatException))]
        public void ToType_Invalid()
        {
            var formatter = new NumberFormatter();

            formatter.ToType("abc", typeof(int));
        }
    }
}