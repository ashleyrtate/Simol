/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System.Collections;
using System.Collections.Generic;

namespace Simol.Indexing
{
    /// <summary>
    /// A structure that holds stringified attribute values for full-text indexing.
    /// </summary>
    /// <remarks>
    /// Multi-valued attributes are flattened into a space-delimited list when stored 
    /// in this class.
    /// </remarks>
    /// <seealso cref="IIndexer"/>
    public class IndexValues : IEnumerable<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexValues"/> class.
        /// </summary>
        /// <param name="id">The item id.</param>
        public IndexValues(string id)
        {
            Id = id;
            Values = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The name of the item.</value>
        public string Id { get; private set; }

        /// <summary>
        /// Gets or sets the item values with the specified property name.
        /// </summary>
        /// <value></value>
        public string this[string propertyName]
        {
            get
            {
                string pValue;
                Values.TryGetValue(propertyName, out pValue);
                return pValue;
            }
            set { Values[propertyName] = value; }
        }

        /// <summary>
        /// Gets or sets the item values.
        /// </summary>
        /// <value>The values.</value>
        public Dictionary<string, string> Values { get; set; }

        /// <summary>
        /// Returns an enumerator that iterates through the property names.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator()
        {
            return Values.Keys.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the property names.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the property names.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}