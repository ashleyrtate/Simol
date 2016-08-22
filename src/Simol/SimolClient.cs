/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.SimpleDB;
using Coditate.Common.Util;
using Simol.Async;
using Simol.Cache;
using Simol.Consistency;
using Simol.Core;
using Simol.Indexing;
using AmazonAttribute = Amazon.SimpleDB.Model.Attribute;
using System.Collections;

namespace Simol
{
    /// <summary>
    /// Encapsulates the Simol interface to SimpleDB.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a variety of methods for mapping object properties to SimpleDB 
    /// attributes. Strongly typed methods (those with generic parameters) are provided
    /// for working with entire objects or partial property sets (<see cref="ISimol"/>). Methods are also
    /// provided for dynamic, typeless operations on arbitrary property sets (<see cref="ISimol"/>).
    /// </para>
    /// <para>
    /// For most operations the generic <see cref="Type"/> parameter (the item type) is examined
    /// to determine mapping and formatting rules when converting between
    /// object properties and SimpleDB attributes. For operations without generic type
    /// parameters, an <see cref="ItemMapping"/> must be provided to accomplish the same purpose.
    /// </para>
    /// <para>
    /// For information on mapping behavior and usage examples, see the task-oriented documentation at: 
    /// <a href="http://simol.codeplex.com/documentation">http://simol.codeplex.com/documentation</a>
    /// </para>
    /// <para>
    /// All public members of this class are thread-safe <em>provided</em> that the installed <see cref="IItemCache"/> implementation
    /// is thread-safe.
    /// </para>
    /// <para>
    /// Any Simol method can be invoked asynchronously using the async extension methods. To use
    /// these extension methods add the <c>Simol.Async</c> namespace to your calling class like this:
    /// <code>
    /// using Simol.Async;
    /// // [snip]   
    /// var employeeId = new Guid("6c0a37b4-9c59-49ce-95af-d01a66f9605d");
    /// IAsyncResult result = simol.BeginGet&lt;Employee&gt;(employeeId, null, null);
    /// // do something else useful
    /// var e = simol.EndGet&lt;Employee&gt;(result);
    /// </code>
    /// </para>
    /// <para>
    /// Instances of this class are somewhat expensive to create when caching and automatic domain creation are enabled. It is generally good practice
    /// to reuse a single instance across an entire application unless additional instances are needed with different configuration options.
    /// </para>
    /// </remarks>
    /// <seealso cref="SimolConfig"/>
    /// <seealso cref="SelectCommand{T}"/>
    /// <seealso cref="AsyncExtensions"/>
    public class SimolClient : ISimol
    {
        private ISimolInternal simol;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimolClient"/> class.
        /// </summary>
        /// <param name="awsAccessKeyId">The Amazon Web Services access key id.</param>
        /// <param name="awsSecretAccessKey">The Amazon Web Services secret access key.</param>
        public SimolClient(string awsAccessKeyId, string awsSecretAccessKey)
            : this(awsAccessKeyId, awsSecretAccessKey, new SimolConfig())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimolClient"/> class.
        /// </summary>
        /// <param name="awsAccessKeyId">The Amazon Web Services access key id.</param>
        /// <param name="awsSecretAccessKey">The Amazon Web Services secret access key.</param>
        /// <param name="config">The Simol advanced configuration options.</param>
        public SimolClient(string awsAccessKeyId, string awsSecretAccessKey, SimolConfig config)
        {
            Arg.CheckNullOrEmpty("awsAccessKeyId", awsAccessKeyId);
            Arg.CheckNullOrEmpty("awsSecretAccessKey", awsSecretAccessKey);
            Arg.CheckNull("config", config);

            var amazonSimpleDb = new AmazonSimpleDBClient(awsAccessKeyId, awsSecretAccessKey);
            Init(amazonSimpleDb, config);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimolClient"/> class.
        /// </summary>
        /// <param name="simpleDb">An already configured SimpleDB client.</param>
        /// <param name="config">The Simol advanced configuration options.</param>
        /// <remarks>
        /// Simol uses the AWS SDK classes for low-level communication with 
        /// SimpleDB. This constructor allows for advanced configuration of 
        /// the underlying <see cref="AmazonSimpleDB"/> instance.
        /// </remarks>
        public SimolClient(AmazonSimpleDB simpleDb, SimolConfig config)
        {
            Arg.CheckNull("simpleDb", simpleDb);
            Arg.CheckNull("config", config);

            Init(simpleDb, config);
        }

        /// <summary>
        /// Gets or sets the underlying AWS SDK class used to communicate with SimpleDB.
        /// </summary>
        /// <value>The service.</value>
        public AmazonSimpleDB SimpleDB { get; private set; }

        /// <summary>
        /// Gets or sets the Simol advanced configuration options.
        /// </summary>
        /// <value>The Simol configuration.</value>
        public SimolConfig Config { get; private set; }

        /// <summary>
        /// Puts the specified item into SimpleDB.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <remarks>
        /// This method only puts items using the PutAttributes operation.
        /// </remarks>
        /// <exception cref="ArgumentNullException">If the item parameter is null</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the item cannot be converted to a SimpleDB item</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        public void Put(object item)
        {
            Arg.CheckNull("item", item);

            ItemMapping mapping = ItemMapping.Create(item.GetType());
            var values = PropertyValues.CreateValues(mapping, item);
            var allValues = new List<PropertyValues>() { values };

            simol.PutAttributes(mapping, allValues);
        }

        /// <summary>
        /// Puts multiple items into SimpleDB.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <remarks>
        /// Single items are put using the SimpleDB PutAttributes operation. Multiple items are
        /// put using the BatchPutAttributes operation. Lists of items
        /// are automatically split into multiple batches to fit within the 25 item limit for BatchPutAttributes.
        /// </remarks>
        /// <exception cref="ArgumentNullException">If the items parameter is null</exception>
        /// <exception cref="ArgumentException">If the items parameter contains more than one type of item</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the item cannot be converted to a SimpleDB item</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <seealso cref="SimolConfig.BatchReplaceAttributes"/>
        public void Put<T>(List<T> items)
        {
            Arg.CheckNull("items", items);
            if (items.Count == 0)
            {
                return;
            }
            if (items.Where(i => i == null).Any())
            {
                throw new ArgumentException("The item list may not contain null values.");
            }

            ItemMapping mapping = ItemMapping.Create(typeof(T));
            var allValues = PropertyValues.CreateValues(mapping, items);

            simol.PutAttributes(mapping, allValues);
        }

        /// <summary>
        /// Puts multiple property values collections into SimpleDB.
        /// </summary>
        /// <typeparam name="T">The item type</typeparam>
        /// <param name="items">Values for the items to store</param>
        /// <exception cref="ArgumentNullException">If the items parameter is null</exception>
        /// <exception cref="ArgumentException">If the items parameter contains property values that are incompatible
        /// with the item type.
        /// </exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the item values cannot be converted to SimpleDB attributes</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// See <see cref="Put{T}"/> for details on batch-put behavior.
        /// </remarks>
        public void PutAttributes<T>(List<PropertyValues> items)
        {
            Arg.CheckNull("items", items);

            if (items.Count == 0)
            {
                return;
            }

            for (int k = 0; k < items.Count; k++)
            {
                string errorMessage;
                if (items[k].IsTypeCompatible(typeof(T), out errorMessage))
                {
                    continue;
                }
                string fullMessage = string.Format("'items[{0}]' has a problem: {1}", k, errorMessage);
                throw new ArgumentException(fullMessage);
            }

            var mapping = TypeItemMapping.GetMapping(typeof(T));
            simol.PutAttributes(mapping, items);
        }

        /// <summary>
        /// Puts a property values collection into SimpleDB.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">Values for the item to store</param>
        /// <exception cref="ArgumentNullException">If the item parameter is null</exception>
        /// <exception cref="ArgumentException">If the item parameter contains property values that are incompatible
        /// with the item type.
        /// </exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the item values cannot be converted to SimpleDB attributes</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// </remarks>
        public void PutAttributes<T>(PropertyValues item)
        {
            Arg.CheckNull("item", item);

            var mapping = TypeItemMapping.GetMapping(typeof(T));
            string errorMessage;
            if (!item.IsTypeCompatible(typeof(T), out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            simol.PutAttributes(mapping, item.ToUniList());
        }

        /// <summary>
        /// Gets an item from SimpleDB.
        /// </summary>
        /// <typeparam name="T">Type of the item to return</typeparam>
        /// <param name="itemName">SimpleDB item name of the item to get.</param>
        /// <returns>
        /// The item or null if no attributes exist for the specified item name.
        /// </returns>
        /// <remarks>
        /// </remarks>
        /// <exception cref="ArgumentNullException">If the itemName parameter is null</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the SimpleDB item cannot be converted into the requested item type</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the requested item type</exception>
        public T Get<T>(object itemName)
        {
            ItemMapping mapping = TypeItemMapping.GetMapping(typeof(T));
            Arg.CheckIsType("itemName", itemName, mapping.ItemNameMapping.PropertyType);

            PropertyValues values = simol.GetAttributes(mapping, itemName, null);

            return (T)PropertyValues.CreateItem(typeof(T), values);
        }

        /// <summary>
        /// Gets a property values collection from SimpleDB.
        /// </summary>
        /// <typeparam name="T">Item type which identifies the SimpleDB domain and
        /// mapping rules for converting attributes to property values</typeparam>
        /// <param name="itemName">Identifies the item for which attributes will be retrieved</param>
        /// <param name="propertyNames">List of properties whose values will be retrieved</param>
        /// <returns>
        /// The property values or null if no attributes exist for the specified item name.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the itemName parameter is null</exception>
        /// <exception cref="ArgumentException">If the itemName type is invalid for the item type</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// Supports retrieval of a subset of typed property values when
        /// you don't need or want to retrieve all attributes stored in a domain.
        /// </remarks>
        public PropertyValues GetAttributes<T>(object itemName, params string[] propertyNames)
        {
            Arg.CheckNull("propertyNames", propertyNames);
            ItemMapping mapping = ValuesItemMapping.CreateInternal(typeof(T), propertyNames.ToList());
            Arg.CheckIsType("itemName", itemName, mapping.ItemNameMapping.PropertyType);

            return simol.GetAttributes(mapping, itemName, propertyNames.ToList());
        }

        /// <summary>
        /// Deletes an item from SimpleDB (all attributes).
        /// </summary>
        /// <typeparam name="T">Type of the item to delete</typeparam>
        /// <param name="itemName">Item name of the item to delete.</param>
        /// <remarks>
        /// </remarks>
        /// <exception cref="ArgumentNullException">If the itemName parameter is null</exception>
        /// <exception cref="ArgumentException">If the itemName type is invalid for the item type</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        public void Delete<T>(object itemName)
        {
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof(T));
            Arg.CheckIsType("itemName", itemName, mapping.ItemNameMapping.PropertyType);

            simol.DeleteAttributes(mapping, new List<object> { itemName }, null);
        }

        /// <summary>
        /// Deletes multiple items from SimpleDB (all attributes).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemNames">Item names of the items to delete</param>
        /// <exception cref="ArgumentNullException">If the itemNames parameter is null</exception>
        /// <exception cref="ArgumentException">If the itemNames types are invalid for the item type</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// Single items are deleted using the SimpleDB DeleteAttributes operation. Multiple items are
        /// deleted using the BatchDeleteAttributes operation. Lists of items
        /// are automatically split into multiple batches to fit within the 25 item limit for BatchDeleteAttributes.
        /// </remarks>
        public void Delete<T>(IList itemNames)
        {
            Arg.CheckNull("itemNames", itemNames);
            if (itemNames.Count == 0)
            {
                return;
            }
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof(T));
            Arg.CheckIsType("itemNames[0]", itemNames[0], mapping.ItemNameMapping.PropertyType);

            simol.DeleteAttributes(mapping, itemNames.Cast<object>().ToList(), null);
        }

        /// <summary>
        /// Deletes specified attributes from a single item in SimpleDB.
        /// </summary>
        /// <typeparam name="T">Item type which identifies the SimpleDB domain where the item is stored</typeparam>
        /// <param name="itemName">Identifies the item from which to remove attributes</param>
        /// <param name="propertyNames">Names of properties to delete</param>
        /// <remarks>
        /// If the list of property names is empty all attributes will be deleted (same behavior as <see cref="Delete{T}(object)"/>).
        /// </remarks>
        /// <exception cref="ArgumentNullException">If the itemName parameter is null</exception>
        /// <exception cref="ArgumentException">If the itemName type is invalid for the item type</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures </exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        public void DeleteAttributes<T>(object itemName, params string[] propertyNames)
        {
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof(T));
            Arg.CheckIsType("itemName", itemName, mapping.ItemNameMapping.PropertyType);
            Arg.CheckNull("propertyNames", propertyNames);

            simol.DeleteAttributes(mapping, new List<object> { itemName }, propertyNames.ToList());
        }

