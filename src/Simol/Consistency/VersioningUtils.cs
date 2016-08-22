/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.SimpleDB.Model;
using Simol.Core;
using Simol.Formatters;

namespace Simol.Consistency
{
    /// <summary>
    /// Contains internal utility methods for applying versioning and consistency behavior.
    /// </summary>
    internal class VersioningUtils
    {
        /// <summary>
        /// Increments a version value (either int or DateTime) and returns the new version value.
        /// </summary>
        public static object IncrementVersion(AttributeMapping versionMapping, object oldVersion)
        {
            object newVersion = null;
            if (versionMapping.FormatType == typeof (DateTime))
            {
                newVersion = DateTime.UtcNow;
            }
            else if (versionMapping.FormatType == typeof (int))
            {
                var intVersion = (int) (oldVersion ?? 0);
                newVersion = intVersion + 1;
            }
            return newVersion;
        }

        /// <summary>
        /// Gets the version attribute mapping from an item mapping.
        /// </summary>
        public static AttributeMapping GetVersionMapping(ItemMapping mapping)
        {
            AttributeMapping versionMapping = mapping.AttributeMappings.Where(a => a.IsVersionProperty).FirstOrDefault();
            return versionMapping;
        }

        /// <summary>
        /// Applies the currently configured versioning behavior to a put request.
        /// </summary>
        public static void ApplyVersioningBehavior(PropertyFormatter formatter, ItemMapping mapping,
                                                   PropertyValues values, List<ReplaceableAttribute> attributes,
                                                   PutAttributesRequest request)
        {
            AttributeMapping versionMapping = GetVersionMapping(mapping);
            if (versionMapping == null ||
                versionMapping.Versioning == VersioningBehavior.None)
            {
                return;
            }

            // if version attribute is not being updated do nothing
            ReplaceableAttribute versionAttribute =
                attributes.Where(a => a.Name == versionMapping.AttributeName).FirstOrDefault();
            if (versionAttribute == null)
            {
                return;
            }

            // get old version and increment
            object oldVersion = values[versionMapping.PropertyName];
            object newVersion = IncrementVersion(versionMapping, oldVersion);

            // update the outgoing attribute with the new version
            versionAttribute.Value = MappingUtils.PropertyValueToString(formatter, versionMapping, newVersion);

            // update values collection with new version so will be current when placed in cache
            values[versionMapping.PropertyName] = newVersion;

            // add an update condition ONLY for PutAttributesRequest
            if (request != null && versionMapping.Versioning == VersioningBehavior.AutoIncrementAndConditionallyUpdate)
            {
                // if the old version indicates we're inserting a new object create an "exists" condition
                if (oldVersion == null || Equals(oldVersion, 0) || Equals(oldVersion, DateTime.MinValue))
                {
                    request.Expected = new UpdateCondition
                        {
                            Exists = false,
                            Name = versionMapping.AttributeName
                        };
                }
                else
                {
                    string versionString = MappingUtils.PropertyValueToString(formatter, versionMapping, oldVersion);
                    request.Expected = new UpdateCondition
                        {
                            Name = versionMapping.AttributeName,
                            Value = versionString
                        };
                }
            }
        }
    }
}