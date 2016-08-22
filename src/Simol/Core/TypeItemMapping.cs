/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Coditate.Common.Util;
using Simol.Formatters;

namespace Simol.Core
{
    /// <summary>
    /// Defines mapping between a .Net type and a SimpleDB item.
    /// </summary>
    internal class TypeItemMapping : ItemMapping
    {
        public static readonly Dictionary<Type, TypeItemMapping> CachedMappings =
            new Dictionary<Type, TypeItemMapping>();

        /// <summary>
        /// Gets or sets the type of the item object.
        /// </summary>
        /// <value>The type of the item.</value>
        public Type ItemType { get; protected set; }

        /// <summary>
        /// Gets the names of all mapped properties on the specified item type.
        /// </summary>
        /// <param name="itemType">Type of the item.</param>
        /// <returns></returns>
        public static List<string> GetMappedProperties(Type itemType)
        {
            TypeItemMapping itemMapping = GetMapping(itemType);
            return itemMapping.AttributeMappings.Select(a => a.PropertyName).ToList();
        }

        /// <summary>
        /// Gets a mapping for the specified item type.
        /// </summary>
        /// <param name="itemType">Type of the item object.</param>
        /// <returns>A new or cached mapping instance</returns>
        public static TypeItemMapping GetMapping(Type itemType)
        {
            Arg.CheckNull("itemType", itemType);

            TypeItemMapping mapping;
            lock (((ICollection) CachedMappings).SyncRoot)
            {
                CachedMappings.TryGetValue(itemType, out mapping);

                if (mapping == null)
                {
                    mapping = CreateInternal(itemType);
                    CachedMappings.Add(itemType, mapping);
                }
            }

            return mapping;
        }

        /// <summary>
        /// Gets the scalar property type.
        /// </summary>
        /// <param name="propertyType">Type of the property.</param>
        /// <returns></returns>
        public static Type GetScalarType(Type propertyType)
        {
            Type scalarType = propertyType;

            Type collectionInterface = propertyType.GetInterface("ICollection`1");
            if (collectionInterface != null && !propertyType.IsArray)
            {
                scalarType = collectionInterface.GetGenericArguments()[0];
            }

            return scalarType;
        }

        /// <summary>
        /// Creates a new mapping instance for the specified type.
        /// </summary>
        /// <param name="itemType">Type of the item.</param>
        /// <returns></returns>
        public static TypeItemMapping CreateInternal(Type itemType)
        {
            var itemMapping = new TypeItemMapping
                {
                    ItemType = itemType,
                    DomainName = itemType.Name
                };

            // override domain name if domain attribute is present
            var domainAttribute = (DomainNameAttribute)
                                  itemType.GetCustomAttributes(typeof (DomainNameAttribute), true)
                                      .FirstOrDefault();
            if (domainAttribute != null)
            {
                itemMapping.DomainName = domainAttribute.DomainName;
            }
            var constraintAttribute = (ConstraintAttribute)
                                      itemType.GetCustomAttributes(typeof (ConstraintAttribute), true)
                                          .FirstOrDefault();
            if (constraintAttribute != null)
            {
                itemMapping.Constraint = constraintAttribute.Constraint;
            }

            CheckAttributesOnInvalidProperties(itemType);

            Dictionary<string, PropertyInfo> defaultProperties =
                itemType.GetProperties().Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p.Name);

            // set item name property and remove from default list
            PropertyInfo itemNameProperty = GetItemNameProperty(itemMapping.ItemType, defaultProperties);
            itemMapping.ItemNameMapping = CreateInternal(itemNameProperty);
            defaultProperties.Remove(itemMapping.ItemNameMapping.PropertyName);

            // process explicitly included/excluded properties
            List<string> includedNames =
                defaultProperties.Values.Where(
                    p => p.GetCustomAttributes(typeof (SimolIncludeAttribute), true).Any()).Select(p => p.Name).
                    ToList();
            List<string> customizedNames =
                defaultProperties.Values.Where(
                    p => p.GetCustomAttributes(typeof (SimolFormatAttribute), true).Any() ||
                         p.GetCustomAttributes(typeof (AttributeNameAttribute), true).Any() ||
                         p.GetCustomAttributes(typeof (SpanAttribute), true).Any() ||
                         p.GetCustomAttributes(typeof (IndexAttribute), true).Any()).Select(p => p.Name).
                    ToList();

            List<string> excludedNames = defaultProperties.Values.Where(
                p => p.GetCustomAttributes(typeof (SimolExcludeAttribute), true).Any()).Select(p => p.Name).
                ToList();


            foreach (string name in excludedNames)
            {
                defaultProperties.Remove(name);
            }

