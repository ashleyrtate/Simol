/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System.Linq;
using Simol.Cache;
using System.Collections.Generic;

namespace Simol.Core
{
    /// <summary>
    /// Decorating simol implementation that transparently caches items on puts, gets, selects, and deletes to 
    /// mitigate the negative impact of the eventual-consistency model of SimpleDB on applications.
    /// </summary>
    internal class CachingSimol : DecoratingSimol
    {
        private readonly IItemCache cache;
        private readonly object deleteMarker = new object();
        private readonly CacheUtils cacheUtils = new CacheUtils();

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingSimol"/> class.
        /// </summary>
        /// <param name="decoratedSimol">The decorated simol.</param>
        public CachingSimol(ISimolInternal decoratedSimol)
            : base(decoratedSimol)
        {
            cache = Config.Cache;
        }

        public override void PutAttributes(ItemMapping mapping, List<PropertyValues> values)
        {
            base.PutAttributes(mapping, values);

            // add to the cache AFTER successful add to SimpleDB
            foreach (PropertyValues item in values)
            {
                string key = cacheUtils.CreateKey(Config.Formatter, mapping, item.ItemName);
                cache[key] = item;
            }
        }

        public override void DeleteAttributes(ItemMapping mapping, List<object> itemNames, List<string> propertyNames)
        {
            base.DeleteAttributes(mapping, itemNames, propertyNames);

            propertyNames = propertyNames ?? ListUtils.EmptyStringList;

            foreach (object itemName in itemNames)
            {
                // flush items from cache AFTER successful delete on SimpleDB
                string key = cacheUtils.CreateKey(Config.Formatter, mapping, itemName);

                if (propertyNames.Count == 0)
                {
                    // mark as deleted if we're SURE all attributes are being deleted
                    cache[key] = deleteMarker;
                }
                else
                {
                    cache.Remove(key);
                }
            }
        }

        public override SelectResults<PropertyValues> SelectAttributes(SelectCommand command)
        {
            SelectResults<PropertyValues> results = base.SelectAttributes(command);

            foreach (PropertyValues loadedValues in results)
            {
                string key = cacheUtils.CreateKey(Config.Formatter, command.Mapping, loadedValues.ItemName);
                object cachedValues = cache[key];

                // don't add item back to cache if was previously deleted
                if (cachedValues == deleteMarker)
                {
                    continue;
                }

                if (cachedValues == null)
                {
                    cache[key] = loadedValues;
                }
                else
                {
                    PropertyValues.Copy(loadedValues, (PropertyValues) cachedValues);
                }
            }

            return results;
        }

        public override PropertyValues GetAttributes(ItemMapping mapping, object itemName, List<string> propertyNames)
        {
            propertyNames = propertyNames ?? ListUtils.EmptyStringList;

            string key = cacheUtils.CreateKey(Config.Formatter, mapping, itemName);
            object cachedValues = cache[key];

            if (cachedValues == deleteMarker)
            {
                return null;
            }

            if (!PropertiesAreCached((PropertyValues) cachedValues, mapping, propertyNames))
            {
                PropertyValues loadedValues = base.GetAttributes(mapping, itemName, propertyNames);
                if (cachedValues == null)
                {
                    cache[key] = loadedValues;
                    cachedValues = loadedValues;
                }
                else if (loadedValues != null)
                {
                    PropertyValues.Copy(loadedValues, (PropertyValues) cachedValues);
                }
            }

            return (PropertyValues) cachedValues;
        }

        /// <summary>
        /// Checks if all requested properties are in the list of cached values.
        /// </summary>
        private bool PropertiesAreCached(PropertyValues cachedValues, ItemMapping mapping, List<string> requestedProperties)
        {
            if (cachedValues == null)
            {
                return false;
            }
            if (requestedProperties.Count > 0)
            {
                foreach (string property in requestedProperties)
                {
                    if (!cachedValues.ContainsProperty(property))
                    {
                        return false;
                    }
                }
                return true;
            }
            foreach (string property in mapping.AttributeMappings.Select(a => a.PropertyName))
            {
                if (!cachedValues.ContainsProperty(property))
                {
                    return false;
                }
            }
            return true;
        }
    }
}