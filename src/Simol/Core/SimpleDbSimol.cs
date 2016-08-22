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
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Coditate.Common.Util;
using Simol.Consistency;
using Simol.Formatters;
using AmazonAttribute = Amazon.SimpleDB.Model.Attribute;

namespace Simol.Core
{
    /// <summary>
    /// Simol implementation that interacts with SimpleDB.
    /// </summary>
    internal class SimpleDbSimol : ISimolInternal
    {
        private readonly NumberFormatter countFormatter;
        private SpanUtils spanUtils;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleDbSimol"/> class.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <param name="simpleDb">The simple db.</param>
        public SimpleDbSimol(SimolConfig config, AmazonSimpleDB simpleDb)
        {
            Arg.CheckNull("config", config);
            Arg.CheckNull("simpleDb", simpleDb);

            SimpleDB = simpleDb;
            Config = config;

            countFormatter = new NumberFormatter
                {
                    ApplyOffset = false,
                    IsSigned = false,
                    WholeDigits = 10
                };

            spanUtils = new SpanUtils(config);
        }

        public SimolConfig Config { get; private set; }

        public AmazonSimpleDB SimpleDB { get; private set; }

        public void PutAttributes(ItemMapping mapping, List<PropertyValues> values)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("values", values);

            ValuesItemMapping valuesMapping = CreateValuesMapping(mapping);

            PutImpl(valuesMapping, new List<PropertyValues>(values));
        }

        public PropertyValues GetAttributes(ItemMapping mapping, object itemName, List<string> propertyNames)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("itemName", itemName);
            propertyNames = propertyNames ?? ListUtils.EmptyStringList;

            ValuesItemMapping itemMapping = CreateValuesMapping(mapping);

