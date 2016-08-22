/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Threading;
using Coditate.Common.Util;
using Common.Logging;

namespace Simol.Indexing
{
    /// <summary>
    /// Manages the process of crawling and indexing full-text indexed domains.
    /// </summary>
    /// <remarks>
    /// Each instance of this class dedicates a single thread (via the <see cref="ThreadPool"/> 
    /// to crawling each domain registered for indexing. You may tune performance by changing  
    /// <see cref="IndexBatchSize"/> or <see cref="UpdateInterval"/>. Creating and starting 
    /// multiple instances of this class may result in a slight performance increase, but is not recommended.
    /// <para>
    /// To use this class:
    /// <list type="number">
    /// <item>
    /// Register each mapping (domain) for indexing by invoking <see cref="Register"/>.
    /// </item>
    /// <item>
    /// Start the crawl process by invoking <see cref="Start"/>.
    /// </item>
    /// <item>
    /// Stop the crawl process when your server/application is shutting down by invoking <see cref="Stop"/>. 
    /// Crawl operations in progress may continue for 1-2 seconds after <c>Stop</c> returns.
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <seealso cref="IIndexer"/>
    /// <seealso cref="SimolConfig.Indexer"/>
    /// <seealso cref="LuceneIndexer"/>
    public class IndexBuilder
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Default value for <see cref="IndexBatchSize"/>. The value is 100.
        /// </summary>
        public const int DefaultIndexBatchSize = 100;

        /// <summary>
        /// Default value for <see cref="UpdateInterval"/>. The value is 1 minute.
        /// </summary>
        public static readonly TimeSpan DefaultUpdateInterval = TimeSpan.FromMinutes(1);

        private readonly Dictionary<string, DomainCrawler> crawlers = new Dictionary<string, DomainCrawler>();
        private readonly object synchLock = new object();

        private bool run;
        private bool sleeping;
        private Thread thread;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexBuilder"/> class.
        /// </summary>
        /// <param name="simol">The simol instance.</param>
        public IndexBuilder(ISimol simol)
        {
            Arg.CheckNull("simol", simol);
            Arg.CheckNull("simol.Config", simol.Config);

            Simol = simol;
            UpdateInterval = DefaultUpdateInterval;
            IndexBatchSize = DefaultIndexBatchSize;
        }

        /// <summary>
        /// Gets or sets the Simol instance to use when querying SimpleDB for indexing operations.
        /// </summary>
        /// <value>The Simol instance.</value>
        private ISimol Simol { get; set; }

        /// <summary>
        /// Gets or sets the interval at which to crawl each indexed domain.
        /// </summary>
        /// <value>The update interval.</value>
        /// <remarks>The default value is <see cref="DefaultUpdateInterval"/>.</remarks>
        public TimeSpan UpdateInterval { get; set; }

        /// <summary>
        /// Gets or sets the number of items retrieved at a time for eaching indexing operation.
        /// </summary>
        /// <value>The size of the index batch.</value>
        /// <remarks>
        /// Regardless of how high you set this value each batch retrieved for indexing will limited 
        /// to 1 MB by SimpleDB itself. The default value is <see cref="DefaultIndexBatchSize"/>
        /// </remarks>
        public int IndexBatchSize { get; set; }

        /// <summary>
        /// Registers the specified mapping for indexing.
        /// </summary>
        /// <param name="mapping">The item mapping.</param>
        public void Register(ItemMapping mapping)
        {
            Arg.CheckNull("mapping", mapping);
            State.CheckTrue(run, "Mappings may not be registered/deregistered while builder is running");

            if (crawlers.ContainsKey(mapping.DomainName))
            {
                string message =
                    string.Format("Duplicate mapping registration. A mapping was already registered for domain '{0}'.",
                                  mapping.DomainName);
                throw new InvalidOperationException(message);
            }

            var crawler = new DomainCrawler(Simol, mapping)
                {
                    IndexBatchSize = IndexBatchSize
                };
            crawlers[mapping.DomainName] = crawler;

            Log.Info(s => s("Domain '{0}' was registered with the full-text indexer.", mapping.DomainName));
        }

        /// <summary>
        /// Deregisters the specified mapping.
        /// </summary>
        /// <param name="mapping">The item mapping.</param>
        public void Deregister(ItemMapping mapping)
        {
            Arg.CheckNull("mapping", mapping);
            State.CheckTrue(run, "Mappings may not be registered/deregistered while builder is running");

            crawlers.Remove(mapping.DomainName);

            Log.Info(s => s("Domain '{0}' was de-registered from the full-text indexer.", mapping.DomainName));
        }

        /// <summary>
        /// Starts this instance and returns.
        /// </summary>
        public void Start()
        {
            State.CheckTrue(run, "Builder is already running");
            lock (synchLock)
            {
                run = true;
                thread = new Thread(Run);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            lock (synchLock)
            {
                if (thread == null)
                {
                    return;
                }
                run = false;
                if (sleeping)
                {
                    thread.Interrupt();
                }
                thread.Join();
                thread = null;
            }
        }

        private void Run()
        {
            Log.Info(s => s("Starting the full-text indexer"));
            
            while (run)
            {
                foreach (DomainCrawler crawler in crawlers.Values)
                {
                    Log.Debug(s => s("Full-text indexer is crawling domain '{0}'", crawler.Mapping.DomainName));
                    
                    ThreadPool.QueueUserWorkItem(crawler.Crawl);
                }
                Sleep(UpdateInterval);
            }

            Log.Info(s => s("Stopping the full-text indexer"));
        }

        private void Sleep(TimeSpan interval)
        {
            sleeping = true;
            try
            {
                Thread.Sleep(interval);
            }
            catch (ThreadInterruptedException)
            {
                // ignore
            }
            finally
            {
                sleeping = false;
            }
        }
    }
}