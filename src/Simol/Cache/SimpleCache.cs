/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coditate.Common.Util;

namespace Simol.Cache
{
    /// <summary>
    /// Default item cache implementation.
    /// </summary>
    /// <remarks>
    /// All public members of this class are thread-safe.
    /// </remarks>
    public class SimpleCache : IItemCache
    {
        private class CacheEntry
        {
            public object Value { get; set; }

            public DateTime Added { get; set; }

            public bool HasExpired(TimeSpan expirationInterval)
            {
                return Added + expirationInterval <= DateTime.Now;
            }
        }

        /// <summary>
        /// Default interval at which to prune the cache (1 minute).
        /// </summary>
        public static readonly TimeSpan DefaultPruneInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Default absolute interval after which to expire cache entries (10 minutes).
        /// </summary>
        public static readonly TimeSpan DefaultExpirationInterval = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Default maximum number of entries.
        /// </summary>
        public const int DefaultMaxSize = 10000;

        private readonly Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();
        private DateTime lastPruneTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleCache"/> class.
        /// </summary>
        public SimpleCache()
        {
            PruneInterval = DefaultPruneInterval;
            ExpirationInterval = DefaultExpirationInterval;
            MaxSize = DefaultMaxSize;
        }

        /// <summary>
        /// Sets the value at the specified cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">If the key is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the key is empty</exception>
        public void Put(string key, object value)
        {
            Arg.CheckNullOrEmpty("key", key);

            PruneIfDue();

            CacheEntry entry = GetEntry(key, true);
            entry.Value = value;
            lock (((ICollection) cache).SyncRoot)
            {
                cache[key] = entry;
            }
        }

        /// <summary>
        /// Gets the value at the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>The requested value or null</returns>
        /// <exception cref="ArgumentNullException">If the key is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the key is empty</exception>
        public object Get(string key)
        {
            Arg.CheckNullOrEmpty("key", key);

            CacheEntry entry = GetEntry(key, false);
            if (entry != null && !entry.HasExpired(ExpirationInterval))
            {
                return entry.Value;
            }
            return null;
        }

        /// <summary>
        /// Removes the value at the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>true if the value was found and removed; otherwise, false</returns>
        public bool Remove(string key)
        {
            Arg.CheckNullOrEmpty("key", key);

            return cache.Remove(key);
        }

        /// <summary>
        /// Flushes all items from the cache.
        /// </summary>
        public void Flush()
        {
            lock (((ICollection)cache).SyncRoot)
            {
                cache.Clear();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> at the specified key.
        /// </summary>
        /// <value></value>
        /// <exception cref="ArgumentNullException">If the key is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the key is empty</exception>
        public object this[string key]
        {
            get { return Get(key); }
            set { Put(key, value); }
        }

        /// <summary>
        /// Gets or sets the interval at which to prune the cache.
        /// </summary>
        /// <value>The prune interval.</value>
        public TimeSpan PruneInterval { get; set; }

        /// <summary>
        /// Gets or sets the absolute interval after which unused cache entries will expire.
        /// </summary>
        /// <value>The expiration interval.</value>
        public TimeSpan ExpirationInterval { get; set; }

        /// <summary>
        /// Gets or sets the max number of entries allowed in the cache.
        /// </summary>
        /// <value>The max size.</value>
        public int MaxSize { get; set; }

        internal int EntryCount
        {
            get
            {
                lock (((ICollection) cache).SyncRoot)
                {
                    return cache.Count;
                }
            }
        }

        private CacheEntry GetEntry(string key, bool create)
        {
            CacheEntry value;
            lock (((ICollection) cache).SyncRoot)
            {
                cache.TryGetValue(key, out value);
            }
            if (value == null && create)
            {
                value = new CacheEntry
                    {
                        Added = DateTime.Now
                    };
            }
            return value;
        }

        private void PruneIfDue()
        {
            DateTime now = DateTime.Now;
            bool pruneDue = now > lastPruneTime + PruneInterval;

            lock (((ICollection) cache).SyncRoot)
            {
                if (!pruneDue && cache.Count < MaxSize)
                {
                    return;
                }
                // attempts to prune cache multiple times, each time
                // with a more recent expiration time until fill % is less than 90%
                int attempts = 0;
                long expirationIntervalTicks = ExpirationInterval.Ticks;
                do
                {
                    DateTime expiration = now -
                                          TimeSpan.FromTicks(expirationIntervalTicks);
                    PruneExpired(expiration, (int) (MaxSize*.1));
                    expirationIntervalTicks -= (ExpirationInterval.Ticks*attempts);
                    attempts++;
                } while (cache.Count > MaxSize*.9);
                lastPruneTime = now;
            }
        }

        private void PruneExpired(DateTime expiration, int maxToPrune)
        {
            // remove entries added before the specified time
            List<string> expired =
                cache.Where(c => c.Value.Added < expiration).Take(maxToPrune).Select(c => c.Key).ToList();
            foreach (string key in expired)
            {
                cache.Remove(key);
            }
        }
    }
}