        /// <summary>
        /// Deletes specified attributes from multiple items in SimpleDB.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemNames">Item names of the items to delete</param>
        /// <param name="propertyNames">Names of properties to delete</param>
        /// <exception cref="ArgumentNullException">If the itemNames parameter is null</exception>
        /// <exception cref="ArgumentException">If the itemNames types are invalid for the item type</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// See <see cref="Delete{T}(IList)"/> for details on batch-put behavior.
        /// </remarks>
        public void DeleteAttributes<T>(IList itemNames, params string[] propertyNames)
        {
            Arg.CheckNull("itemNames", itemNames);
            if (itemNames.Count == 0)
            {
                return;
            }
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof(T));
            Arg.CheckIsType("itemNames[0]", itemNames[0], mapping.ItemNameMapping.PropertyType);
            Arg.CheckNull("propertyNames", propertyNames);

            simol.DeleteAttributes(mapping, itemNames.Cast<object>().ToList(), propertyNames.ToList());
        }

        /// <summary>
        /// Executes the specified select statement against SimpleDB using default select options.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selectStatement">The select statement to execute.</param>
        /// <param name="selectParams">The select parameter values.</param>
        /// <returns>The items returned by the select command or an empty list if no items were returned</returns>
        /// <remarks>
        /// Care should be taken when selecting against potentially large result sets as <em>all</em>
        /// available results are returned.
        /// </remarks>
        /// <exception cref="ArgumentNullException">If the selectStatement parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the selectStatement parameter is empty</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the SimpleDB item cannot be converted into the requested item type</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the requested item type</exception>
        public List<T> Select<T>(string selectStatement, params CommandParameter[] selectParams)
        {
            Arg.CheckNullOrEmpty("selectStatement", selectStatement);
            Arg.CheckNull("selectParams", selectParams);

            var command = new SelectCommand(typeof(T), selectStatement, selectParams);
            SelectResults<PropertyValues> results = simol.SelectAttributes(command);
            var items = new List<T>();
            foreach (PropertyValues values in results)
            {
                var item = (T)PropertyValues.CreateItem(typeof(T), values);
                items.Add(item);
            }
            return items;
        }

