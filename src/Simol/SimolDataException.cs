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
    /// Exception thrown when <see cref="SimolClient"/> fails to convert SimpleDB items into requested .NET types
    /// or vice versa.
    /// </summary>
    public class SimolDataException : SimolException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimolDataException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The source exception.</param>
        public SimolDataException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimolDataException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public SimolDataException(string message)
            : base(message)
        {
        }
    }
}