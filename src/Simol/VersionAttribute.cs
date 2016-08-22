/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;

namespace Simol
{

    /// <summary>
    /// Defines options for how a version property behaves when updating items in SimpleDB.
    /// </summary>
    public enum VersioningBehavior
    {
        /// <summary>
        /// Apply no custom behavior. The property is marked as a version but the application is responsible
        /// for implementing all versioning-related behavior. This is primarily useful for 
        /// working with full-text indexed items that don't otherwise require any versioning behavior.
        /// </summary>
        None,
        /// <summary>
        /// Simol automatically increments the version property when updating the item
        /// in SimpleDB. Note that the version is incremented on the outgoing PutAttributes request
        /// but not on the version item or <see cref="PropertyValues"/> collection passed to Simol.
        /// </summary>
        AutoIncrement,
        /// <summary>
        /// Simol automatically increments the version property when updating the item
        /// in SimpleDB AND instructs SimpleDB to reject the update if the item has already been updated
        /// by another process (using the SimpleDB conditional update setting). 
        /// </summary>
        AutoIncrementAndConditionallyUpdate
    }
    
    /// <summary>
    /// Supports designation of a property for item versioning.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class VersionAttribute : SimolAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionAttribute"/> class.
        /// </summary>
        /// <param name="versioning">The versioning.</param>
        public VersionAttribute(VersioningBehavior versioning)
        {
            Versioning = versioning;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionAttribute"/> class.
        /// </summary>
        public VersionAttribute() :this (VersioningBehavior.None)
        {
        }

        /// <summary>
        /// Gets or sets the versioning behavior to use.
        /// </summary>
        /// <value>The versioning behavior.</value>
        public VersioningBehavior Versioning
        {
            get;
            private set;
        }
    }
}