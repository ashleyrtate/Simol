/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using Simol.Indexing;

namespace Simol
{
    /// <summary>
    /// Supports full-text indexing of property values stored in SimpleDB.
    /// </summary>
    /// <remarks>
    /// Mark string properties with this attribute to index and search them using 
    /// the installed full-text indexing engine.
    /// </remarks>
    /// <seealso cref="IIndexer"/>
    /// <seealso cref="SimolConfig.Indexer"/>
    /// <seealso cref="IndexBuilder"/>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IndexAttribute : SimolAttribute
    {
    }
}