/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;

namespace Simol.Data
{
    /// <summary>
    /// Persistent object that stores information about one step of a reliable-write operation.
    /// </summary>
    internal class ReliableWriteStep : SystemData
    {
        /// <summary>
        /// Gets or sets the reliable write id.
        /// </summary>
        /// <value>The reliable write id.</value>
        public Guid ReliableWriteId { get; set; }

        /// <summary>
        /// Gets or sets the simple DB request.
        /// </summary>
        /// <value>The simple DB request.</value>
        [Span(true), CustomFormat(typeof (SimpleDBRequestFormatter))]
        public object SimpleDBRequest { get; set; }
    }
}