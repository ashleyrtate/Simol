/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using Amazon.SimpleDB;
using System.Collections.Generic;

namespace Simol.Core
{
    /// <summary>
    /// Internal interface for exposing additional properties and methods to core classes.
    /// </summary>
    internal interface ISimolInternal 
    {
        AmazonSimpleDB SimpleDB { get; }

        SimolConfig Config { get; }

        /// <summary>
        /// Puts a list of item values into SimpleDB.
        /// </summary>
        void PutAttributes(ItemMapping mapping, List<PropertyValues> values);

        /// <summary>
        /// Gets an ad-hoc list of item values from SimpleDB without an item type generic parameter.
        /// </summary>
        PropertyValues GetAttributes(ItemMapping mapping, object itemName, List<string> propertyNames);

        /// <summary>
        /// Deletes an ad-hoc list of item values from SimpleDB without an item type generic parameter.
        /// </summary>
        void DeleteAttributes(ItemMapping mapping, List<object> itemNames, List<string> propertyNames);

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
    }
}