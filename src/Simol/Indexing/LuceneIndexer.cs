/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Coditate.Common.Util;
using Common.Logging;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace Simol.Indexing
{
    /// <summary>
    /// An indexer implementation for Lucene.NET 
    /// (<a href="http://incubator.apache.org/lucene.net/">http://incubator.apache.org/lucene.net/</a>).
    /// </summary>
    /// <remarks>
    /// Instances of this class are thread-safe. However all instances share the same set of
    /// Lucene <c>IndexWriter</c>s and <c>IndexSearcher</c>s (one per index/domain). This is recommended as writers and 
    /// searchers are expensive to create. 
    /// 
    /// <para>Since underlying resources are shared there is no performance
    /// benefit from using multiple instances and there <em>can</em> be adverse consequences: disposing one instance
    /// will close the shared Lucene resources and may interfere with ongoing operations in the other 
    /// instances.</para>
    /// </remarks>
    /// <seealso cref="IIndexer"/>
    /// <seealso cref="IndexBuilder"/>
    public class LuceneIndexer : IIndexer
    {
        /// <summary>
        /// The default value for <see cref="IndexRootPath"/>. The value is "/SimolLucene/".
        /// </summary>
        public const string DefaultIndexRootPath = @"/SimolLucene/";

        /// <summary>
        /// The default value for <see cref="OptimizationInterval"/>. The value is 100.
        /// </summary>
        public const int DefaultOptimizationInterval = 100;

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<string, IndexSearcher> searchers = new Dictionary<string, IndexSearcher>();
        private static readonly Dictionary<string, IndexWriter> writers = new Dictionary<string, IndexWriter>();
        private int updateCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneIndexer"/> class.
        /// </summary>
        public LuceneIndexer()
        {
            IndexRootPath = DefaultIndexRootPath;
            OptimizationInterval = DefaultOptimizationInterval;
        }

        private bool IsDisposed { get; set; }

        /// <summary>
        /// Gets or sets the Lucene index optimization interval.
        /// </summary>
        /// <value>The optimization interval.</value>
        /// <remarks>The default value is <see cref="DefaultOptimizationInterval"/>.</remarks>
        public int OptimizationInterval { get; set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            lock (((ICollection) searchers).SyncRoot)
            {
                foreach (IndexSearcher searcher in searchers.Values)
                {
                    searcher.Close();
                }
                searchers.Clear();
            }
            lock (((ICollection) writers).SyncRoot)
            {
                foreach (IndexWriter writer in writers.Values)
                {
                    writer.Close();
                }
                writers.Clear();
            }
            // run the garbage collector primarily to ensure that searchers we
            // discarded without closing are cleaned up
            GC.Collect();
        }

        /// <summary>
        /// Gets or sets the index root path.
        /// </summary>
        /// <value>The index root path.</value>
        /// <remarks>The default value is <see cref="DefaultIndexRootPath"/></remarks>
        public string IndexRootPath { get; set; }

        /// <summary>
        /// Indexes a list of item values and stores them in an index dedicated to the
        /// specified domain.
        /// </summary>
        /// <param name="domain">The domain being indexed.</param>
        /// <param name="items">The items to index.</param>
        public void IndexItems(string domain, List<IndexValues> items)
        {
            Arg.CheckNullOrEmpty("domain", domain);
            Arg.CheckNull("items", items);

            State.CheckTrue(IsDisposed, "Indexer has been disposed");
            try
            {
                // open/create index
                IndexWriter writer = GetWriter(domain, false);

                foreach (IndexValues item in items)
                {
                    IndexItem(writer, item);
                    updateCount++;

                    Log.Debug(m => m("Indexed item with domain '{0}' and id '{1}'. Indexed properties = {2}", domain, item.Id, 
                        StringUtils.Join(", ", item.Values.Keys)));
                }
                writer.Commit();

                if (updateCount > OptimizationInterval)
                {
                    writer.Optimize();
                    updateCount = 0;
                }
            }
            catch
            {
                // force a refresh of the cached writer if the write fails with an exception
                GetWriter(domain, true);
                throw;
            }
        }

        /// <summary>
        /// Finds items containing the specified full-text query terms.
        /// </summary>
        /// <param name="domain">The domain index to query.</param>
        /// <param name="queryText">The query text.</param>
        /// <param name="resultStartIndex">Start index of the index results to return.</param>
        /// <param name="resultCount">The number of results to return.</param>
        /// <param name="property">The default search property. May be null.</param>
        /// <returns></returns>
        public List<string> FindItems(string domain, string queryText, int resultStartIndex,
                                      int resultCount, string property)
        {
            Arg.CheckNullOrEmpty("domain", domain);
            Arg.CheckNullOrEmpty("queryText", queryText);
            Arg.CheckInRange("resultStartIndex", resultStartIndex, 0, int.MaxValue);
            Arg.CheckInRange("resultCount", resultCount, 1, int.MaxValue);
            State.CheckTrue(IsDisposed, "Indexer has been disposed");

            // Lucene does not allow null default property
            property = property ?? "";

            // getting the writer ensures the index exists before we attempt to search
            GetWriter(domain, false);

            var queryParser = new QueryParser(property, new StandardAnalyzer());
            Query query = queryParser.Parse(queryText);
            try
            {
                IndexSearcher searcher = GetSearcher(domain, false);
                int luceneResultCount = resultStartIndex + resultCount;
                TopDocs docs = searcher.Search(query, luceneResultCount);

                Log.Debug(m => m("Searched domain index '{0}' with query '{1}' and found {2} items.", domain, queryText, docs.totalHits));

                return BuildIdList(searcher, docs, resultStartIndex, resultCount);
            }
            catch
            {
                // force a refresh of the cached searcher if the search fails with an exception
                GetSearcher(domain, true);
                throw;
            }
        }

        /// <summary>
        /// Gets the index path for the specified domain.
        /// </summary>
        /// <param name="domain">The domain.</param>
        /// <returns></returns>
        public string GetIndexPath(string domain)
        {
            string domainPath = Path.Combine(IndexRootPath, domain + "/");
            return domainPath;
        }

        private void IndexItem(IndexWriter writer, IndexValues item)
        {
            // remove previous entry for this item
            var idTerm = new Term("id", item.Id);
            writer.DeleteDocuments(idTerm);

            // now add new item entries
            var doc = new Document();

            var idField =
                new Field("id", item.Id, Field.Store.YES, Field.Index.NOT_ANALYZED);

            doc.Add(idField);

            foreach (string property in item)
            {
                string value = item[property];
                if (value == null)
                {
                    continue;
                }
                var propertyField = new Field(property, value, Field.Store.NO,
                                              Field.Index.ANALYZED);
                doc.Add(propertyField);
            }
            writer.AddDocument(doc);
        }

        private List<string> BuildIdList(IndexSearcher searcher, TopDocs docs, int resultStartIndex,
                                         int resultCount)
        {
            var ids = new List<string>();
            int maxResultIndex = Math.Min(docs.scoreDocs.Length, resultStartIndex + resultCount);
            for (int k = resultStartIndex; k < maxResultIndex; k++)
            {
                Document doc = searcher.Doc(docs.scoreDocs[k].doc);
                string id = doc.Get("id");
                ids.Add(id);
            }

            return ids;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="LuceneIndexer"/> is reclaimed by garbage collection.
        /// </summary>
        ~LuceneIndexer()
        {
            Dispose();
        }

        private IndexSearcher GetSearcher(string domain, bool createAlways)
        {
            IndexSearcher searcher;
            lock (((ICollection) searchers).SyncRoot)
            {
                searchers.TryGetValue(domain, out searcher);
                if (createAlways)
                {
                    Close(searcher);
                    searcher = null;
                }
                if (searcher != null && !searcher.Reader.IsCurrent())
                {
                    // if searcher is stale take it out of the rotation. don't close
                    // it right away because it may still be in use by another thread.
                    // leave cleanup to the garbage collector
                    searcher = null;
                    searchers.Remove(domain);
                }
                if (searcher == null)
                {
                    FSDirectory directory = FSDirectory.GetDirectory(GetIndexPath(domain));
                    searcher = new IndexSearcher(directory);
                    searchers[domain] = searcher;
                }
            }

            return searcher;
        }

        private IndexWriter GetWriter(string domain, bool createAlways)
        {
            IndexWriter writer;
            lock (((ICollection) writers).SyncRoot)
            {
                writers.TryGetValue(domain, out writer);
                if (createAlways)
                {
                    Close(writer);
                    writer = null;
                }
                if (writer == null)
                {
                    FSDirectory directory = FSDirectory.GetDirectory(GetIndexPath(domain));
                    writer = new IndexWriter(directory, new StandardAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);

                    writers[domain] = writer;
                }
            }

            return writer;
        }

        private static void Close(IndexSearcher searcher)
        {
            if (searcher != null)
            {
                searcher.Close();
            }
        }

        private static void Close(IndexWriter writer)
        {
            if (writer != null)
            {
                writer.Close();
            }
        }
    }
}