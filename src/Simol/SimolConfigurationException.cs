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
    /// Exception thrown when <see cref="SimolClient"/> is used with invalid combinations of configuration settings,
    /// item types, or custom attributes.
    /// </summary>
    public class SimolConfigurationException : SimolException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimolConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public SimolConfigurationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimolConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public SimolConfigurationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}