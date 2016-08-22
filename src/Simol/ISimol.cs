/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System.Collections.Generic;
using System.Collections;
using Amazon.SimpleDB;

namespace Simol 
{
    /// <summary>
    /// Defines the Simol methods for interacting with SimpleDB.
    /// </summary>
    /// <seealso cref="SimolClient"/>
    /// <seealso cref="SimolConfig"/>
    /// <seealso cref="SelectCommand"/>
    /// <seealso cref="ISimol"/>
    /// <remarks>
    /// See the documentation on <see cref="SimolClient"/> for more detailed usage instructions.
    /// </remarks>
    public interface ISimol 
    {
        /// <summary>
        /// Puts the specified item into SimpleDB.
        /// </summary>
        void Put(object item);

        /// <summary>
        /// Puts multiple items into SimpleDB.
        /// </summary>
        void Put<T>(List<T> items);

        /// <summary>
        /// Puts multiple property values collections into SimpleDB.
        /// </summary>
        void PutAttributes<T>(List<PropertyValues> items);

        /// <summary>
        /// Puts a property values collection into SimpleDB.
        /// </summary>
        void PutAttributes<T>(PropertyValues item);

        /// <summary>
        /// Gets an item from SimpleDB.
        /// </summary>
        T Get<T>(object itemName);

        /// <summary>
        /// Gets a property values collection from SimpleDB.
        /// </summary>
        PropertyValues GetAttributes<T>(object itemName, params string[] propertyNames);

        /// <summary>
        /// Deletes an item from SimpleDB (all attributes).
        /// </summary>
        void Delete<T>(object itemName);

        /// <summary>
        /// Deletes multiple items from SimpleDB (all attributes).
        /// </summary>
        void Delete<T>(IList itemNames);

        /// <summary>
        /// Deletes specified attributes from multiple items in SimpleDB.
        /// </summary>
        void DeleteAttributes<T>(IList itemNames, params string[] propertyNames);

        /// <summary>
        /// Deletes specified attributes from a single item in SimpleDB.
        /// </summary>
        void DeleteAttributes<T>(object itemName, params string[] propertyNames);

        /// <summary>
        /// Executes the specified select statement against SimpleDB using default options
        /// and returns a list of items.
        /// </summary>
        List<T> Select<T>(string selectStatement, params CommandParameter[] selectParams);

        /// <summary>
        /// Executes the specified select statement against SimpleDB using default options
        /// and returns a list of item values.
        /// </summary>
        List<PropertyValues> SelectAttributes<T>(string selectStatement, params CommandParameter[] selectParams);

        /// <summary>
        /// Executes the specified select statement against SimpleDB
        /// using advanced options provided by the command object.
        /// </summary>
        SelectResults<T> Select<T>(SelectCommand<T> command);

        /// <summary>
        /// Executes the specified select statement against SimpleDB
        /// using advanced options provided by the command object.
        /// </summary>
        SelectResults<PropertyValues> SelectAttributes<T>(SelectCommand<T> command);

        /// <summary>
        /// Executes the specified select statement against SimpleDB using default options
        /// and returns only the first attribute value in the result set.
        /// </summary>
        object SelectScalar<T>(string selectStatement, params CommandParameter[] selectParams);

        /// <summary>
        /// Searches the full-text index with a specified query string and returns all items that still
        /// exist in SimpleDB.
        /// </summary>
        /// <returns></returns>
        List<T> Find<T>(string queryText, int resultStartIndex, int resultCount, string searchProperty);

        /// <summary>
        /// Searches the full-text index with a specified query string and returns all items that still
        /// exist in SimpleDB.
        /// </summary>
        /// <returns></returns>
        List<PropertyValues> FindAttributes<T>(string queryText, int resultStartIndex, int resultCount,
                                               string searchProperty, params string[] propertyNames);

        /// <summary>
        /// Puts multiple property values collections into SimpleDB.
        /// </summary>
        void PutAttributes(ItemMapping mapping, List<PropertyValues> items);

        /// <summary>
        /// Puts a single property values collection into SimpleDB.
        /// </summary>
        void PutAttributes(ItemMapping mapping, PropertyValues item);

        /// <summary>
        /// Gets an ad-hoc list of item values from SimpleDB without an item type generic parameter.
        /// </summary>
        PropertyValues GetAttributes(ItemMapping mapping, object itemName, params string[] propertyNames);

        /// <summary>
        /// Deletes an ad-hoc list of item values from SimpleDB without an item type generic parameter.
        /// </summary>
        void DeleteAttributes(ItemMapping mapping, object itemName, params string[] propertyNames);

        /// <summary>
        /// Deletes an ad-hoc list of item values from multiple items in SimpleDB without an item type generic parameter.
        /// </summary>
        void DeleteAttributes(ItemMapping mapping, IList itemNames, params string[] propertyNames);

        /// <summary>
        /// Executes the specified select command against SimpleDB
        /// using advanced options provided by the command object without an item type generic parameter.
        /// </summary>
        SelectResults<PropertyValues> SelectAttributes(SelectCommand command);

        /// <summary>
        /// Executes the specified select command against SimpleDB
        /// without an item type generic parameter and returns only the first attribute 
        /// value in the result set.
        /// </summary>
        object SelectScalar(SelectCommand command);

        /// <summary>
        /// Gets the Simol configuration object.
        /// </summary>
        /// <value>The configuration.</value>
        SimolConfig Config { get; }

        /// <summary>
        /// Gets the Amazon SimpleDB interface.
        /// </summary>
        /// <value>The simple DB.</value>
        AmazonSimpleDB SimpleDB { get; }
    }
}