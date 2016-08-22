using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SimpleDB.Model;
using Coditate.Common.Util;
using Simol.Consistency;
using Simol.Data;
using Simol.TestSupport;
using Coditate.TestSupport;
using NUnit.Framework;

namespace Simol.System
{
    [TestFixture, Explicit]
    public class ReliableWriteSystemTest
    {
        private const int itemCount = 10;
        private const string TestDomain = "ReliableWriteTest";
        private List<ReliableWriteTestItem> dataItems;
        private WriteMonitor monitor;
        private SimolClient simol;

        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            simol = SystemTestUtils.GetSimol();
            monitor = CreateMonitor();

            var request = new DeleteDomainRequest {DomainName = "ConsistencyTestItem"};
            simol.SimpleDB.DeleteDomain(request);
        }

        [TestFixtureTearDown]
        public void TearDownFixture()
        {
            monitor.Stop();

            var request = new DeleteDomainRequest {DomainName = "ConsistencyTestItem"};
            simol.SimpleDB.DeleteDomain(request);
        }

        private WriteMonitor CreateMonitor()
        {
            return new WriteMonitor(SystemTestUtils.GetSimol())
                {
                    ReprocessDelay = TimeSpan.FromMilliseconds(1),
                    RetryInterval = TimeSpan.FromMilliseconds(1)
                };
        }

        [Test]
        public void SystemTest()
        {
            DateTime start = DateTime.Now;

            Console.WriteLine("Starting test...");

            start = DateTime.Now;

            PutwithReliableWrites();
            SystemTestUtils.Log("Putting cross-domain data with reliable-write...", ref start);

            PropagateFailedWrites();
            SystemTestUtils.Log("Propagating failed writes...", ref start);

            ValidatePutPropagation();
            SystemTestUtils.Log("Validating put propagation...", ref start);

            DeleteWithReliableWrites();
            SystemTestUtils.Log("Deleting cross-domain data with reliable-write...", ref start);

            PropagateFailedWrites();
            SystemTestUtils.Log("Propagating failed writes...", ref start);

            ValidateDeletePropagation();
            SystemTestUtils.Log("Validating delete propagation...", ref start);
        }

        private List<ReliableWriteTestItem> CreateItems(int count)
        {
            var list = new List<ReliableWriteTestItem>();
            for (int k = 0; k < count; k++)
            {
                ReliableWriteTestItem item1 = CreateItem();
                list.Add(item1);
            }
            return list;
        }

        /// <summary>
        /// Uses reliable write to synch data in multiple domains.
        /// </summary>
        private void PutwithReliableWrites()
        {
            dataItems = CreateItems(itemCount);
            foreach (ReliableWriteTestItem item in dataItems)
            {
                using (var writeScope = new ReliableWriteScope(monitor))
                {
                    List<SystemTestItem> relatedItems = CreateRelatedItems(3);
                    item.RelatedItems = relatedItems.Select(i => i.ItemName).ToList();
                    // switch between put-attributes and batch-put-attributes
                    if (RandomData.Bool())
                    {
                        simol.Put(relatedItems);
                    }
                    else
                    {
                        foreach (SystemTestItem r in relatedItems)
                        {
                            simol.Put(r);
                        }
                    }
                    simol.Put(item);

                    // switch between immediate and delayed propagation
                    if (RandomData.Bool())
                    {
                        writeScope.Commit(false);
                    }
                    else
                    {
                        writeScope.Commit();
                    }
                }
            }
        }

        private void DeleteWithReliableWrites()
        {
            foreach (ReliableWriteTestItem item in dataItems)
            {
                using (var writeScope = new ReliableWriteScope(monitor))
                {
                    simol.Delete<ReliableWriteTestItem>(item.ItemName);
                    foreach (Guid relatedId in item.RelatedItems)
                    {
                        simol.Delete<SystemTestItem>(relatedId);
                    }

                    // switch between immediate and delayed propagation
                    if (RandomData.Bool())
                    {
                        writeScope.Commit(false);
                    }
                    else
                    {
                        writeScope.Commit();
                    }
                }
            }
        }

        private void ValidateDeletePropagation()
        {
            // verify that all deletes completed
            using (new ConsistentReadScope())
            {
                foreach (ReliableWriteTestItem item in dataItems)
                {
                    var item2 = simol.Get<ReliableWriteTestItem>(item.ItemName);

                    Assert.IsNull(item2);
                    foreach (Guid relatedId in item.RelatedItems)
                    {
                        var relatedItem = simol.Get<SystemTestItem>(relatedId);
                        Assert.IsNull(relatedItem);
                    }
                }
            }
        }

        private void PropagateFailedWrites()
        {
            // periodically sleep and destructively dispose the underlying SimpleDB client
            // while waiting for writes to propagate
            int sleepTime = 1000;
            do
            {
                monitor.Start();
                Thread.Sleep(sleepTime);
                monitor.Simol.SimpleDB.Dispose();
                monitor.Stop();
                monitor = CreateMonitor();
                sleepTime *= 2;
            } while (GetWriteStepCount() > 0);
        }

        private void ValidatePutPropagation()
        {
            // verify that all writes completed
            using (new ConsistentReadScope())
            {
                foreach (ReliableWriteTestItem item in dataItems)
                {
                    var item2 = simol.Get<ReliableWriteTestItem>(item.ItemName);

                    Assert.IsNotNull(item2);
                    foreach (Guid relatedId in item2.RelatedItems)
                    {
                        var relatedItem = simol.Get<SystemTestItem>(relatedId);
                        Assert.IsNotNull(relatedItem);
                    }
                }
            }
        }

        private int GetWriteStepCount()
        {
            using (new ConsistentReadScope())
            {
                return (int)
                       simol.SelectScalar<ReliableWriteStep>(
                           "select count(*) from SimolSystem where DataType = 'ReliableWriteStep'");
            }
        }

        private List<SystemTestItem> CreateRelatedItems(int count)
        {
            var related = new List<SystemTestItem>();
            for (int k = 0; k < count; k++)
            {
                SystemTestItem testItem = SystemTestItem.Create();
                related.Add(testItem);
            }
            return related;
        }

        private ReliableWriteTestItem CreateItem()
        {
            return new ReliableWriteTestItem
                {
                    IntValue = RandomData.Generator.Next(),
                    ItemName = Guid.NewGuid()
                };
        }

        [DomainName(TestDomain)]
        public class ReliableWriteTestItem : TestItemBase
        {
            public int IntValue { get; set; }

            [Version(VersioningBehavior.AutoIncrement)]
            public int Version { get; set; }

            public List<Guid> RelatedItems { get; set; }
        }
    }
}