        /// <summary>
        /// Executes the specified select statement against SimpleDB using default select options
        /// and returns a list of item values.
        /// </summary>
        /// <typeparam name="T">Item type which defines mapping and formatting rules to use
        /// on return attributes</typeparam>
        /// <param name="selectStatement">The select statement to execute</param>
        /// <param name="selectParams">The select parameter values</param>
        /// <returns>
        /// The values returned by the select command or an emtpy list if no values were returned
        /// </returns>
        /// <exception cref="ArgumentNullException">If the selectStatement parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the selectStatement parameter is empty</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// Care should be taken when selecting against potentially large result sets as <em>all</em>
        /// available results are returned.
        /// </remarks>
        public List<PropertyValues> SelectAttributes<T>(string selectStatement, params CommandParameter[] selectParams)
        {
            Arg.CheckNullOrEmpty("selectStatement", selectStatement);
            Arg.CheckNull("selectParams", selectParams);

            var command = new SelectCommand(typeof(T), selectStatement, selectParams);

            SelectResults<PropertyValues> valuesResults = simol.SelectAttributes(command);
            return valuesResults.Items;
        }

        /// <summary>
        /// Executes the specified select statement against SimpleDB
        /// using advanced options provided by the command object.
        /// </summary>
        /// <typeparam name="T">Type of returned items</typeparam>
        /// <param name="command">The command to execute.</param>
        /// <returns>
        /// The results returned by the select command or an empty results list if no items were returned.
        /// </returns>
        /// <remarks>
        /// Care should be taken when selecting against potentially large result sets as <em>all</em>
        /// available results are returned by default. Use <see cref="SelectCommand.MaxResultPages"/> and <see cref="SelectCommand.PaginationToken"/> 
        /// to precisely control the number of results returned for each request.
        /// </remarks>
        /// <exception cref="ArgumentNullException">If the command parameter is null</exception>
        /// <exception cref="ArgumentException">If the command parameter has already been cancelled</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the SimpleDB item cannot be converted into the requested item type</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the requested item type</exception>
        public SelectResults<T> Select<T>(SelectCommand<T> command)
        {
            Arg.CheckNull("command", command);
            Arg.CheckCondition("command", !command.IsCancelled, "Command was already cancelled");

            SelectResults<PropertyValues> valuesResults = simol.SelectAttributes(command);
            var itemResults = new SelectResults<T>
                {
                    PaginationToken = valuesResults.PaginationToken,
                    WasCommandCancelled = valuesResults.WasCommandCancelled
                };
            foreach (PropertyValues values in valuesResults)
            {
                var item = (T)PropertyValues.CreateItem(typeof(T), values);
                itemResults.Items.Add(item);
            }
            return itemResults;
        }

