using System;
using Coditate.TestSupport;
using NUnit.Framework;
using Coditate.Common.Util;

namespace Simol.Formatters
{
    [TestFixture]
    public class TimeSpanFormatterTest
    {
        private TimeSpanFormatter formatter;
        
        [SetUp]
        public void SetUp()
        {
            formatter = new TimeSpanFormatter();
        }

        [Test]
        public void RoundTrip()
        {
            TimeSpan t = TimeSpan.FromTicks((long)RandomData.Double()*TimeSpan.MaxValue.Ticks);
            string s = formatter.ToString(t);
            TimeSpan t2 = (TimeSpan)formatter.ToType(s, typeof(TimeSpan));

            Assert.AreEqual(t, t2);
        }
    }
}