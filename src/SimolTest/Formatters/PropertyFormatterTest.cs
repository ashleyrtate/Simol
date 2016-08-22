using NUnit.Framework;

namespace Simol.Formatters
{
    [TestFixture]
    public class PropertyFormatterTest
    {
        private PropertyFormatter formatter;

        [SetUp]
        public void SetUp()
        {
            formatter = new PropertyFormatter(new SimolConfig());
        }

        [Test]
        public void Format_Int()
        {
            int intValue = 1;
            string expectedFormat = "10000000001";
            string actualFormat = formatter.ToString(null, intValue);

            Assert.AreEqual(expectedFormat, actualFormat);

            intValue = -1;
            expectedFormat = "09999999999";
            actualFormat = formatter.ToString(null, intValue);

            Assert.AreEqual(expectedFormat, actualFormat);
        }

        [Test]
        public void Format_Null()
        {
            int? intValue1 = null;
            string expectedFormat = PropertyFormatter.NullString;
            string actualFormat = formatter.ToString(null, intValue1);

            Assert.AreEqual(expectedFormat, actualFormat);

            int? intValue2 = null;
            intValue2 = (int?) formatter.ToType(null, PropertyFormatter.Base64NullString, typeof (int));

            Assert.AreEqual(intValue1, intValue2);
        }
    }
}