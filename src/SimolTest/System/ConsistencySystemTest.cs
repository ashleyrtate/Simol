using System;
using System.Collections.Generic;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Coditate.Common.Util;
using Simol.Consistency;
using Simol.TestSupport;
using NUnit.Framework;

namespace Simol.System
{
    [TestFixture, Explicit]
    public class ConsistencySystemTest
    {
        private const int itemCount = 10;
        private const string TestDomain = "ConsistencyTest";
        private List<ConsistencyTestItem> dataItems;
        private SimolClient simol;

        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            simol = SystemTestUtils.GetSimol();
            var request = new DeleteDomainRequest {DomainName = TestDomain};
            simol.SimpleDB.DeleteDomain(request);
        }

        [TestFixtureTearDown]
        public void TearDownFixture()
        {
            var request = new DeleteDomainRequest {DomainName = TestDomain};
            simol.SimpleDB.DeleteDomain(request);
        }

        [Test]
        public void SystemTest()
        {
            DateTime start = DateTime.Now;

            Console.WriteLine("Starting test...");

            start = DateTime.Now;
            PutGet();
            SystemTestUtils.Log("Putting and Getting...", ref start);

            PutStaleNoVersion();
            SystemTestUtils.Log("Putting stale data with NO version...", ref start);

            PutStaleOldVersion();
            SystemTestUtils.Log("Putting stale data with OLD version...", ref start);
        }

        private void PutGet()
        {
            dataItems = CreateItems(itemCount);
            foreach (ConsistencyTestItem item1 in dataItems)
            {
                simol.Put(item1);

                using (new ConsistentReadScope())
                {
                    var item2 = simol.Get<ConsistencyTestItem>(item1.ItemName);

                    Assert.IsNotNull(item2);
                    Assert.AreEqual(item1.IntValue, item2.IntValue);
                    Assert.Greater(item2.Version, item1.Version);
                }
            }
        }

        private List<ConsistencyTestItem> CreateItems(int count)
        {
            var list = new List<ConsistencyTestItem>();
            for (int k = 0; k < count; k++)
            {
                ConsistencyTestItem item1 = CreateItem();
                list.Add(item1);
            }
            return list;
        }

        private void PutStaleNoVersion()
        {
            foreach (ConsistencyTestItem item in dataItems)
            {
                try
                {
                    // put item again with default/empty version
                    simol.Put(item);

                    Assert.Fail(item.ItemName.ToString());
                }
                catch (AmazonSimpleDBException ex)
                {
                    Assert.AreEqual("ConditionalCheckFailed", ex.ErrorCode);
                }
            }
        }

        private void PutStaleOldVersion()
        {
            foreach (ConsistencyTestItem item in dataItems)
            {
                // increment version and put item
                item.Version++;
                simol.Put(item);

                try
                {
                    // put item again with same version
                    simol.Put(item);

                    Assert.Fail(item.ItemName.ToString());
                }
                catch (AmazonSimpleDBException ex)
                {
                    Assert.AreEqual("ConditionalCheckFailed", ex.ErrorCode);
                }
            }
        }

        private ConsistencyTestItem CreateItem()
        {
            return new ConsistencyTestItem
                {
                    IntValue = RandomData.Generator.Next(),
                    ItemName = Guid.NewGuid()
                };
        }

        [DomainName(TestDomain)]
        public class ConsistencyTestItem : TestItemBase
        {
            public int IntValue { get; set; }

            [Version(VersioningBehavior.AutoIncrementAndConditionallyUpdate)]
            public int Version { get; set; }
        }
    }
}