        /// <summary>
        /// Executes the specified select statement against SimpleDB
        /// using advanced options provided by the command object.
        /// </summary>
        /// <typeparam name="T">Item type which defines mapping and formatting rules to use
        /// on return attributes</typeparam>
        /// <param name="command">The command to execute.</param>
        /// <returns>The results returned by the select command or an empty results list if no items were returned.</returns>
        /// <exception cref="ArgumentNullException">If the command parameter is null</exception>
        /// <exception cref="ArgumentException">If the command parameter has already been cancelled</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the requested item type</exception>
        public SelectResults<PropertyValues> SelectAttributes<T>(SelectCommand<T> command)
        {
            Arg.CheckNull("command", command);
            Arg.CheckCondition("command", !command.IsCancelled, "Command was already cancelled");

            return simol.SelectAttributes(command);
        }

        /// <summary>
        /// Executes the specified select statement against SimpleDB using default options
        /// and returns only the first attribute value in the result set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selectStatement">The select statement to execute.</param>
        /// <param name="selectParams">The select parameter values.</param>
        /// <returns>
        /// The first attribute of the first item in the result set or null if there are no results. 
        /// </returns>
        /// <exception cref="ArgumentNullException">If the selectStatement parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the selectStatement parameter is empty</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the SimpleDB item cannot be converted into the requested item type</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the requested item type</exception>
        /// <remarks>
        /// This method supports use of the SimpleDB "count" keyword. For example: <c>select count(*) from MyDomain</c>
        /// </remarks>
        public object SelectScalar<T>(string selectStatement, params CommandParameter[] selectParams)
        {
            Arg.CheckNullOrEmpty("selectStatement", selectStatement);
            Arg.CheckNull("selectParams", selectParams);

