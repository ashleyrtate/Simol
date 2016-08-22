using System;
using NUnit.Framework;

namespace Simol.Data
{
    [TestFixture]
    public class IndexStateTest
    {
        [Test]
        public void GetMachineGuid()
        {
            Guid g = IndexState.GetMachineGuid();

            Assert.AreNotEqual(Guid.Empty, g);
        }

        [Test]
        public void GetDataType()
        {
            IndexState i  = new IndexState();

            Assert.AreEqual(typeof(IndexState).Name, i.DataType);
        }
    }
}