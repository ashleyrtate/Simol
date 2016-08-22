/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections;
using System.Collections.Generic;
using Coditate.Common.Util;
using Simol.Core;
using System.Linq;

namespace Simol
{
    /// <summary>
    /// Holds an ad-hoc collection of values that may represent the complete set or a subset of the 
    /// attributes stored in SimpleDB for a single item.
    /// </summary>
    public class PropertyValues : IEnumerable<string>
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyValues"/> class.
        /// </summary>
        /// <param name="itemName">The item name.</param>
        public PropertyValues(object itemName)
        {
            Arg.CheckNull("itemName", itemName);

            ItemName = itemName;
            IsCompleteSet = false;
        }

        /// <summary>
        /// Gets or sets the SimpleDB item name associated with this list of property values.
        /// </summary>
        /// <value>The item name.</value>
        public object ItemName { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance contains all mapped
        /// properties stored for the item.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is a complete set; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompleteSet { get; internal set; }

        /// <summary>
        /// Gets or sets the value with the specified property name.
        /// </summary>
        /// <value></value>
        /// <remarks>
        /// Attempts to read a non-existent property value <em>do not</em> result in an exception
        /// but simply return null. Use <see cref="ContainsProperty"/> to test
        /// for property existence.
        /// </remarks>
        /// <returns>
        /// </returns>
        /// <exception cref="ArgumentNullException">If propertyName is null</exception>
        public object this[string propertyName]
        {
            get
            {
                object value;
                values.TryGetValue(propertyName, out value);
                return value;
            }
            set { values[propertyName] = value; }
        }

        /// <summary>
        /// Determines whether the specified property exists in the values collection.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>
        /// 	<c>true</c> if the specified property name is found; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsProperty(string propertyName)
        {
            return values.ContainsKey(propertyName);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the property names.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the property names.
        /// </returns>
        public IEnumerator<string> GetEnumerator()
        {
            return values.Keys.GetEnumerator();
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

        /// <summary>
        /// Gets the property count.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get
            {
                return values.Count;
            }
        }

        /// <summary>
        /// Creates a values collection from the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        /// <remarks>
        /// The returned values collection will contain entries for all 
        /// mapped properties (including null-value properties).
        /// </remarks>
        public static PropertyValues CreateValues(object item)
        {
            Arg.CheckNull("item", item);

            TypeItemMapping mapping = TypeItemMapping.GetMapping(item.GetType());
            PropertyValues values = CreateValues(mapping, item);
            values.IsCompleteSet = true;
            return values;
        }

        /// <summary>
        /// Creates a values collection from the specified item containing only the specified properties.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="propertyNames">The property names.</param>
        /// <returns></returns>
        /// <remarks>
        /// The returned values collection will contain entries only the specified properties
        /// (including null-value properties).
        /// </remarks>
        public static PropertyValues CreateValues(object item, params string[] propertyNames)
        {
            Arg.CheckNull("item", item);
            Arg.CheckNull("propertyNames", propertyNames);

            ItemMapping mapping = ItemMapping.Create(item.GetType(),
                                                     propertyNames.ToList());
            return CreateValues(mapping, item);
        }

        /// <summary>
        /// Creates a values collection from the specified item using a dynamic mapping.
        /// </summary>
        /// <param name="mapping">The item mapping.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        /// <remarks>
        /// The returned values collection will contain entries for all 
        /// mapped properties (including null-value properties).
        /// </remarks>
        public static PropertyValues CreateValues(ItemMapping mapping, object item)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("item", item);

            return MappingUtils.GetPropertyValues(mapping, item);
        }

        /// <summary>
        /// Creates a list of values collections from a list of items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mapping">The item mapping.</param>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        /// <remarks>
        /// The returned values collections will contain entries for all
        /// mapped properties (including null-value properties).
        /// </remarks>
        public static List<PropertyValues> CreateValues<T>(ItemMapping mapping, List<T> items)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("items", items);

            var allValues = new List<PropertyValues>();
            foreach (object item in items)
            {
                var values = CreateValues(mapping, item);
                allValues.Add(values);
            }
            return allValues;
        }

        /// <summary>
        /// Creates an item instance from the specified values collection.
        /// </summary>
        /// <param name="itemType">Type of the item to return.</param>
        /// <param name="values">The property values.</param>
        /// <returns>A new item or null if the value collection is null</returns>
        /// <remarks>
        /// Unmapped property values are silently discared. Property values 
        /// that <em>are</em> mapped but fail conversion will cause an exception.
        /// </remarks>
        public static object CreateItem(Type itemType, PropertyValues values)
        {
            Arg.CheckNull("itemType", itemType);

            TypeItemMapping mapping = TypeItemMapping.GetMapping(itemType);
            return CreateItem(mapping, itemType, values);
        }

        /// <summary>
        /// Creates an item instance from the specified values collection
        /// using a dynamic mapping.
        /// </summary>
        /// <param name="mapping">The item mapping.</param>
        /// <param name="itemType">Type of the item to return.</param>
        /// <param name="values">The property values.</param>
        /// <returns>A new item or null if the value collection is null</returns>
        /// <returns></returns>
        public static object CreateItem(ItemMapping mapping, Type itemType, PropertyValues values)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("itemType", itemType);

            object item = null;
            if (values != null)
            {
                item = MappingUtils.CreateInstance(itemType);
                MappingUtils.SetPropertyValues(mapping, values, item);
            }
            return item;
        }

        /// <summary>
        /// Copies all values from one collection to another, overwriting existing values in
        /// the destination collection.
        /// </summary>
        /// <param name="from">Values collection to copy from.</param>
        /// <param name="to">Values collection to copy to.</param>
        /// <remarks>
        /// The <see cref="ItemName"/> is <b>not</b> copied.
        /// </remarks>
        public static void Copy(PropertyValues from, PropertyValues to)
        {
            Arg.CheckNull("from", from);
            Arg.CheckNull("to", to);

            foreach (var key in from)
            {
                to[key] = from[key];
            }
            to.IsCompleteSet = from.IsCompleteSet;
        }

        /// <summary>
        /// Checks the compatibility of this instance with the specified item type.
        /// </summary>
        /// <param name="itemType">Type of the item.</param>
        /// <param name="errorMessage">The error message to report if the type is not compatible.</param>
        /// <returns>
        /// 	<c>true</c> if the type is compatible; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// </remarks>
        internal bool IsTypeCompatible(Type itemType, out string errorMessage)
        {
            errorMessage = null;
            TypeItemMapping typeMapping = TypeItemMapping.GetMapping(itemType);

            if (ItemName.GetType() != typeMapping.ItemNameMapping.FormatType)
            {
                errorMessage =
                    string.Format(
                        "Invalid ItemName type. The ItemName '{0}' is expected to be a '{1}' but is actually a '{2}'.",
                        ItemName, typeMapping.ItemNameMapping.PropertyType.FullName, ItemName.GetType().FullName);
            }

            foreach (string propertyName in this)
            {
                AttributeMapping typeAttMapping = typeMapping[propertyName];
                if (typeAttMapping == null)
                {
                    errorMessage =
                        string.Format(
                            "Invalid property value. The property named '{0}' was not found on the mapped item type '{1}'.",
                            propertyName, itemType.FullName);
                    break;
                }
            }
            return errorMessage == null;
        }
    }
}