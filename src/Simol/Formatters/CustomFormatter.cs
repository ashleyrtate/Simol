/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Globalization;
using Coditate.Common.Util;

namespace Simol.Formatters
{
    /// <summary>
    /// Used by <see cref="PropertyFormatter"/> for custom conversions between strings and arbitrary types. 
    /// </summary>
    internal class CustomFormatter : FormatterBase
    {
        /// <summary>
        /// Gets or sets the custom formatter.
        /// </summary>
        /// <value>The formatter.</value>
        public ITypeFormatter Formatter { get; set; }

        public override string ToString(object value)
        {
            State.CheckTrue(Format == null && Formatter == null, "'Format' and 'Formatter' are both null");

            string stringValue;
            if (Formatter != null)
            {
                stringValue = Formatter.ToString(value);
            }
            else
            {
                Arg.CheckIsType("value", value, typeof (IFormattable));

                stringValue = ((IFormattable) value).ToString(Format, CultureInfo.InvariantCulture);
            }
            return stringValue;
        }

        public override object ToType(string valueString, Type expected)
        {
            State.CheckTrue(Format == null && Formatter == null, "'Format' and 'Formatter' are both null");

            object value;
            if (Formatter != null)
            {
                value = Formatter.ToType(valueString, expected);
            }
            else
            {
                value = Convert.ChangeType(valueString, expected, CultureInfo.InvariantCulture);
            }
            return value;
        }
    }
}