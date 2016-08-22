/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System.Text;
using Coditate.Common.Util;
using Simol.Core;
using Simol.Formatters;

namespace Simol.Cache
{
    /// <summary>
    /// Utility methods for working with the Simol item cache.
    /// </summary>
    public class CacheUtils
    {
        /// <summary>
        /// Creates a cache key to use when adding or removing individual items from the <see cref="IItemCache"/>.
        /// </summary>
        /// <param name="formatter">The formatter to use when formatting the item name.</param>
        /// <param name="mapping">The item mapping.</param>
        /// <param name="itemName">The item name.</param>
        /// <returns></returns>
        public string CreateKey(PropertyFormatter formatter, ItemMapping mapping, object itemName)
        {
            Arg.CheckNull("formatter", formatter);
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("itemName", itemName);
            
            string itemNameString = MappingUtils.ItemNameToString(formatter, mapping.ItemNameMapping, itemName);

            var key = new StringBuilder(mapping.DomainName);
            key.Append('+');
            key.Append(itemNameString);
            return key.ToString();
        }
    }
}