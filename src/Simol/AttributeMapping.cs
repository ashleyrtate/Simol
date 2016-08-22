/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using Coditate.Common.Util;
using Simol.Core;
using Simol.Formatters;
using Simol.Indexing;

namespace Simol
{
    /// <summary>
    /// Determines whether a scalar property value should be allowed to span multiple attributes in SimpleDB.
    /// </summary>
    [Flags]
    public enum SpanType
    {
        /// <summary>
        /// The property should be stored in a single attribute value.
        /// </summary>
        None = 0,
        /// <summary>
        /// The property value should be split and stored across multiple attributes if necessary.
        /// </summary>
        Span = 1,
        /// <summary>
        /// The property value should be compressed and stored across multiple attributes.
        /// </summary>
        Compress = 2,
        /// <summary>
        /// The property value should be encrypted and stored across multiple attributes.
        /// </summary>
        Encrypt = 4
    }
    
    /// <summary>
    /// Defines an ad-hoc mapping between a single property and SimpleDB attribute.
    /// </summary>
    /// <remarks>
    /// See <see cref="ItemMapping"/> for more usage details.
    /// </remarks>
    /// <seealso cref="ItemMapping"/>
    public abstract class AttributeMapping
    {
        private string attributeName;
        private Type scalarType;

        /// <summary>
        /// Gets the name of the mapped property.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets the full name of the property, including contextual information.
        /// </summary>
        /// <value>The full name of the property.</value>
        public abstract string FullPropertyName { get; }

        /// <summary>
        /// Gets the mapped property type.
        /// </summary>
        /// <value>The type of the property.</value>
        /// <remarks>
        /// Holds the <em>actual</em> property type. In other words, if the property 
        /// is a nullable type, this will contain <see cref="System.Nullable{T}"/>, not <see cref="String"/>,
        /// <see cref="Int32"/>, etc. Likewise, if the mapped property is defined as a generic collection such 
        /// as <see cref="List{T}"/>, the list type will be returned, not the type contained by the list.
        /// </remarks>
        public Type PropertyType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether content should
        /// span multiple SimpleDB attributes if it cannot fit into a single attribute.
        /// </summary>
        /// <value>The span setting.</value>
        public SpanType SpanAttributes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the property should be indexed 
        /// by the installed <see cref="IIndexer"/>.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the property is indexed; otherwise, <c>false</c>.
        /// </value>
        /// <seealso cref="SimolConfig.Indexer"/>
        public bool IsIndexed { get; set; }

        /// <summary>
        /// Gets a value indicating whether this property tracks the item version.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this property tracks the version; otherwise, <c>false</c>.
        /// </value>
        public bool IsVersionProperty { 
            get {
                return Versioning != null;
            }
        }

        /// <summary>
        /// Gets the scalar property type.
        /// </summary>
        /// <value>The property list type.</value>
        /// <remarks>
        /// Holds the same value as <see cref="PropertyType"/> except when the property is a list type
        /// such as <see cref="List{T}"/>. For list properties the generic type <c>T</c> is returned (i.e. the 
        /// scalar type contained by the list).
        /// </remarks>
        public Type ScalarType
        {
            get
            {
                if (scalarType == null)
                {
                    scalarType = TypeItemMapping.GetScalarType(PropertyType);
                }
                return scalarType;
            }
        }

        /// <summary>
        /// Gets the property type to use for formatting purposes.
        /// </summary>
        /// <value></value>
        /// <remarks>
        /// In most cases this holds the same type as <see cref="PropertyType"/>.
        /// However, this property also returns the underlying or contained type
        /// for <see cref="Nullable{T}"/> and <see cref="List{T}"/>
        /// properties. This is the type passed to <see cref="PropertyFormatter"/>
        /// when the property value to/from a string.
        /// </remarks>
        /// <returns></returns>
        public Type FormatType
        {
            get
            {
                Type formatType = ScalarType;
                Type nullableType = Nullable.GetUnderlyingType(ScalarType);
                if (nullableType != null)
                {
                    formatType = nullableType;
                }

                return formatType;
            }
        }

        /// <summary>
        /// Gets or sets the name of the SimpleDB attribute used to store
        /// the described property.
        /// </summary>
        /// <value>The name of the attribute.</value>
        /// <remarks>
        /// By default this is the same as <see cref="PropertyName"/>. A different 
        /// value need only be provided when the property and SimpleDB
        /// attribute names are different.
        /// </remarks>
        public string AttributeName
        {
            get
            {
                if (attributeName == null)
                {
                    return PropertyName;
                }
                return attributeName;
            }
            set { attributeName = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the described property is a list.
        /// </summary>
        /// <value><c>true</c> if the property is a list; otherwise, <c>false</c>.</value>
        public bool IsList
        {
            get { return ScalarType != PropertyType; }
        }

        /// <summary>
        /// Gets a value indicating whether the property type is formattable.
        /// </summary>
        /// <value><c>true</c> if the type returned by <see cref="FormatType"/> implements <see cref="IFormattable"/>; 
        /// otherwise, <c>false</c>.</value>
        public bool IsFormattable
        {
            get { return (typeof (IFormattable).IsAssignableFrom(FormatType)); }
        }

        /// <summary>
        /// Gets a value indicating whether the property type is numeric.
        /// </summary>
        /// <value><c>true</c> if the type returned by <see cref="FormatType"/> is numeric; 
        /// otherwise, <c>false</c>.</value>
        public bool IsNumeric
        {
            get
            {
                var formatter =
                    PropertyFormatter.GetDefaultFormatter(FormatType) as NumberFormatter;
                return formatter != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the property type is a signed numeric type.
        /// </summary>
        /// <value><c>true</c> if type returned by <see cref="FormatType"/> is a signed numeric type; 
        /// otherwise, <c>false</c>.</value>
        public bool IsSigned
        {
            get
            {
                var formatter =
                    PropertyFormatter.GetDefaultFormatter(FormatType) as NumberFormatter;
                return formatter != null && formatter.IsSigned;
            }
        }

        /// <summary>
        /// Gets or sets the custom formatter to use with this property if one has been provided.
        /// </summary>
        /// <value>The formatter.</value>
        /// <remarks>
        /// If this value is null, the property value will be converted by the default
        /// formatter registered with the <see cref="PropertyFormatter"/>.
        /// </remarks>
        public ITypeFormatter Formatter { get; set; }

        /// <summary>
        /// Gets or sets the versioning information.
        /// </summary>
        /// <value>The versioning.</value>
        /// <remarks>
        /// If this value is null, the property is not a "version" property
        /// and <see cref="IsVersionProperty"/> will return <c>false</c>.
        /// </remarks>
        public VersioningBehavior? Versioning { get; set; }

        /// <summary>
        /// Creates an attribute mapping for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <returns></returns>
        /// <remarks>
        /// By default this method creates a mapping that assumes the property and
        /// SimpleDB attribute names are the same. If this is not the case you 
        /// must set <see cref="AttributeName"/> to the desired 
        /// value on the returned mapping.
        /// </remarks>
        public static AttributeMapping Create(string propertyName, Type propertyType)
        {
            Arg.CheckNullOrEmpty("propertyName", propertyName);
            Arg.CheckNull("propertyType", propertyType);

            var mapping = new ValuesAttributeMapping
                {
                    PropertyName = propertyName,
                    PropertyType = propertyType
                };
            return mapping;
        }
    }
}