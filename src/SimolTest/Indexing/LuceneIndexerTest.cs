using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Coditate.Common.IO;
using Coditate.Common.Util;
using Coditate.TestSupport;
using NUnit.Framework;

namespace Simol.Indexing
{
    [TestFixture]
    public class LuceneIndexerTest
    {
        private string domainName;
        private LuceneIndexer indexer;
        private string tempPath;

        [SetUp]
        public void SetUp()
        {
            domainName = "TestDomain";
            tempPath = Path.Combine(Path.GetTempPath(),
                                    GetType().Name);

            IOUtils.Delete(new DirectoryInfo(tempPath), true);

            indexer = new LuceneIndexer
                {
                    IndexRootPath = tempPath
                };
        }

        [TearDown]
        public void TearDown()
        {
            indexer.Dispose();
            // wait a second to ensure discarded Lucene searchers are disposed
            Thread.Sleep(1000);
            IOUtils.Delete(new DirectoryInfo(tempPath), true);
        }

        [Test]
        public void IndexAndFind()
        {
            List<IndexValues> indexItems = CreateIndexItems();
            indexer.IndexItems(domainName, indexItems);

            // find with default property
            List<string> ids = indexer.FindItems(domainName, indexItems[0]["Field1"], 0, 100, "Field1");
            Assert.Contains(indexItems[0].Id, ids);

            // find with property embedded in query
            ids = indexer.FindItems(domainName, "Field2: " + indexItems[0]["Field2"], 0, 100, "");
            Assert.Contains(indexItems[0].Id, ids);
        }

        [Test]
        public void DeleteAndFind()
        {
            List<IndexValues> indexItems = CreateIndexItems();
            indexer.IndexItems(domainName, indexItems);

            string field1 = indexItems[0]["Field1"];
            indexItems[0]["Field1"] = null;

            indexer.IndexItems(domainName, indexItems);

            List<string> ids = indexer.FindItems(domainName, field1, 0, 100, "Field1");
            Assert.IsFalse(ids.Contains(indexItems[0].Id));
        }

        [Test]
        public void SearcherRefreshed()
        {
            List<IndexValues> indexItems = CreateIndexItems();

            // first find creates searcher
            List<string> ids = indexer.FindItems(domainName, indexItems[0]["Field1"], 0, 100, "Field1");
            Assert.IsEmpty(ids);

            // index items
            indexer.IndexItems(domainName, indexItems);

            // find again to ensure searcher was refreshed and we pick up the new items
            ids = indexer.FindItems(domainName, indexItems[0]["Field1"], 0, 100, "Field1");
            Assert.Contains(indexItems[0].Id, ids);
        }

        /// <summary>
        /// Create, use, and dispose a bunch of indexers concurrently to force our cached searchers and writers to be
        /// refreshed because of the inevitable errors. 
        /// </summary>
        [Test]
        public void FailAndContinue()
        {
            int threadCount = 3;
            int runCount = 10;
            int errorCount = 0;
            ParameterizedThreadStart callback = delegate
            {
                for (int k = 0; k < runCount; k++)
                {
                    LuceneIndexer indexer2 = new LuceneIndexer { IndexRootPath = tempPath };
                    try
                    {
                        List<IndexValues> indexItems = CreateIndexItems();
                        indexer2.IndexItems(domainName, indexItems);
                        indexer2.FindItems(domainName, indexItems[0]["Field1"], 0, 100, "Field1");
                    }
                    catch
                    {
                        Interlocked.Increment(ref errorCount);
                    }
                    finally
                    {
                        indexer2.Dispose();
                    }
                }
            };
            var runner = new TestThreadRunner();
            runner.AddThreads(callback, null, threadCount);
            runner.Run();

            // verify that failures occurred
            Assert.Greater(errorCount, 0);

            // now perform index and find with original indexer instance 
            // to verify that it has "recovered"
            List<IndexValues> indexItems2 = CreateIndexItems();
            indexer.IndexItems(domainName, indexItems2);

            List<string> ids2 = indexer.FindItems(domainName, indexItems2[0]["Field1"], 0, 100, "Field1");
            Assert.Contains(indexItems2[0].Id, ids2);
        }

        [Test]
        public void IndexAndFind_MultiThreaded()
        {
            int threadCount = 3;
            int runCount = 25;
            var errors = new List<string>();
            // ensure that an optimization happens during test
            indexer.OptimizationInterval = runCount;

            ParameterizedThreadStart callback = delegate
                {
                    for (int k = 0; k < runCount; k++)
                    {
                        try
                        {
                            List<IndexValues> indexItems = CreateIndexItems();
                            indexer.IndexItems(domainName, indexItems);

                            List<string> ids = indexer.FindItems(domainName, indexItems[0]["Field1"], 0, 100, "Field1");
                            Assert.Contains(indexItems[0].Id, ids);
                        }
                        catch (Exception ex)
                        {
                            ICollection c = errors;
                            lock (c.SyncRoot)
                            {
                                errors.Add(ex.ToString());
                            }
                        }
                    }
                };

            // setup and start thread runner
            var runner = new TestThreadRunner();
            runner.AddThreads(callback, null, threadCount);
            runner.Run();

            foreach (string s in errors)
            {
                Console.WriteLine("Error: " + s + Environment.NewLine);
            }
            Assert.AreEqual(0, errors.Count);
        }

        private List<IndexValues> CreateIndexItems()
        {
            string id = RandomData.AlphaNumericString(10, false);

            var indexItems = new List<IndexValues>();
            var indexItem = new IndexValues(id);
            indexItem["Field1"] = RandomData.AlphaNumericString(100, true);
            indexItem["Field2"] = RandomData.AlphaNumericString(100, true);
            indexItems.Add(indexItem);

            return indexItems;
        }
    }
}