/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;

namespace Simol.Formatters
{
    /// <summary>
    /// Base class for all formatters used by <see cref="PropertyFormatter"/>.
    /// </summary>
    internal abstract class FormatterBase : ITypeFormatter
    {
        /// <summary>
        /// Gets or sets the string format to use with <see cref="IFormattable"/> types.
        /// </summary>
        /// <value>The format.</value>
        public virtual string Format { get; set; }

        public abstract string ToString(object value);

        public abstract object ToType(string valueString, Type expected);
    }
}