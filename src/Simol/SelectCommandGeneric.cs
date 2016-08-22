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
    /// Encapsulates advanced options for making select requests that are strongly typed with a generic parameter.
    /// </summary>
    /// <typeparam name="T">The mapped item type with which this command will be used</typeparam>
    /// <remarks>
    /// This class is simply a generic version of <see cref="SelectCommand"/>. The item mapping
    /// is determined from the generic type parameter rather than from an <see cref="ItemMapping"/>
    /// or <see cref="Type"/> passed to the constructor. See the base class for more detailed information
    /// on usage and behavior.
    /// </remarks>
    public class SelectCommand<T> : SelectCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectCommand{T}"/> class.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="parameters">The command parameters.</param>
        public SelectCommand(string commandText, params CommandParameter[] parameters)
            : base(typeof (T), commandText, parameters)
        {
        }
    }
}