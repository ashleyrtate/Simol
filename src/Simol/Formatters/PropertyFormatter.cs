/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using Coditate.Common.Util;

namespace Simol.Formatters
{
    /// <summary>
    /// Registry of formatters used by Simol for property conversion.
    /// </summary>
    /// <remarks>
    /// Custom <see cref="ITypeFormatter"/>s registered here will be used for <em>all</em> conversions
    /// of the specified property type, except for properties marked by a <see cref="SimolFormatAttribute"/>.
    /// </remarks>
    public class PropertyFormatter
    {
        // as of version 1.03 of the AWS SDK the null string is returned base-64 encoded
        internal const string Base64NullString = "AA==";
        internal const string NullString = "\0";

        private static readonly FormatterBase DefaultFormatter = new ToStringFormatter();
        private static readonly PropertyFormatter DefaultInstance = new PropertyFormatter(new SimolConfig());

        private readonly Dictionary<Type, ITypeFormatter> typeFormatters =
            new Dictionary<Type, ITypeFormatter>();

        private SimolConfig config;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyFormatter"/> class.
        /// </summary>
        internal PropertyFormatter(SimolConfig config)
        {
            Arg.CheckNull("config", config);

            this.config = config;
            SetFormatter(typeof (byte),
                         new NumberFormatter {WholeDigits = 3, IsSigned = false, ApplyOffset = false});
            SetFormatter(typeof (sbyte), new NumberFormatter {WholeDigits = 3});
            SetFormatter(typeof (short), new NumberFormatter {WholeDigits = 5});
            SetFormatter(typeof (ushort),
                         new NumberFormatter {WholeDigits = 5, IsSigned = false, ApplyOffset = false});
            SetFormatter(typeof (int), new NumberFormatter {WholeDigits = 10});
            SetFormatter(typeof (uint),
                         new NumberFormatter {WholeDigits = 10, IsSigned = false, ApplyOffset = false});

            SetFormatter(typeof (long), new NumberFormatter {WholeDigits = 19});
            SetFormatter(typeof (ulong),
                         new NumberFormatter {WholeDigits = 20, IsSigned = false, ApplyOffset = false});
            SetFormatter(typeof (float), new NumberFormatter {WholeDigits = 3, DecimalDigits = 4});
            SetFormatter(typeof (double), new NumberFormatter {WholeDigits = 7, DecimalDigits = 8});
            SetFormatter(typeof (decimal), new NumberFormatter {WholeDigits = 18, DecimalDigits = 10});
            SetFormatter(typeof (DateTime),
                         new DateFormatter("yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture,
                                           DateTimeStyles.RoundtripKind));
            SetFormatter(typeof (Guid), new CustomFormatter {Formatter = new GuidFormatter()});
            SetFormatter(typeof (Enum), new CustomFormatter {Formatter = new EnumFormatter()});
            SetFormatter(typeof (TimeSpan), new CustomFormatter {Formatter = new TimeSpanFormatter()});
        }

        /// <summary>
        /// Registers a formatter for the specified type.
        /// </summary>
        /// <param name="propertyType">The type to format.</param>
        /// <param name="formatter">The formatter.</param>
        /// <remarks>
        /// Replaces any formatter previously registered for the property type.
        /// </remarks>
        public void SetFormatter(Type propertyType, ITypeFormatter formatter)
        {
            var numberFormatter = formatter as NumberFormatter;
            if (numberFormatter != null)
            {
                numberFormatter.ApplyOffset = (numberFormatter.IsSigned && config.OffsetNumbers == Offset.Signed ||
                                               config.OffsetNumbers == Offset.All);
            }
            typeFormatters[propertyType] = formatter;
        }

        /// <summary>
        /// Gets the formatter registered for the specified type.
        /// </summary>
        /// <param name="propertyType">The type to format.</param>
        /// <returns>The registered formatter or null if none exists.</returns>
        public ITypeFormatter GetFormatter(Type propertyType)
        {
            Type registeredType = propertyType;
            if (propertyType.IsEnum)
            {
                registeredType = typeof (Enum);
            }
            ITypeFormatter formatter;
            typeFormatters.TryGetValue(registeredType, out formatter);
            if (formatter == null)
            {
                formatter = DefaultFormatter;
            }

            return formatter;
        }

        /// <summary>
        /// Converts an object to a string using the specified formatter.
        /// </summary>
        /// <param name="formatter">The formatter to use. If null, the default formatter
        /// for the value type is used.</param>
        /// <param name="value">The value to format.</param>
        /// <returns></returns>
        internal string ToString(ITypeFormatter formatter, object value)
        {
            if (value == null)
            {
                return NullString;
            }
            if (formatter == null)
            {
                formatter = GetFormatter(value.GetType());
            }

            return formatter.ToString(value);
        }

        /// <summary>
        /// Converts a string to the requested type using the specified formatter.
        /// </summary>
        /// <param name="formatter">The formatter to use. If null, the default
        /// formatter for the expected type is used.</param>
        /// <param name="valueString">The value string to convert.</param>
        /// <param name="expected">The expected return type.</param>
        /// <returns></returns>
        internal object ToType(ITypeFormatter formatter, string valueString, Type expected)
        {
            Arg.CheckNull("expected", expected);

            if (valueString == null || valueString == Base64NullString)
            {
                return null;
            }
            if (formatter == null)
            {
                formatter = GetFormatter(expected);
            }

            return formatter.ToType(valueString, expected);
        }

        /// <summary>
        /// Gets the default formatter for the specified type.
        /// </summary>
        /// <param name="propertyType">Type for which to retrieve a formatter.</param>
        /// <returns></returns>
        internal static ITypeFormatter GetDefaultFormatter(Type propertyType)
        {
            return DefaultInstance.GetFormatter(propertyType);
        }

        /// <summary>
        /// Implements default type formatting and conversion logic.
        /// </summary>
        private class ToStringFormatter : FormatterBase
        {
            public override string ToString(object value)
            {
                return value.ToString();
            }

            public override object ToType(string valueString, Type expected)
            {
                return Convert.ChangeType(valueString, expected, CultureInfo.InvariantCulture);
            }
        }
    }
}