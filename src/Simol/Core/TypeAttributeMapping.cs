/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Reflection;
using Coditate.Common.Util;

namespace Simol.Core
{
    /// <summary>
    /// Holds mapping metadata about persistent item properties used with Simol.
    /// </summary>
    internal class TypeAttributeMapping : AttributeMapping
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeAttributeMapping"/> class.
        /// </summary>
        /// <param name="property">The property.</param>
        public TypeAttributeMapping(PropertyInfo property)
        {
            Arg.CheckNull("property", property);

            Property = property;
            PropertyName = Property.Name;
            PropertyType = Property.PropertyType;
        }

        public PropertyInfo Property { get; private set; }

        public override string FullPropertyName
        {
            get { return Property.DeclaringType.FullName + "." + Property.Name; }
        }
    }
}