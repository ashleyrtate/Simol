using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Coditate.Common.Util;
using Simol.TestSupport;
using Coditate.TestSupport;
using NUnit.Framework;

namespace Simol.Cache
{
    [TestFixture]
    public class SimpleCacheTest
    {
        private SimpleCache cache;

        [SetUp]
        public void SetUp()
        {
            cache = new SimpleCache();
        }

        [Test]
        public void PutGet()
        {
            var o1 = new object();
            string key = RandomData.AsciiString(10);

            cache.Put(key, o1);
            object o2 = cache.Get(key);

            Assert.AreSame(o1, o2);
        }

        [Test]
        public void PutGet_Expired()
        {
            cache.ExpirationInterval = TimeSpan.FromMilliseconds(100);

            var o1 = new object();
            string key = RandomData.AsciiString(10);

            cache.Put(key, o1);

            Thread.Sleep(cache.ExpirationInterval + cache.ExpirationInterval);

            object o2 = cache.Get(key);

            Assert.IsNull(o2);
        }

        [Test]
        public void Remove()
        {
            var o1 = new object();
            string key = RandomData.AsciiString(10);
            cache.Put(key, o1);

            bool removed = cache.Remove(key);

            Assert.IsTrue(removed);
            Assert.IsNull(cache.Get(key));

            removed = cache.Remove(key);
            Assert.IsFalse(removed);
        }

        [Test]
        public void Flush()
        {
            int count = 3;
            for (int k = 0; k < count; k++)
            {
                cache.Put(RandomData.NumericString(10), new object());
            }
            Assert.AreEqual(count, cache.EntryCount);

            cache.Flush();

            Assert.AreEqual(0, cache.EntryCount);
        }

        [Test]
        public void PutGet_MultiThreaded()
        {
            cache.PruneInterval = TimeSpan.FromMilliseconds(500);

            // count must be less than 90% of cache limit or cache will be pruned
            int count = 1000;
            int threads = 3;
            var dataObjects = new Dictionary<Guid, A>();
            for (int k = 0; k < count; k++)
            {
                var a = new A();
                dataObjects.Add(a.ItemName, a);
                cache.Put(a.ItemName.ToString(), a);
            }

            ParameterizedThreadStart threadStart = delegate
                {
                    // 1) Take object out of test item collection
                    // 2) Put and Get from cache
                    // 3) Put same object again or null
                    // 4) Put test item collection in same state as cache so
                    //      we can compare states at the end.
                    for (int k = 0; k < count/threads; k++)
                    {
                        A a;
                        lock ((ICollection) dataObjects)
                        {
                            a = RandomData.ListValue<A>(dataObjects.Values);
                            // entry has already been nulled out
                            if (a == null)
                            {
                                continue;
                            }
                            dataObjects.Remove(a.ItemName);
                        }
                        cache.Put(a.ItemName.ToString(), a);
                        var a2 = cache.Get(a.ItemName.ToString()) as A;

                        Assert.AreSame(a, a2);

                        Thread.Sleep(0);

                        bool remove = RandomData.Bool();
                        if (remove)
                        {
                            cache.Put(a.ItemName.ToString(), null);
                        }
                        else
                        {
                            cache.Put(a.ItemName.ToString(), a);
                        }
                        var a3 = cache.Get(a.ItemName.ToString()) as A;
                        lock ((ICollection) dataObjects)
                        {
                            dataObjects.Add(a.ItemName, a3);
                        }
                    }
                };

            var testRunner = new TestThreadRunner();
            testRunner.AddThreads(threadStart, null, threads);
            testRunner.Run();

            foreach (Guid g in dataObjects.Keys)
            {
                A a1 = dataObjects[g];
                var a2 = (A) cache[g.ToString()];

                Assert.AreSame(a1, a2);
            }
        }

        [Test]
        public void Prune_ExpiredEntry()
        {
            cache.ExpirationInterval = TimeSpan.FromMilliseconds(100);
            cache.PruneInterval = cache.ExpirationInterval;

            var o1 = new object();
            string key = RandomData.AsciiString(10);

            cache.Put(key, o1);

            Thread.Sleep(cache.ExpirationInterval + cache.ExpirationInterval);

            // cache is pruned only on put operations, so put another
            cache.Put("abc", null);
            Assert.AreEqual(1, cache.EntryCount);
        }

        [Test]
        public void Prune_MaxFill()
        {
            cache.MaxSize = 100;
            int keySize = 20;
            for (int k = 0; k < cache.MaxSize; k++)
            {
                cache.Put(RandomData.NumericString(keySize), new object());
            }
            Assert.AreEqual(cache.MaxSize, cache.EntryCount);

            cache.Put(RandomData.NumericString(keySize), new object());

            Assert.AreEqual((cache.MaxSize*.9) + 1, cache.EntryCount);
        }
    }
}