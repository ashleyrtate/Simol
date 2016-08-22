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
using Simol.Consistency;
using Simol.Core;

namespace Simol
{
    /// <summary>
    /// Defines an ad-hoc item mapping between a list of properties and SimpleDB attributes.
    /// </summary>
    /// <remarks>
    /// You won't normally deal directly with item mappings when using Simol methods typed
    /// with a generic parameter such as <see cref="SimolClient.Get{T}"/>. The mapping
    /// classes (<c>ItemMapping</c> and <see cref="AttributeMapping"/>) are provided primarily 
    /// for tools and infrastructure classes that require completely dynamic interactions with Simol.
    /// </remarks>
    public abstract class ItemMapping
    {
        private IDomainConstraint constraint;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemMapping"/> class.
        /// </summary>
        protected ItemMapping()
        {
            AttributeMappings = new List<AttributeMapping>();
        }

        /// <summary>
        /// Gets or sets the name of the SimpleDB domain used to store properties.
        /// </summary>
        /// <value>The name of the domain.</value>
        public string DomainName { get; set; }

        /// <summary>
        /// Gets or sets the mapping for the item name property.
        /// </summary>
        /// <value>The item name mapping.</value>
        public AttributeMapping ItemNameMapping { get; set; }

        /// <summary>
        /// Gets or sets the mappings for persistent properties.
        /// </summary>
        /// <value>The property mappings.</value>
        public List<AttributeMapping> AttributeMappings { get; private set; }

        /// <summary>
        /// Gets or sets the custom constraint to apply when loading or saving data
        /// to the mapped domain.
        /// </summary>
        /// <value>The custom constraint.</value>
        /// <remarks>
        /// This property always returns an non-null <see cref="IDomainConstraint"/>. 
        /// If no user constraint was installed, then a no-op constraint is returned.
        /// </remarks>
        public IDomainConstraint Constraint
        {
            get
            {
                if (constraint == null)
                {
                    return NoOpConstraint.Default;
                }
                return constraint;
            }
            set { constraint = value; }
        }

        /// <summary>
        /// Gets the <see cref="AttributeMapping"/> with the specified property name.
        /// </summary>
        /// <value></value>
        public AttributeMapping this[string propertyName]
        {
            get { return AttributeMappings.Where(m => m.PropertyName == propertyName).FirstOrDefault(); }
        }

        /// <summary>
        /// Creates a complete mapping for the specified item type.
        /// </summary>
        /// <param name="itemType">The item type</param>
        /// <returns></returns>
        /// <remarks>
        /// This method build a mapping containing the same properties
        /// included when you invoke <see cref="SimolClient"/> methods that accept generic 
        /// item type parameters, such as <see cref="SimolClient.Get{T}"/>, 
        /// <see cref="SimolClient.Delete{T}(object)"/>, etc.
        /// </remarks>
        public static ItemMapping Create(Type itemType)
        {
            Arg.CheckNull("itemType", itemType);

            return ValuesItemMapping.CreateInternal(itemType, new List<string>());
        }

        /// <summary>
        /// Creates a partial mapping from the specified item type.
        /// </summary>
        /// <param name="itemType">Type of the item.</param>
        /// <param name="propertyNames">The properties to include in the mapping.</param>
        /// <returns></returns>
        /// <remarks>
        /// Only the specified property names are included in the mapping. However,
        /// if you pass an empty list of property names <em>all</em> mapped 
        /// properties will be added to the mapping (same behavior as <see cref="Create(Type)"/>).
        /// </remarks>
        public static ItemMapping Create(Type itemType, List<string> propertyNames)
        {
            Arg.CheckNull("itemType", itemType);
            Arg.CheckNull("propertyNames", propertyNames);

            return ValuesItemMapping.CreateInternal(itemType, propertyNames);
        }

        /// <summary>
        /// Creates a minimal, ad-hoc mapping not associated with any .NET type.
        /// </summary>
        /// <param name="domainName">Domain where property values will be stored.</param>
        /// <param name="itemNameMapping">The item name mapping.</param>
        /// <returns></returns>
        /// <remarks>
        /// Mappings created with this method include <em>no mapped properties</em>. Desired property
        /// mappings must be added explicitly like this:
        /// <code>
        ///     AttributeMapping itemNameMapping = AttributeMapping.Create("Id", typeof(Guid));
        ///     ItemMapping mapping = ItemMapping.Create("Person", itemNameMapping);
        ///     AttributeMapping nameMapping = AttributeMapping.Create("Name", typeof(string));
        ///     mapping.AttributeMappings.Add(nameMapping);
        /// 
        ///     Guid personId;
        ///     PropertyValues values = simol.GetAttributes(mapping, personId);
        /// </code>
        /// </remarks>
        public static ItemMapping Create(string domainName, AttributeMapping itemNameMapping)
        {
            Arg.CheckNullOrEmpty("domainName", domainName);
            Arg.CheckNull("itemNameMapping", itemNameMapping);

            var itemMapping = new ValuesItemMapping
                {
                    DomainName = domainName,
                    ItemNameMapping = itemNameMapping
                };
            return itemMapping;
        }
    }
}