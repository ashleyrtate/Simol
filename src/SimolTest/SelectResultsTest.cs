using System.Collections;
using Simol.TestSupport;
using NUnit.Framework;

namespace Simol
{
    [TestFixture]
    public class SelectResultsTest
    {
        [Test]
        public void EnumerateResults()
        {
            SelectResults<C> results = new SelectResults<C>();

            for (int k = 0; k < 3; k++)
            {
                results.Items.Add(new C());
            }

            IEnumerable e = results;
            foreach (C c in e)
            {
                Assert.IsNotNull(c);
            }
        }
    }
}