            var command = new SelectCommand(typeof(T), selectStatement, selectParams);
            return simol.SelectScalar(command);
        }

        /// <summary>
        /// Searches the full-text index with a specified query string and returns all items that still
        /// exist in SimpleDB.
        /// </summary>
        /// <typeparam name="T">The item type to return</typeparam>
        /// <param name="queryText">The search query text</param>
        /// <param name="resultStartIndex">The start index of items to return from the full-text index.</param>
        /// <param name="resultCount">The maximum number of items to return from the full-text index.</param>
        /// <param name="searchProperty">The default indexed property to search</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If the queryText parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the queryText parameter is empty</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the SimpleDB item cannot be converted into the requested item type</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the requested item type or no indexer is installed</exception>
        /// <exception cref="InvalidOperationException">If the requested item type has no indexed properties.</exception>
        /// <remarks>
        /// The search query string and search property are simply passed through to the underlying full-text engine
        /// through the configured <see cref="IIndexer"/>. Items found during the full-text search are retrieved from 
        /// SimpleDB using multiple threads. The returned items are ordered according to the ordering logic of the full-text engine. 
        /// The <see cref="LuceneIndexer"/> installed by default orders items by search score rank. 
        /// <para>
        /// Here are some query examples for use with the <c>LuceneIndexer</c>. To search the "Message" property for "Send the package"
        /// you have two options:
        /// <list type="bullet">
        /// <item>
        /// queryText=Send the package<br/>
        /// searchProperty=Message
        /// </item>
        /// <item>
        /// queryText=Message: Send the package<br/>
        /// searchProperty=null
        /// </item>        
        /// </list>
        /// To search both the "Address1" and "Address2" fields for "6523 Front St":
        /// <list type="bullet">
        /// <item>
        /// queryText=Address1:"6523 Front St"<br/>
        /// searchProperty="Address2"
        /// </item>
        /// <item>
        /// queryText=Address1:"6523 Front St" OR Address2:"6523 Front St"<br/>
        /// searchProperty=null
        /// </item>        
        /// </list>        
        /// </para>
        /// <para>You can find out more about the Lucene query syntax at 
        /// <a href="http://lucene.apache.org/java/2_4_0/queryparsersyntax.html">http://lucene.apache.org/java/2_4_0/queryparsersyntax.html</a>.
        /// </para>
        /// <para>
        /// <em><b>IMPORTANT:</b> <see cref="ConsistentReadScope"/> has no effect when used with the <c>Find</c> methods because all indexed 
        /// item retrievals are done asynchronously.</em>
        /// </para>
        /// </remarks>
        public List<T> Find<T>(string queryText, int resultStartIndex, int resultCount, string searchProperty)
        {
            Arg.CheckNullOrEmpty("queryText", queryText);
            Arg.CheckInRange("resultStartIndex", resultStartIndex, 0, int.MaxValue);
            Arg.CheckInRange("resultCount", resultCount, 1, int.MaxValue);

            List<PropertyValues> allValues = FindImpl<T>(queryText, resultStartIndex, resultCount, searchProperty);
            var items = new List<T>();

            foreach (PropertyValues values in allValues)
            {
                var item = (T)PropertyValues.CreateItem(typeof(T), values);
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Searches the full-text index with a specified query string and returns all items that still
        /// exist in SimpleDB.
        /// </summary>
        /// <typeparam name="T">The item type to return</typeparam>
        /// <param name="queryText">The search query text</param>
        /// <param name="resultStartIndex">The start index of items to return from the full-text index.</param>
        /// <param name="resultCount">The maximum number of items to return from the full-text index.</param>
        /// <param name="searchProperty">The default indexed property to search</param>
        /// <param name="propertyNames">Names of properties to get</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If the queryText parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the queryText parameter is empty</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the SimpleDB item cannot be converted into the requested item type</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the requested item type or no indexer is installed</exception>
        /// <exception cref="InvalidOperationException">If the requested item type has no indexed properties.</exception>
        /// <remarks>
        /// See <see cref="Find{T}"/> for more information.
        /// </remarks>
        public List<PropertyValues> FindAttributes<T>(string queryText, int resultStartIndex, int resultCount,
                                                      string searchProperty, params string[] propertyNames)
        {
            Arg.CheckNullOrEmpty("queryText", queryText);
            Arg.CheckInRange("resultStartIndex", resultStartIndex, 0, int.MaxValue);
            Arg.CheckInRange("resultCount", resultCount, 1, int.MaxValue);
            Arg.CheckNull("propertyNames", propertyNames);

            return FindImpl<T>(queryText, resultStartIndex, resultCount, searchProperty, propertyNames);
        }

        /// <summary>
        /// Gets an ad-hoc list of item values from SimpleDB without an item type generic parameter.
        /// </summary>
        /// <param name="mapping">The item mapping</param>
        /// <param name="itemName">Identifies the item for which attributes will be retrieved</param>
        /// <param name="propertyNames">List of properties whose values will be retrieved</param>
        /// <returns>
        /// The property values or null if no attributes exist for the specified item name.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the mapping or itemName parameters are null</exception>
        /// <exception cref="ArgumentException">If the itemName type is invalid for the item type</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// Supports retrieval of a subset of typed property values when
        /// you don't need or want to retrieve all attributes stored in a domain.
        /// </remarks>
        public PropertyValues GetAttributes(ItemMapping mapping, object itemName, params string[] propertyNames)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("itemName", itemName);
            Arg.CheckNull("propertyNames", propertyNames);

            return simol.GetAttributes(mapping, itemName, propertyNames.ToList());
        }

        /// <summary>
        /// Puts multiple property values collections into SimpleDB.
        /// </summary>
        /// <param name="mapping">The item mapping</param>
        /// <param name="items"></param>
        /// <exception cref="ArgumentNullException">If the values parameter is null</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the item values cannot be converted to SimpleDB attributes</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// Supports storage of arbitrary lists of item values without dependence on an item type.
        /// See <see cref="Put{T}"/> for details on batch-put behavior.
        /// </remarks>
        public void PutAttributes(ItemMapping mapping, List<PropertyValues> items)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("items", items);

            simol.PutAttributes(mapping, items);
        }

