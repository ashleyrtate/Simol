/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using Coditate.Common.Util;

namespace Simol.Core
{
    /// <summary>
    /// Describes attribute mappings for use with <see cref="PropertyValues"/>.
    /// </summary>
    internal class ValuesAttributeMapping : AttributeMapping
    {
        public override string FullPropertyName
        {
            get { return "PropertyValues[" + PropertyName + "]"; }
        }

        /// <summary>
        /// Creates a new mapping from an existing mapping.
        /// </summary>
        /// <param name="mapping">The new mapping.</param>
        /// <returns></returns>
        public static ValuesAttributeMapping CreateInternal(AttributeMapping mapping)
        {
            Arg.CheckNull("mapping", mapping);

            var newMapping = new ValuesAttributeMapping
                {
                    AttributeName = mapping.AttributeName,
                    Formatter = mapping.Formatter,
                    PropertyName = mapping.PropertyName,
                    PropertyType = mapping.PropertyType,
                    SpanAttributes = mapping.SpanAttributes,
                    IsIndexed = mapping.IsIndexed,
                    Versioning = mapping.Versioning
                };
            return newMapping;
        }
    }
}