using System;
using NUnit.Framework;

namespace Simol.Formatters
{
    [TestFixture]
    public class GuidFormatterTest
    {
        private GuidFormatter formatter;
        
        [SetUp]
        public void SetUp()
        {
            formatter = new GuidFormatter();
        }

        [Test]
        public void RoundTrip()
        {
            Guid g = Guid.NewGuid();
            string s = formatter.ToString(g);
            Guid g2 = (Guid)formatter.ToType(s, typeof (Guid));

            Assert.AreEqual(g, g2);
        }
    }
}