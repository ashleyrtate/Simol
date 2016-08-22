/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using Simol.Indexing;

namespace Simol.Data
{
    /// <summary>
    /// Persistent object that stores information about the state of full-text indexes in the SimolSystem domain.
    /// </summary>
    internal class IndexState : SystemData
    {
        /// <summary>
        /// Gets or sets the 
        /// </summary>
        /// <value>The name of the domain.</value>
        public string DomainName { get; set; }

        /// <summary>
        /// Gets or sets the version (datetime) of the last
        /// item indexed by the <see cref="IndexBuilder"/>.
        /// </summary>
        /// <value>The last indexed version.</value>
        public DateTime LastIndexedVersion { get; set; }
    }
}