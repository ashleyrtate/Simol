/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System.Collections;
using System.Collections.Generic;

namespace Simol
{
    /// <summary>
    /// Holds the results of a call to <see cref="SimolClient.Select{T}(SelectCommand{T})"/> and
    /// other select methods.
    /// </summary>
    /// <typeparam name="T">Type of the item returned in the results</typeparam>
    public class SelectResults<T> : IEnumerable<T>
    {
        private readonly List<T> items = new List<T>();

        /// <summary>
        /// Gets or sets the pagination token.
        /// </summary>
        /// <value>The pagination token.</value>
        /// <remarks>
        /// Pass this token on the next select request to get the next page of
        /// results. 
        /// </remarks>
        /// <seealso cref="SelectCommand.PaginationToken"/>
        public string PaginationToken { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether the select command was cancelled.
        /// </summary>
        /// <remarks>
        /// This property will only be true if the command was cancelled at a meaningful
        /// point in the select operation. In other words, if a cancellation
        /// has no impact on the returned results this property will be false.
        /// </remarks>
        /// <value><c>true</c> if the select command was cancelled; otherwise, <c>false</c>.</value>
        public bool WasCommandCancelled { get; internal set; }

        /// <summary>
        /// Gets the count of items returned.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get { return Items.Count; }
        }

        /// <summary>
        /// Gets the list of item returned.
        /// </summary>
        /// <value>The result objects.</value>
        public List<T> Items
        {
            get { return items; }
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <value></value>
        public T this[int index]
        {
            get { return Items[index]; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}