        /// <summary>
        /// Puts a single property values collection into SimpleDB.
        /// </summary>
        /// <param name="mapping">The item mapping.</param>
        /// <param name="item">The item</param>
        /// <exception cref="ArgumentNullException">If mapping or item parameters are null</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the item values cannot be converted to SimpleDB attributes</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// Supports storage of arbitrary lists of item values without dependence on an item type.
        /// See <see cref="Put{T}"/> for details on batch-put behavior.
        /// </remarks>
        public void PutAttributes(ItemMapping mapping, PropertyValues item)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("item", item);

            simol.PutAttributes(mapping, item.ToUniList());
        }

        /// <summary>
        /// Deletes an ad-hoc list of item values from SimpleDB without an item type generic parameter.
        /// </summary>
        /// <param name="mapping">The item mapping</param>
        /// <param name="itemName">Identifies the item from which to remove attributes</param>
        /// <param name="propertyNames">Names of properties to delete</param>
        /// <remarks>
        /// If the list of property names is empty all attributes will be deleted (same behavior as <see cref="Delete{T}(object)"/>).
        /// </remarks>
        /// <exception cref="ArgumentNullException">If the mapping or itemName parameters are null</exception>
        /// <exception cref="ArgumentException">If the itemName type is invalid for the item type</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures </exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        public void DeleteAttributes(ItemMapping mapping, object itemName, params string[] propertyNames)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("itemName", itemName);
            Arg.CheckNull("propertyNames", propertyNames);

            simol.DeleteAttributes(mapping, itemName.ToUniList(), propertyNames.ToList());
        }

        /// <summary>
        /// Deletes an ad-hoc list of item values from multiple items in SimpleDB without an item type generic parameter.
        /// </summary>
        /// <param name="mapping">The item mapping</param>
        /// <param name="itemNames">Item names of the items to delete</param>
        /// <param name="propertyNames">Names of properties to delete</param>
        /// <exception cref="ArgumentNullException">If the mapping or itemNames parameters are null</exception>
        /// <exception cref="ArgumentException">If the itemNames types are invalid for the item type</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the specified item type</exception>
        /// <remarks>
        /// See <see cref="Delete{T}(IList)"/> for details on batch-put behavior.
        /// </remarks>
        public void DeleteAttributes(ItemMapping mapping, IList itemNames, params string[] propertyNames)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNull("itemNames", itemNames);
            Arg.CheckNull("propertyNames", propertyNames);

            simol.DeleteAttributes(mapping, itemNames.Cast<object>().ToList(), propertyNames.ToList());
        }

        /// <summary>
        /// Executes the specified select command against SimpleDB
        /// using advanced options provided by the command object without an item type generic parameter.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>
        /// The results returned by the select command or an empty results list if no items were returned.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the command parameter is null</exception>
        /// <exception cref="ArgumentException">If the command parameter has already been cancelled</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If there are any property value conversion failures</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the requested item type</exception>
        public SelectResults<PropertyValues> SelectAttributes(SelectCommand command)
        {
            Arg.CheckNull("command", command);

            return simol.SelectAttributes(command);
        }

        /// <summary>
        /// Executes the specified select command against SimpleDB
        /// without an item type generic parameter and returns only the first attribute 
        /// value in the result set.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>
        /// The first attribute of the first item in the result set or null if there are no results.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the command parameter is null</exception>
        /// <exception cref="AmazonSimpleDBException">If the request to SimpleDB fails for any reason</exception>
        /// <exception cref="SimolDataException">If the SimpleDB item cannot be converted into the requested item type</exception>
        /// <exception cref="SimolConfigurationException">If no valid SimpleDB mapping can be configured
        /// for the requested item type</exception>
        /// <remarks>
        /// This method supports use of the SimpleDB "count" keyword. For example: <c>select count(*) from MyDomain</c>
        /// </remarks>
        public object SelectScalar(SelectCommand command)
        {
            Arg.CheckNull("command", command);

            return simol.SelectScalar(command);
        }

        private List<PropertyValues> FindImpl<T>(string queryText, int resultStartIndex, int resultCount,
                                                 string searchProperty, params string[] propertyNames)
        {
            ItemMapping mapping = TypeItemMapping.GetMapping(typeof(T));
            if (!mapping.AttributeMappings.Where(a => a.IsIndexed).Any())
            {
                string message =
                    string.Format(
                        @"Unable to find items of type '{0}' because the type has no indexed properties. At least one property must be marked with an {1}.",
                        typeof(T).FullName, typeof(IndexAttribute).Name);
                throw new InvalidOperationException(message);
            }
            if (Config.Indexer == null)
            {
                string message =
                    @"No full-text indexer is installed. You must provide an indexer via SimolConfig.Indexer before invoking the 'Find' methods.";
                throw new SimolConfigurationException(message);
            }

            List<string> itemNames = Config.Indexer.FindItems(mapping.DomainName, queryText,
                                                              resultStartIndex, resultCount, searchProperty);
            var items = new List<PropertyValues>();

            var results = new List<IAsyncResult>();
            foreach (string itemNameStr in itemNames)
            {
                object itemName = MappingUtils.StringToPropertyValue(Config.Formatter, mapping.ItemNameMapping,
                                                                     itemNameStr);
                IAsyncResult result = this.BeginGetAttributes(mapping, itemName, propertyNames, null, null);
                results.Add(result);
            }
            foreach (IAsyncResult r in results)
            {
                PropertyValues item = ((ISimol)this).EndGetAttributes(r);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private void Init(AmazonSimpleDB simpleDb, SimolConfig config)
        {
            Config = config;

            // decorate SimpleDb instance
            simpleDb = new ConsistentSimpleDB(simpleDb, config);
            SimpleDB = simpleDb;

            // decorate Simol instance
            simol = new SimpleDbSimol(Config, SimpleDB);
            if (Config.Cache != null)
            {
                simol = new CachingSimol(simol);
            }
            if (Config.AutoCreateDomains)
            {
                simol = new DomainCreatingSimol(simol);
            }
            simol = new ConstrainingSimol(simol);
        }
    }
}