/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.ComponentModel;

namespace Simol
{
    /// <summary>
    /// Defines methods for converting between typed values and strings.
    /// </summary>
    /// <remarks>
    /// The .NET class libraries provide several standard classes and interfaces for performing type conversion
    /// (<see cref="TypeConverter"/>, <see cref="IConvertible"/>, and <see cref="IFormattable"/>). 
    /// But these standard conversion patterns are not ideal for SimpleDB formatting. They are either overly complex, 
    /// coupled to specific implementation classes, or designed for one-way conversion to strings. <c>ITypeFormatter</c>
    /// provides a simple interface for defining arbitrary, bi-directional conversion between strings and .NET Types.
    /// </remarks>
    public interface ITypeFormatter
    {
        /// <summary>
        /// Converts an object to a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted object</returns>
        /// <exception cref="ArgumentNullException">If the value parameter is null</exception>
        /// <exception cref="ArgumentException">If the value parameter is an unsupported type</exception>
        string ToString(object value);

        /// <summary>
        /// Converts a string to a specified type. 
        /// </summary>
        /// <param name="valueString">The string value to convert.</param>
        /// <param name="expected">The expected type of the returned object.</param>
        /// <returns>The converted object or null</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="valueString"/> parameter is null</exception>
        /// <exception cref="ArgumentException">If the <paramref name="expected"/> parameter is an unsupported type</exception>
        object ToType(string valueString, Type expected);
    }
}