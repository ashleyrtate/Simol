/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Coditate.Common.Util;

namespace Simol.Core
{
    /// <summary>
    /// Item mapping implementation supporting partial property sets or arbitrary lists of properties.
    /// </summary>
    internal class ValuesItemMapping : ItemMapping
    {
        /// <summary>
        /// Creates a mapping for the specified item type and list of properties.
        /// </summary>
        /// <param name="itemType">Type of the item.</param>
        /// <param name="propertyNames">The property names.</param>
        /// <returns></returns>
        public static ValuesItemMapping CreateInternal(Type itemType, List<string> propertyNames)
        {
            Arg.CheckNull("itemType", itemType);
            Arg.CheckNull("propertyNames", propertyNames);

            TypeItemMapping typeMapping = TypeItemMapping.GetMapping(itemType);
            foreach (string property in propertyNames)
            {
                if (typeMapping[property] == null)
                {
                    string message = string.Format("Item type '{0}' has no property named '{1}' mapped to SimpleDB.",
                                                   itemType.FullName, property);
                    throw new SimolDataException(message);
                }
            }

            return CreateInternal(typeMapping, propertyNames);
        }

        /// <summary>
        /// Creates a new values mapping from the specified item mapping.
        /// </summary>
        /// <param name="itemMapping">The item mapping.</param>
        /// <param name="propertyNames">The property names.</param>
        /// <param name="createAlways">if set to <c>true</c> always create a new mapping; otherwise return 
        /// the existing mapping if it is the correct type.</param>
        /// <returns></returns>
        public static ValuesItemMapping CreateInternal(ItemMapping itemMapping, List<string> propertyNames,
                                                       bool createAlways)
        {
            Arg.CheckNull("itemMapping", itemMapping);
            Arg.CheckNull("propertyNames", propertyNames);

            var valuesMapping = itemMapping as ValuesItemMapping;
            if (valuesMapping == null || createAlways)
            {
                valuesMapping = CreateInternal(itemMapping, propertyNames);
            }

            return valuesMapping;
        }

        private static ValuesItemMapping CreateInternal(ItemMapping itemMapping, List<string> propertyNames)
        {
            var valuesMapping = new ValuesItemMapping
                {
                    DomainName = itemMapping.DomainName,
                    ItemNameMapping = ValuesAttributeMapping.CreateInternal(itemMapping.ItemNameMapping),
                    Constraint = itemMapping.Constraint
                };

            List<string> includedProperties = itemMapping.AttributeMappings.Select(p => p.PropertyName).ToList();
            if (propertyNames.Count > 0)
            {
                includedProperties =
                    itemMapping.AttributeMappings.Select(p => p.PropertyName).Intersect(propertyNames).ToList();
            }

            foreach (string property in includedProperties)
            {
                AttributeMapping attMapping = itemMapping[property];
                ValuesAttributeMapping valuesAttMapping = ValuesAttributeMapping.CreateInternal(attMapping);
                valuesMapping.AttributeMappings.Add(valuesAttMapping);
            }

            return valuesMapping;
        }
    }
}