using Amazon.SimpleDB.Model;
using Coditate.TestSupport;
using NUnit.Framework;

namespace Simol.Data
{
    [TestFixture]
    public class SimpleDBRequestFormatterTest
    {
        private SimpleDBRequestFormatter formatter;

        [SetUp]
        public void SetUp()
        {
            formatter = new SimpleDBRequestFormatter();
        }

        [Test]
        public void RoundTrip_PutAttributesRequest()
        {
            var request = new PutAttributesRequest();
            string xml = formatter.ToString(request);
            object request2 = formatter.ToType(xml, typeof (object));

            PropertyMatcher.MatchResult match = PropertyMatcher.AreEqual(request, request2);
            Assert.IsTrue(match.Equal, match.Message);
        }

        [Test]
        public void RoundTrip_BatchPutAttributesRequest()
        {
            var request = new BatchPutAttributesRequest();
            string xml = formatter.ToString(request);
            object request2 = formatter.ToType(xml, typeof (object));

            PropertyMatcher.MatchResult match = PropertyMatcher.AreEqual(request, request2);
            Assert.IsTrue(match.Equal, match.Message);
        }

        [Test]
        public void RoundTrip_DeleteAttributesRequest()
        {
            var request = new DeleteAttributesRequest();
            string xml = formatter.ToString(request);
            object request2 = formatter.ToType(xml, typeof (object));

            PropertyMatcher.MatchResult match = PropertyMatcher.AreEqual(request, request2);
            Assert.IsTrue(match.Equal, match.Message);
        }

        [Test]
        public void RoundTrip_BatchDeleteAttributesRequest()
        {
            var request = new BatchDeleteAttributesRequest();
            string xml = formatter.ToString(request);
            object request2 = formatter.ToType(xml, typeof(object));

            PropertyMatcher.MatchResult match = PropertyMatcher.AreEqual(request, request2);
            Assert.IsTrue(match.Equal, match.Message);
        }
    }
}