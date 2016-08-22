/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;

namespace Simol.Cache
{
    /// <summary>
    /// Contract for Simol item caches.
    /// </summary>
    /// <seealso cref="SimpleCache"/>
    /// <remarks>
    /// Implementations must be safe for use by multiple threads.
    /// </remarks>
    public interface IItemCache
    {
        /// <summary>
        /// Sets the value at the specified cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">If the key is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the key is empty</exception>
        void Put(string key, object value);

        /// <summary>
        /// Gets the value at the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>The requested value or null</returns>
        /// <exception cref="ArgumentNullException">If the key is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the key is empty</exception>
        object Get(string key);

        /// <summary>
        /// Removes the value at the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>true if the value was found and removed; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">If the key is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the key is empty</exception>
        bool Remove(string key);

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> at the specified key.
        /// </summary>
        /// <value></value>
        /// <exception cref="ArgumentNullException">If the key is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the key is empty</exception>
        object this[string key] { get; set; }

        /// <summary>
        /// Flushes all items from the cache.
        /// </summary>
        void Flush();
    }
}