            return GetImpl(itemMapping, itemName, propertyNames.Count > 0);
        }

        public void DeleteAttributes(ItemMapping mapping, List<object> itemNames, List<string> propertyNames)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckIsType("itemNames.First", itemNames.First(), mapping.ItemNameMapping.PropertyType);
            propertyNames = propertyNames ?? ListUtils.EmptyStringList;

            ValuesItemMapping valuesMapping = CreateValuesMapping(mapping);

            DeleteImpl(valuesMapping, itemNames, propertyNames);
        }

        private void DeleteImpl(ValuesItemMapping itemMapping, List<object> itemNames, List<string> propertyNames)
        {
            if (itemNames.Count == 1)
            {
                DeleteOne(itemMapping, itemNames.First(), propertyNames);
            }
            else
            {
                int startIndex = 0;
                do
                {
                    int batchCount = Math.Min(Config.BatchDeleteMaxCount, itemNames.Count - startIndex);
                    List<object> batch = itemNames.GetRange(startIndex, batchCount);
                    startIndex += batchCount;

                    DeleteBatch(itemMapping, batch, propertyNames);
                } while (startIndex < itemNames.Count);
            }
        }

        private void DeleteOne(ValuesItemMapping mapping, object itemName, List<string> propertyNames)
        {
            string itemNameString = MappingUtils.ItemNameToString(Config.Formatter, mapping.ItemNameMapping, itemName);
            var deleteRequest = new DeleteAttributesRequest { DomainName = mapping.DomainName, ItemName = itemNameString };
            foreach (string propertyName in propertyNames)
            {
                var attribute = new AmazonAttribute
                {
                    Name =
                        mapping[propertyName].AttributeName
                };
                deleteRequest.Attribute.Add(attribute);
            }
            SimpleDB.DeleteAttributes(deleteRequest);
        }

        private void DeleteBatch(ValuesItemMapping mapping, List<object> itemNames, List<string> propertyNames)
        {
            var deleteRequest = new BatchDeleteAttributesRequest { DomainName = mapping.DomainName };
            var attributes = new List<AmazonAttribute>();
            foreach (string propertyName in propertyNames)
            {
                var attribute = new AmazonAttribute
                {
                    Name = mapping[propertyName].AttributeName
                };
                attributes.Add(attribute);
            }
            foreach (object itemName in itemNames)
            {
                string itemNameString = MappingUtils.ItemNameToString(Config.Formatter, mapping.ItemNameMapping, itemName);
                var deleteItem = new DeleteableItem
                {
                    ItemName = itemNameString,
                    Attribute = attributes
                };
                deleteRequest.Item.Add(deleteItem);
            }
            SimpleDB.BatchDeleteAttributes(deleteRequest);
        }

        public SelectResults<PropertyValues> SelectAttributes(SelectCommand command)
        {
            Arg.CheckNull("command", command);
            Arg.CheckCondition("command", !command.IsCancelled, "Command was already cancelled");

            command.Formatter = Config.Formatter;
            string paginationToken = command.PaginationToken;
            bool wasCommandCancelled = false;
            List<Item> resultItems = GetAllResults(command, ref paginationToken, ref wasCommandCancelled);
            ValuesItemMapping valuesMapping = CreateValuesMapping(command.Mapping);

            var results = new SelectResults<PropertyValues>
                {
                    PaginationToken = paginationToken,
                    WasCommandCancelled = wasCommandCancelled
                };

            foreach (Item i in resultItems)
            {
                PropertyValues values = CreateValues(valuesMapping, i.Attribute, i.Name, command.IsCompleteSet);
                results.Items.Add(values);
            }

            return results;
        }

        public object SelectScalar(SelectCommand command)
        {
            Arg.CheckNull("command", command);

            command.Formatter = Config.Formatter;
            string paginationToken = command.PaginationToken;
            bool wasCommandCancelled = false;
            List<Item> resultItems = GetAllResults(command, ref paginationToken, ref wasCommandCancelled);

            Item firstItem = resultItems.FirstOrDefault();
            if (firstItem == null)
            {
                return null;
            }

            object scalarValue = null;
            // explicitly support count(*)
            if (firstItem.Name == "Domain")
            {
                // sum all count valus returned
                int count = 0;
                foreach (Item i in resultItems)
                {
                    string value = i.Attribute.Where(a => a.Name == "Count").Select(a => a.Value).FirstOrDefault();
                    count += (int) countFormatter.ToType(value, typeof (int));
                }
                scalarValue = count;
            }
            else
            {
                AmazonAttribute firstAttribute = firstItem.Attribute.FirstOrDefault();
                ValuesItemMapping valuesMapping = CreateValuesMapping(command.Mapping);
                if (firstAttribute == null)
                {
                    // if no attributes found return the item name
                    scalarValue = MappingUtils.StringToPropertyValue(Config.Formatter, valuesMapping.ItemNameMapping,
                                                                     firstItem.Name);
                }
                else
                {
                    AttributeMapping attributeMapping =
                        valuesMapping.AttributeMappings.Where(p => p.AttributeName == firstAttribute.Name).
                            FirstOrDefault();
                    if (attributeMapping != null)
                    {
                        scalarValue = MappingUtils.StringToPropertyValue(Config.Formatter, attributeMapping,
                                                                         firstAttribute.Value);
                    }
                }
            }
            return scalarValue;
        }

        private List<Item> GetAllResults(SelectCommand command, ref string paginationToken, ref bool wasCommandCancelled)
        {
            command.CheckNeedsReset();
            
            var resultItems = new List<Item>();
            int pagesRequested = 0;
            while (CollectResults(command, ref resultItems, ref paginationToken, ++pagesRequested))
            {
                // only mark the result as cancelled if the cancellation actually impacts
                // the returned results
                if (command.IsCancelled)
                {
                    wasCommandCancelled = true;
                    break;
                }
            }
            return resultItems;
        }

        private ValuesItemMapping CreateValuesMapping(ItemMapping mapping)
        {
            ValuesItemMapping valuesMapping = ValuesItemMapping.CreateInternal(mapping, new List<string>(), false);
            return valuesMapping;
        }

        private PropertyValues GetImpl(ValuesItemMapping itemMapping, object itemName,
                                       bool enumerateAttributes)
        {
            string itemNameString = MappingUtils.ItemNameToString(Config.Formatter, itemMapping.ItemNameMapping,
                                                                  itemName);
            var request = new GetAttributesRequest
                {
                    DomainName = itemMapping.DomainName,
                    ItemName = itemNameString
                };
            // enumerating attribute names limits the returned values to those we requested
            if (enumerateAttributes)
            {
                request.AttributeName.AddRange(itemMapping.AttributeMappings.Select(a => a.AttributeName));
            }

            GetAttributesResponse response = SimpleDB.GetAttributes(request);

            PropertyValues values = null;
            if (response.GetAttributesResult.Attribute.Count > 0)
            {
                values = CreateValues(itemMapping, response.GetAttributesResult.Attribute, itemNameString,
                                      !enumerateAttributes);
            }

            return values;
        }

        private void PutImpl(ValuesItemMapping itemMapping, List<PropertyValues> allValues)
        {
            if (allValues.Count == 1)
            {
                PutOne(itemMapping, allValues[0]);
            }
            else
            {
                int startIndex = 0;
                do
                {
                    int batchCount = Math.Min(Config.BatchPutMaxCount, allValues.Count - startIndex);
                    List<PropertyValues> batch = allValues.GetRange(startIndex, batchCount);
                    startIndex += batchCount;

                    PutBatch(itemMapping, batch);
                } while (startIndex < allValues.Count);
            }
        }

        private void PutOne(ValuesItemMapping itemMapping, PropertyValues values)
        {
            string itemName = MappingUtils.ItemNameToString(Config.Formatter, itemMapping.ItemNameMapping,
                                                            values.ItemName);

            var request = new PutAttributesRequest
                {
                    DomainName = itemMapping.DomainName,
                    Attribute = CreatePutAttributes(itemMapping, values),
                    ItemName = itemName
                };

            VersioningUtils.ApplyVersioningBehavior(Config.Formatter, itemMapping, values, request.Attribute, request);

            SimpleDB.PutAttributes(request);
        }

        private void PutBatch(ValuesItemMapping itemMapping, List<PropertyValues> allValues)
        {
            var request = new BatchPutAttributesRequest
                {
                    DomainName = itemMapping.DomainName
                };
            foreach (PropertyValues values in allValues)
            {
                string itemName = MappingUtils.ItemNameToString(Config.Formatter, itemMapping.ItemNameMapping,
                                                                values.ItemName);
                var item = new ReplaceableItem
                    {
                        ItemName = itemName,
                        Attribute = CreatePutAttributes(itemMapping, values)
                    };

                foreach (var attribute in item.Attribute)
                {
                    attribute.Replace = Config.BatchReplaceAttributes;
                }
                VersioningUtils.ApplyVersioningBehavior(Config.Formatter, itemMapping, values, item.Attribute, null);

                request.Item.Add(item);
            }

            SimpleDB.BatchPutAttributes(request);
        }

        private bool CollectResults(SelectCommand command, ref List<Item> results, ref string paginationToken,
                                    int pagesRequested)
        {
            var request = new SelectRequest
                {
                    NextToken = paginationToken,
                    SelectExpression = command.ExpandedCommandText,
                };

            SelectResponse response = SimpleDB.Select(request);

            paginationToken = response.SelectResult.NextToken;

            results.AddRange(response.SelectResult.Item);

            bool getMoreResults = command.MaxResultPages <= 0 || pagesRequested < command.MaxResultPages;
            bool moreResultsAvailable = !string.IsNullOrEmpty(response.SelectResult.NextToken);
            return getMoreResults && moreResultsAvailable;
        }

        private PropertyValues CreateValues(ItemMapping mapping, List<AmazonAttribute> attributes, string itemName,
                                            bool isCompleteSet)
        {
            object itemNameValue = MappingUtils.StringToPropertyValue(Config.Formatter, mapping.ItemNameMapping,
                                                                      itemName);
            var propertyValues = new PropertyValues(itemNameValue)
                {
                    IsCompleteSet = isCompleteSet
                };

            foreach (AttributeMapping attributeMapping in mapping.AttributeMappings)
            {
                List<AmazonAttribute> attributeGroup =
                    attributes.Where(a => a.Name == attributeMapping.AttributeName).ToList();
                ApplyAttributes(attributeMapping, attributeGroup, propertyValues);
            }

            return propertyValues;
        }

        private void ApplyAttributes(AttributeMapping attributeMapping, List<AmazonAttribute> attributes, PropertyValues values)
        {
            if (IsEmptyAttribute(attributes))
            {
                return;
            }
            
            if (attributeMapping.SpanAttributes == SpanType.None)
            {
                foreach (AmazonAttribute attribute in attributes)
                {
                    object propertyValue = MappingUtils.StringToPropertyValue(Config.Formatter, attributeMapping,
                                                                              attribute.Value);
                    MappingUtils.AddProperty(attributeMapping, values, propertyValue);
                }
            }
            else
            {
                string valueStr = spanUtils.JoinAttributeValues(attributes.Select(a => a.Value).ToList(), attributeMapping.SpanAttributes);
                object propertyValue = MappingUtils.StringToPropertyValue(Config.Formatter, attributeMapping, valueStr);
                MappingUtils.AddProperty(attributeMapping, values, propertyValue);
            }
        }

        private bool IsEmptyAttribute(List<AmazonAttribute> attributes)
        {
            return (attributes.Count == 0 || attributes[0].Value == PropertyFormatter.Base64NullString);
        }

        private List<ReplaceableAttribute> CreatePutAttributes(ItemMapping mapping, PropertyValues values)
        {
            var attributes = new List<ReplaceableAttribute>();

            foreach (AttributeMapping attributeMapping in mapping.AttributeMappings)
            {
                AddPutAttributes(attributes, attributeMapping, values);
            }
            return attributes;
        }

        private void AddPutAttributes(List<ReplaceableAttribute> attributes, AttributeMapping mapping,
                                      PropertyValues values)
        {
            // skip properties in the mapping but not in the values collection so we
            // don't null out existing attributes when saving partial property sets
            if (!values.ContainsProperty(mapping.PropertyName))
            {
                return;
            }
            object value = values[mapping.PropertyName];
            ICollection valueList = MappingUtils.ToList(value);

            if (Config.NullPutBehavior == NullBehavior.MarkAsNull && MappingUtils.IsEmptyList(valueList))
            {
                string valueStr = MappingUtils.PropertyValueToString(Config.Formatter, mapping, null);
                AddPutAttribute(attributes, mapping.AttributeName, valueStr);
            }

            foreach (object propertyValue in valueList)
            {
                if (propertyValue == null)
                {
                    continue;
                }
                string valueStr = MappingUtils.PropertyValueToString(Config.Formatter, mapping, propertyValue);
                if (mapping.SpanAttributes == SpanType.None)
                {
                    AddPutAttribute(attributes, mapping.AttributeName, valueStr);
                }
                else
                {
                    AddSpanAttributes(attributes, mapping, valueStr);
                }
            }
        }

        private void AddSpanAttributes(List<ReplaceableAttribute> attributes, AttributeMapping attributeMapping,
                                       string value)
        {
            List<string> values = spanUtils.SplitPropertyValue(value, attributeMapping.SpanAttributes);
            foreach (string v in values)
            {
                AddPutAttribute(attributes, attributeMapping.AttributeName, v);
            }
        }

        private void AddPutAttribute(List<ReplaceableAttribute> attributes, string name, string value)
        {
            var attribute = new ReplaceableAttribute
                {
                    Name = name,
                    Replace = true,
                    Value = value
                };
            attributes.Add(attribute);
        }
    }
}