            var persistentProperties = new Dictionary<string, PropertyInfo>();
            if (includedNames.Count == 0)
            {
                // select only value types or string by default
                foreach (PropertyInfo p in defaultProperties.Values.Where(p => ShouldIncludeType(p.PropertyType)))
                {
                    persistentProperties[p.Name] = p;
                }
            }
            else
            {
                // add only specifically included properties
                foreach (string name in includedNames.Where(n => defaultProperties.ContainsKey(n)))
                {
                    persistentProperties[name] = defaultProperties[name];
                }
            }
            // always add any properties that have been specifically customized
            foreach (string name in customizedNames.Where(n => defaultProperties.ContainsKey(n)))
            {
                persistentProperties[name] = defaultProperties[name];
            }

            // create property mappings
            var attributeMappings = new List<AttributeMapping>();
            foreach (PropertyInfo property in persistentProperties.Values)
            {
                TypeAttributeMapping typeAttributeMapping = CreateInternal(property);
                attributeMappings.Add(typeAttributeMapping);
            }

            itemMapping.AttributeMappings.AddRange(attributeMappings.OrderBy(p => p.AttributeName));

            CheckDuplicateAttributes(itemMapping);

            return itemMapping;
        }

        private static bool ShouldIncludeType(Type propertyType)
        {
            Type scalarType = GetScalarType(propertyType);

            return (scalarType.IsValueType || typeof (string).IsAssignableFrom(scalarType));
        }

        private static TypeAttributeMapping CreateInternal(PropertyInfo property)
        {
            var formatAttrib = (SimolFormatAttribute)
                               property.GetCustomAttributes(typeof (SimolFormatAttribute), true).FirstOrDefault();
            var nameAttrib =
                (AttributeNameAttribute)
                property.GetCustomAttributes(typeof (AttributeNameAttribute), true).FirstOrDefault();
            var spanAttrib = (SpanAttribute)
                             property.GetCustomAttributes(typeof (SpanAttribute), true).FirstOrDefault();
            var indexAttrib =
                (IndexAttribute) property.GetCustomAttributes(typeof (IndexAttribute), true).FirstOrDefault();
            var versionAttrib =
                (VersionAttribute) property.GetCustomAttributes(typeof (VersionAttribute), true).FirstOrDefault();

            var attributeMapping = new TypeAttributeMapping(property);

            var customFormat = formatAttrib as CustomFormatAttribute;
            var numberFormat = formatAttrib as NumberFormatAttribute;
            if (numberFormat != null)
            {
                CheckNumberFormatter(attributeMapping, numberFormat);

                attributeMapping.Formatter = new NumberFormatter
                    {
                        IsSigned = attributeMapping.IsSigned,
                        ApplyOffset = numberFormat.ApplyOffset,
                        DecimalDigits = numberFormat.DecimalDigits,
                        WholeDigits = numberFormat.WholeDigits
                    };
            }
            else if (customFormat != null)
            {
                if (customFormat.Format != null && !attributeMapping.IsFormattable)
                {
                    string message =
                        string.Format(
                            "{0}.Format cannot be used with property '{1}.{2}'. The property type does not implement '{3}'.",
                            typeof (CustomFormatAttribute).Name, property.DeclaringType.FullName,
                            property.Name, typeof (IFormattable));
                    throw new SimolConfigurationException(message);
                }

                attributeMapping.Formatter = new CustomFormatter
                    {
                        Formatter = customFormat.Formatter,
                        Format = customFormat.Format
                    };
            }
            if (nameAttrib != null)
            {
                attributeMapping.AttributeName = nameAttrib.AttributeName;
            }
            if (spanAttrib != null)
            {
                if (attributeMapping.IsList)
                {
                    string message =
                        string.Format(
                            "{0} may not be used with list properties. The property '{1}.{2}' has a type of '{3}'.",
                            typeof (SpanAttribute).Name, property.DeclaringType.FullName,
                            property.Name, attributeMapping.PropertyType.FullName);
                    throw new SimolConfigurationException(message);
                }
                attributeMapping.SpanAttributes = SpanType.Span;
                if (spanAttrib.Compress)
                {
                    attributeMapping.SpanAttributes |= SpanType.Compress; 
                }
                if (spanAttrib.Encrypt)
                {
                    attributeMapping.SpanAttributes |= SpanType.Encrypt;
                }
            }
            if (versionAttrib != null)
            {
                if (attributeMapping.IsList ||
                    (attributeMapping.FormatType != typeof (int) && attributeMapping.FormatType != typeof (DateTime)))
                {
                    string message =
                        string.Format(
                            "{0} may only be used with DateTime or int properties. The property '{1}.{2}' has a type of '{3}'.",
                            typeof (VersionAttribute).Name, property.DeclaringType.FullName,
                            property.Name, attributeMapping.PropertyType.FullName);
                    throw new SimolConfigurationException(message);
                }
                attributeMapping.Versioning = versionAttrib.Versioning;
            }
            if (indexAttrib != null)
            {
                if (attributeMapping.ScalarType != typeof (string))
                {
                    string message =
                        string.Format(
                            "{0} may only be used with string properties. The property '{1}.{2}' has a scalar type of '{3}'.",
                            typeof (IndexAttribute).Name, property.DeclaringType.FullName,
                            property.Name, attributeMapping.ScalarType.FullName);
                    throw new SimolConfigurationException(message);
                }
                attributeMapping.IsIndexed = true;
            }

            return attributeMapping;
        }

