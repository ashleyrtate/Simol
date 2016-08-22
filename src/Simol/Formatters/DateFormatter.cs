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
    /// Used by <see cref="PropertyFormatter"/> for converting between <see cref="DateTime"/>s
    /// and strings.
    /// </summary>
    internal sealed class DateFormatter : FormatterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateFormatter"/> class.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="style">The style.</param>
        public DateFormatter(string format, IFormatProvider provider, DateTimeStyles style)
        {
            Arg.CheckNull("format", format);
            Arg.CheckNull("provider", provider);
            Arg.CheckNull("style", style);

            Format = format;
            FormatProvider = provider;
            FormatStyle = style;
        }

        /// <summary>
        /// Gets or sets the format provider to use when parsing date strings.
        /// </summary>
        /// <value>The format provider.</value>
        public IFormatProvider FormatProvider { get; private set; }

        /// <summary>
        /// Gets or sets the format style to use when parsing date strings.
        /// </summary>
        /// <value>The format style.</value>
        public DateTimeStyles FormatStyle { get; private set; }

        public override string ToString(object value)
        {
            Arg.CheckIsType("value", value, typeof (DateTime));

            var dt = (DateTime) value;
            return dt.ToString(Format, FormatProvider);
        }

        public override object ToType(string value, Type expected)
        {
            Arg.CheckNullOrEmpty("value", value);
            Arg.CheckIsAssignableTo("expected", expected, typeof (DateTime));

            return DateTime.ParseExact(value, Format, FormatProvider, FormatStyle);
        }
    }
}