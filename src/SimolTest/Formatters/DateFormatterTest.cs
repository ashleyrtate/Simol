using System;
using System.Globalization;
using NUnit.Framework;

namespace Simol.Formatters
{
    [TestFixture]
    public class DateFormatterTest
    {
        private DateFormatter formatter;

        [SetUp]
        public void SetUp()
        {
            formatter = new DateFormatter("yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture,
                                          DateTimeStyles.RoundtripKind);
        }

        private DateTime Truncate(DateTime d)
        {
            return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, d.Millisecond, d.Kind);
        }

        [Test]
        public void RoundTrip_Local()
        {
            DateTime first = Truncate(DateTime.Now);

            Assert.AreEqual(DateTimeKind.Local, first.Kind);

            string s = formatter.ToString(first);
            var second = (DateTime) formatter.ToType(s, typeof (DateTime));

            Assert.AreEqual(first, second);
        }

        [Test]
        public void RoundTrip_Unspecified()
        {
            DateTime first = Truncate(new DateTime(DateTime.Now.Ticks));

            Assert.AreEqual(DateTimeKind.Unspecified, first.Kind);

            string s = formatter.ToString(first);
            var second = (DateTime) formatter.ToType(s, typeof (DateTime));

            Assert.AreEqual(first, second);
        }

        [Test]
        public void RoundTrip_Utc()
        {
            DateTime first = Truncate(DateTime.UtcNow);

            Assert.AreEqual(DateTimeKind.Utc, first.Kind);

            string s = formatter.ToString(first);
            var second = (DateTime) formatter.ToType(s, typeof (DateTime));

            Assert.AreEqual(first, second);
        }
    }
}