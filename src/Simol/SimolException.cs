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
    /// Base Simol exception class.
    /// </summary>
    public abstract class SimolException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimolException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        protected SimolException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimolException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        protected SimolException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}