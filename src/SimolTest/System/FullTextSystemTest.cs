using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Amazon.SimpleDB.Model;
using Coditate.Common.IO;
using Coditate.Common.Util;
using Simol.Indexing;
using NUnit.Framework;

namespace Simol.System
{
    [TestFixture, Explicit]
    public class FullTextSystemTest
    {
        private const int itemCount = 100;
        private readonly TimeSpan SleepTime = TimeSpan.FromSeconds(10);
        private List<FullTextTestItem> dataItems;
        private IndexBuilder indexBuilder;
        private SimolClient simol;
        private string searchKey1, searchKey2;
        private string tempPath;

        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            tempPath = Path.Combine(Path.GetTempPath(),
                                    GetType().Name);

            IOUtils.Delete(new DirectoryInfo(tempPath), true);


            simol = SystemTestUtils.GetSimol();
            // set global consistent read rather than scoped because find methods read on background threads
            simol.Config.ReadConsistency = ConsistencyBehavior.Immediate;

            var request = new DeleteDomainRequest {DomainName = "SystemTest"};
            simol.SimpleDB.DeleteDomain(request);

            ItemMapping mapping = ItemMapping.Create(typeof (FullTextTestItem));
            indexBuilder = new IndexBuilder(simol)
                {
                    UpdateInterval = TimeSpan.FromSeconds(5),
                    IndexBatchSize = 25
                };
            indexBuilder.Register(mapping);
            indexBuilder.Start();

            searchKey1 = RandomData.NumericString(10);
            searchKey2 = RandomData.NumericString(10);
        }

        [TestFixtureTearDown]
        public void TearDownFixture()
        {
            indexBuilder.Stop();

            // give the domain crawlers a second to finish before disposing index
            Thread.Sleep(1000);
            simol.Config.Indexer.Dispose();

            var request = new DeleteDomainRequest {DomainName = "SystemTest"};
            simol.SimpleDB.DeleteDomain(request);

            IOUtils.Delete(new DirectoryInfo(tempPath), true);
        }

        [Test]
        public void SystemTest()
        {
            DateTime start = DateTime.Now;

            Console.WriteLine("Starting test...");

            start = DateTime.Now;
            Put();
            Log("Putting...", ref start);

            Thread.Sleep(SleepTime);
            Log("Sleeping...", ref start);

            List<FullTextTestItem> foundItems = simol.Find<FullTextTestItem>(searchKey1, 0, dataItems.Count,
                                                                          "LongStringValue1");
            Log("Finding by first key...", ref start);

            Assert.AreEqual(dataItems.Count, foundItems.Count);

            foundItems = simol.Find<FullTextTestItem>(searchKey2, 0, dataItems.Count,
                                                     "LongStringValue1");
            Log("Finding by second key...", ref start);

            Assert.IsEmpty(foundItems);

            Update();
            Log("Updating with second key...", ref start);

            Thread.Sleep(SleepTime);
            Log("Sleeping...", ref start);

            foundItems = simol.Find<FullTextTestItem>(searchKey2, 0, dataItems.Count,
                                                     "LongStringValue1");
            Log("Finding by second key again...", ref start);

            Assert.AreEqual(dataItems.Count, foundItems.Count);

            int deleteCount = dataItems.Count/2;
            DeleteIndexedAttributes(deleteCount);
            Log("Deleting indexed attributes...", ref start);

            Thread.Sleep(SleepTime);
            Log("Sleeping...", ref start);

            foundItems = simol.Find<FullTextTestItem>(searchKey2, 0, dataItems.Count,
                                                     "LongStringValue1");
            Assert.AreEqual(dataItems.Count - deleteCount, foundItems.Count);

            DeleteItems();
            Log("Deleting items...", ref start);

            Thread.Sleep(SleepTime);
            Log("Sleeping...", ref start);

            foundItems = simol.Find<FullTextTestItem>(searchKey2, 0, dataItems.Count,
                                                     "LongStringValue1");
            Assert.AreEqual(0, foundItems.Count);
        }

        private void Log(string message, ref DateTime start)
        {
            DateTime end = DateTime.Now;

            Console.WriteLine(message);
            Console.WriteLine("\t" + (end - start).TotalSeconds + " sec");
            start = DateTime.Now;
        }

        private void Put()
        {
            dataItems = new List<FullTextTestItem>();
            for (int k = 0; k < itemCount; k++)
            {
                FullTextTestItem item = FullTextTestItem.Create();
                item.LongStringValue1 = item.LongStringValue1 + " " + searchKey1;
                dataItems.Add(item);
            }

            simol.Put(dataItems);
        }

        private void Update()
        {
            foreach (FullTextTestItem item in dataItems)
            {
                item.LongStringValue1 = item.LongStringValue1 + " " + searchKey2;
                item.VersionValue = DateTime.Now;
            }

            simol.Put(dataItems);
        }

        private void DeleteIndexedAttributes(int count)
        {
            IEnumerable<FullTextTestItem> toDelete = dataItems.Take(count);
            foreach (FullTextTestItem item in toDelete)
            {
                item.LongStringValue1 = "abc";
                item.VersionValue = DateTime.Now;
            }

            simol.Put(toDelete.ToList());
        }

        private void DeleteItems()
        {
            foreach (FullTextTestItem item in dataItems)
            {
                simol.Delete<FullTextTestItem>(item.ItemName);
            }
        }
    }
}