        private static void CheckNumberFormatter(TypeAttributeMapping mapping, NumberFormatAttribute numberFormat)
        {
            if (!mapping.IsNumeric)
            {
                string message =
                    string.Format(
                        "{0} cannot be used with property '{1}.{2}'. The property type is not numeric.",
                        typeof (NumberFormatAttribute).Name, mapping.Property.DeclaringType.FullName,
                        mapping.Property.Name);
                throw new SimolConfigurationException(message);
            }
            var defaultFormatter = (NumberFormatter)
                                   PropertyFormatter.GetDefaultFormatter(mapping.FormatType);
            int defaultDigits = defaultFormatter.WholeDigits + defaultFormatter.DecimalDigits;
            int customDigits = numberFormat.WholeDigits + numberFormat.DecimalDigits;
            if (customDigits > defaultDigits)
            {
                string message =
                    string.Format("{0} specifies too many whole and/or decimal digits for property '{1}.{2}'. " +
                                  "A maximum of {3} digits is supported for numeric type '{4}'.",
                                  typeof (NumberFormatAttribute).Name, mapping.Property.DeclaringType.FullName,
                                  mapping.Property.Name, defaultDigits, mapping.FormatType.FullName);
                throw new SimolConfigurationException(message);
            }
        }

        private static void CheckAttributesOnInvalidProperties(Type dataType)
        {
            List<string> invalidProperties =
                dataType.GetProperties().Where(
                    p => (!p.CanRead || !p.CanWrite) && HasActiveAttribute(p)).Select(p => "'" + p.Name + "'").ToList();

            if (invalidProperties.Count > 0)
            {
                string properties = StringUtils.Join(" ", invalidProperties);
                string message = string.Format("Only public, read/write properties may be used with Simol. " +
                                               "The following non-conforming properties of type '{0}' have been marked with {1}s: {2}",
                                               dataType.FullName, typeof (SimolAttribute).Name, properties);
                throw new SimolConfigurationException(message);
            }
        }

        private static bool HasActiveAttribute(PropertyInfo property)
        {
            return
                property.GetCustomAttributes(typeof (SimolAttribute), true).Where(a => !(a is SimolExcludeAttribute)).
                    Count() > 0;
        }

        private static void CheckDuplicateAttributes(TypeItemMapping mapping)
        {
            string lastAttributeName = null;
            for (int k = 0; k < mapping.AttributeMappings.Count; k++)
            {
                if (string.Equals(lastAttributeName, mapping.AttributeMappings[k].AttributeName))
                {
                    string message =
                        string.Format("Type '{0}' has multiple properties mapped to SimpleDB attribute '{1}'.",
                                      mapping.ItemType.FullName, lastAttributeName);
                    throw new SimolConfigurationException(message);
                }
                lastAttributeName = mapping.AttributeMappings[k].AttributeName;
            }
        }

        private static PropertyInfo GetItemNameProperty(Type dataType,
                                                        Dictionary<string, PropertyInfo> defaultProperties)
        {
            PropertyInfo itemNameProperty = null;

            List<PropertyInfo> itemNameProperties =
                defaultProperties.Values.Where(p => p.GetCustomAttributes(typeof (ItemNameAttribute), true).Length > 0).
                    ToList();
            if (itemNameProperties.Count > 1)
            {
                string message =
                    string.Format("Type '{0}' has multiple properties marked with an {1}.",
                                  dataType, typeof (ItemNameAttribute).Name);
                throw new SimolConfigurationException(message);
            }
            if (itemNameProperties.Count > 0)
            {
                itemNameProperty = itemNameProperties[0];
            }
            if (itemNameProperty == null)
            {
                string message =
                    string.Format("Type '{0}' has no property marked with an {1}.",
                                  dataType, typeof (ItemNameAttribute).Name);
                throw new SimolConfigurationException(message);
            }
            return itemNameProperty;
        }
    }
}