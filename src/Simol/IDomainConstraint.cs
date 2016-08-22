/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using Simol.Consistency;
using System.Collections.Generic;

namespace Simol
{
    /// <summary>
    /// Defines methods for plugging custom data constraint/validation logic into the 
    /// Simol load/save cycle.
    /// </summary>
    /// <remarks>
    /// It is acceptable to manipulate attribute values passed to the methods of  
    /// implementing classes <em>or</em> to throw exceptions to prevent the storing, loading, or deleting
    /// of data. Exceptions thrown in these methods will be passed up to the 
    /// caller of the original <see cref="SimolClient"/> method.
    /// </remarks>
    /// <seealso cref="ConstraintAttribute"/>
    /// <seealso cref="DomainConstraintBase"/>
    public interface IDomainConstraint
    {
        /// <summary>
        /// This method is invoked once per item for all <see cref="SimolClient"/>
        /// operations which load data from SimpleDB.
        /// </summary>
        /// <remarks>
        /// This method is invoked once for each item returned by a call to <see cref="SimolClient.Get{T}"/>, 
        /// <see cref="SimolClient.GetAttributes"/>, <see cref="SimolClient.GetAttributes{T}"/>, 
        /// <see cref="SimolClient.Select{T}(SelectCommand{T})"/>, <see cref="SimolClient.SelectAttributes{T}(SelectCommand{T})"/>,
        /// <see cref="SimolClient.SelectAttributes(SelectCommand)"/>, etc. In other words, call to <c>Simol.Select</c> which 
        /// returned 100 items would result in 100 invocations of this method, once for each item.
        /// </remarks>
        /// <param name="values">The property values for a single item.</param>
        void AfterLoad(PropertyValues values);

        /// <summary>
        /// This method is invoked once per item for all <see cref="SimolClient"/>
        /// operations which save data to SimpleDB.
        /// </summary>
        /// <remarks>
        /// This method is invoked once for each item passed to a call to <see cref="SimolClient.Put"/>, 
        /// <see cref="SimolClient.PutAttributes{T}(PropertyValues)"/>, etc. In other words, call to <c>Simol.Put</c>  
        /// passing 100 items would result in 100 invocations of this method, once for each item.
        /// </remarks>
        /// <param name="values">The property values for a single item.</param>
        void BeforeSave(PropertyValues values);

        /// <summary>
        /// This method is invoked once per item for all <see cref="SimolClient"/>
        /// operations which delete data from SimpleDB.
        /// </summary>
        /// <remarks>
        /// This method is invoked once for each item passed to <see cref="SimolClient.Delete{T}(object)"/>, 
        /// <see cref="ISimol.DeleteAttributes(ItemMapping,object,string[])"/>, etc.
        /// </remarks>
        /// <param name="itemName">Name of the item being deleted.</param>
        /// <param name="propertyNames">The property names of the attributes being deleted.</param>
        void BeforeDelete(object itemName, List<string> propertyNames);
    }
}