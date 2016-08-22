/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;

namespace Simol.Indexing
{
    /// <summary>
    /// Defines the contract between Simol and external full-text indexing engines.
    /// </summary>
    /// <seealso cref="LuceneIndexer"/>
    /// <seealso cref="SimolConfig.Indexer"/>
    /// <remarks>
    /// This is not intended to be a full-featured contract for full-text indexing. It
    /// defines only the minimal operations necessary for Simol to perform basic indexing and searching
    /// functions. Users are encourage to understand and directly manipulate the installed
    /// indexer as necessary.
    /// </remarks>
    public interface IIndexer : IDisposable
    {
        /// <summary>
        /// Gets or sets the index root path.
        /// </summary>
        /// <value>The index root path.</value>
        string IndexRootPath { get; set; }

        /// <summary>
        /// Indexes a list of item values and stores them in an index dedicated to the 
        /// specified domain.
        /// </summary>
        /// <param name="domain">The domain being indexed.</param>
        /// <param name="items">The items to index.</param>
        void IndexItems(string domain, List<IndexValues> items);

        /// <summary>
        /// Finds items containing the specified full-text query terms.
        /// </summary>
        /// <param name="domain">The domain index to query.</param>
        /// <param name="queryText">The query text.</param>
        /// <param name="resultStartIndex">Start index of the index results to return.</param>
        /// <param name="resultCount">The number of results to return.</param>
        /// <param name="property">The default search property. May be null.</param>
        /// <returns></returns>
        List<string> FindItems(string domain, string queryText, int resultStartIndex,
                               int resultCount, string property);

        /// <summary>
        /// Gets the index path for the specified domain.
        /// </summary>
        /// <param name="domain">The domain.</param>
        /// <returns></returns>
        string GetIndexPath(string domain);
    }
}