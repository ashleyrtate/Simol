/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Coditate.Common.Util;
using Simol.Formatters;

namespace Simol.Core
{
    /// <summary>
    /// Provides common mapping operations with error checking and reporting.
    /// </summary>
    internal static class MappingUtils
    {
        /// <summary>
        /// Copies all mapped property values from an item object to a <see cref="PropertyValues"/> list.
        /// </summary>
        public static PropertyValues GetPropertyValues(ItemMapping mapping, object item)
        {
            object itemName = GetPropertyValue(item, mapping.ItemNameMapping);
            var itemNameStr = itemName as string;
            if (itemName == null || (itemNameStr != null && StringUtils.IsNullOrEmpty(itemNameStr, true)))
            {
                string message = string.Format("Item name of item '{0}' is null or an empty string.", item);
                throw new SimolDataException(message);
            }

            var values = new PropertyValues(itemName);
            foreach (AttributeMapping attributeMapping in mapping.AttributeMappings)
            {
                values[attributeMapping.PropertyName] = GetPropertyValue(item, attributeMapping);
            }

            return values;
        }

        public static void SetPropertyValue(object item, AttributeMapping mapping, object propertyValue)
        {
            PropertyInfo p = GetProperty(item.GetType(), mapping.PropertyName);
            try
            {
                p.SetValue(item, propertyValue, null);
            }
            catch (Exception ex)
            {
                string valueType = propertyValue != null ? propertyValue.GetType().FullName : "Unknown";
                string message =
                    string.Format(
                        "Error setting property '{0}'. The declared property type is '{1}'. The actual value type is '{2}'.",
                        mapping.FullPropertyName,
                        mapping.PropertyType,
                        valueType);
                throw new SimolDataException(message, ex);
            }
        }

        public static object GetPropertyValue(object item, AttributeMapping mapping)
        {
            PropertyInfo p = GetProperty(item.GetType(), mapping.PropertyName);
            try
            {
                return p.GetValue(item, null);
            }
            catch (Exception ex)
            {
                string message =
                    string.Format(
                        "Error reading property '{0}'. The property type is '{1}'.",
                        mapping.FullPropertyName,
                        mapping.PropertyType);
                throw new SimolDataException(message, ex);
            }
        }

        private static PropertyInfo GetProperty(Type t, string propertyName)
        {
            PropertyInfo p = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || !p.CanRead && !p.CanWrite)
            {
                string message = string.Format("Item type '{0}' has no public, read/write property named '{1}'",
                                               t.FullName, propertyName);
                throw new SimolDataException(message);
            }
            return p;
        }

        /// <summary>
        /// Copies all mapped property values to the specified item.
        /// </summary>
        public static void SetPropertyValues(ItemMapping mapping, PropertyValues values, object item)
        {
            SetPropertyValue(item, mapping.ItemNameMapping, values.ItemName);

            foreach (string propertyName in values)
            {
                AttributeMapping attributeMapping = mapping[propertyName];
                if (attributeMapping == null)
                {
                    continue;
                }
                object value = values[attributeMapping.PropertyName];
                SetPropertyValue(item, attributeMapping, value);
            }
        }

        /// <summary>
        /// Converts an item name value to a string.
        /// </summary>
        public static string ItemNameToString(PropertyFormatter formatter, AttributeMapping attributeMapping,
                                              object itemName)
        {
            try
            {
                return PropertyValueToString(formatter, attributeMapping, itemName);
            }
            catch (Exception ex)
            {
                string message = string.Format("Item name of type '{0}' could not be converted to a string using the " +
                                               "formatter configured for item name property '{1}'.",
                                               itemName.GetType().FullName,
                                               attributeMapping.FullPropertyName);
                throw new SimolDataException(message, ex);
            }
        }

        /// <summary>
        /// Converts a property value to a string.
        /// </summary>
        public static string PropertyValueToString(PropertyFormatter formatter, AttributeMapping attributeMapping,
                                                   object propertyValue)
        {
            try
            {
                return formatter.ToString(attributeMapping.Formatter, propertyValue);
            }
            catch (Exception ex)
            {
                string message =
                    string.Format(
                        "Property '{0}' with value '{1}' could not be converted to a string. The property type is '{2}'.",
                        attributeMapping.FullPropertyName, propertyValue,
                        attributeMapping.PropertyType.FullName);
                throw new SimolDataException(message, ex);
            }
        }

        /// <summary>
        /// Converts an attribute string to a typed property value.
        /// </summary>
        public static object StringToPropertyValue(PropertyFormatter formatter, AttributeMapping attributeMapping,
                                                   string valueString)
        {
            try
            {
                return formatter.ToType(attributeMapping.Formatter, valueString,
                                        attributeMapping.FormatType);
            }
            catch (Exception ex)
            {
                string message =
                    string.Format(
                        "String value '{0}' could not be converted to expected property type '{1}' for property '{2}'.",
                        valueString, attributeMapping.PropertyType.FullName,
                        attributeMapping.FullPropertyName);
                throw new SimolDataException(message, ex);
            }
        }



        /// <summary>
        /// Adds a property to the specified item.
        /// </summary>
        public static void AddProperty(AttributeMapping attributeMapping, PropertyValues values, object propertyValue)
        {
            if (attributeMapping.IsList)
            {
                AddListPropertyValue(attributeMapping, values, propertyValue);
            }
            else
            {
                values[attributeMapping.PropertyName] = propertyValue;
            }
        }

        /// <summary>
        /// Determines whether the specified value list is empty.
        /// </summary>
        public static bool IsEmptyList(ICollection valueList)
        {
            int count = 0;
            foreach (object o in valueList)
            {
                if (o != null)
                {
                    count++;
                }
            }
            return count == 0;
        }

        /// <summary>
        /// Converts a property value to a list of values.
        /// </summary>
        public static ICollection ToList(object value)
        {
            // casting directly to IEnumerable would treat strings as lists
            var valueList = value as ICollection;
            if (valueList == null || valueList.GetType().IsArray)
            {
                valueList = (value == null ? new object[] {} : new[] {value});
            }
            return valueList;
        }

        /// <summary>
        /// Adds a value to a list property
        /// </summary>
        public static void AddListPropertyValue(AttributeMapping attributeMapping, PropertyValues values, object propertyValue)
        {
            if (propertyValue == null)
            {
                return;
            }

            try
            {
                object list = values[attributeMapping.PropertyName];
                if (list == null)
                {
                    list = Activator.CreateInstance(attributeMapping.PropertyType);
                    values[attributeMapping.PropertyName] = list;
                }
                Type collectionInterface = attributeMapping.PropertyType.GetInterface("ICollection`1");
                MethodInfo addMethod = collectionInterface.GetMethod("Add");
                addMethod.Invoke(list, new[] {propertyValue});
            }
            catch (Exception ex)
            {
                string message =
                    string.Format(
                        "Error adding value to list property '{0}'. The property type is '{1}'. The value type is '{2}'.",
                        attributeMapping.FullPropertyName, attributeMapping.PropertyType,
                        propertyValue.GetType().FullName);
                throw new SimolDataException(message, ex);
            }
        }

        /// <summary>
        /// Creates an instance of an item type.
        /// </summary>
        public static object CreateInstance(Type itemType)
        {
            try
            {
                return Activator.CreateInstance(itemType);
            }
            catch (Exception ex)
            {
                string message =
                    string.Format(
                        "Unable to instantiate item of type '{0}'. Does the type have a public no-arg constructor?",
                        itemType.FullName);
                throw new SimolDataException(message, ex);
            }
        }
    }
}