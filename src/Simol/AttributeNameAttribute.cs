/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using Coditate.Common.Util;

namespace Simol
{
    /// <summary>
    /// Supports customization of the SimpleDB attribute name used to store an item property.
    /// </summary>
    /// <remarks>SimpleDB attribute names default to the property name of the item being stored.
    /// Use this attribute on item properties to override the default SimpleDB attribute name.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AttributeNameAttribute : SimolAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeNameAttribute"/> class.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="attributeName"/> is an empty string</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="attributeName"/> is null</exception>
        public AttributeNameAttribute(string attributeName)
        {
            Arg.CheckNullOrEmpty("attributeName", attributeName);

            AttributeName = attributeName;
        }

        /// <summary>
        /// Gets or sets the name of the attribute.
        /// </summary>
        /// <value>The name of the attribute.</value>
        public string AttributeName { get; private set; }
    }
}