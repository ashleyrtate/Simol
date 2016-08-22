/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Simol.Formatters
{
    /// <summary>
    /// Used by <see cref="PropertyFormatter"/> for converting between standard numeric types
    /// and strings.
    /// </summary>
    /// <remarks>
    /// This formatter is used internally by Simol when the <see cref="NumberFormatAttribute"/>
    /// is applied to an item property. It may be used directly by applications building 
    /// dynamic <see cref="ItemMapping"/>s.
    /// </remarks>
    public class NumberFormatter : ITypeFormatter
    {
        private static readonly Dictionary<string, object> FloatingOverflows = new Dictionary<string, object>
            {
                {"Single.NaN", Single.NaN},
                {"Single.-Infinity", Single.NegativeInfinity},
                {"Single.Infinity", Single.PositiveInfinity},
                {"Double.NaN", Double.NaN},
                {"Double.-Infinity", Double.NegativeInfinity},
                {"Double.Infinity", Double.PositiveInfinity},
            };

        private string format;
        private decimal? offsetAmount;

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberFormatter"/> class.
        /// </summary>
        public NumberFormatter()
        {
            ApplyOffset = true;
            IsSigned = true;
        }

        /// <summary>
        /// Gets or sets the whole digits in the number to be formatting.
        /// </summary>
        /// <value>The whole digits.</value>
        public byte WholeDigits { get; set; }

        /// <summary>
        /// Gets or sets the decimal digits in the number to be formatting.
        /// </summary>
        /// <value>The decimal digits.</value>
        public byte DecimalDigits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the number to be formatted is signed.
        /// </summary>
        /// <value><c>true</c> if the number is signed; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// </remarks>
        public bool IsSigned { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a negative number format should be applied during formatting.
        /// </summary>
        /// <value><c>true</c> if an offset should be applied; otherwise, <c>false</c>.</value>
        public bool ApplyOffset { get; set; }

        /// <summary>
        /// Gets an offset amount calculated from the value of <see cref="WholeDigits"/>.
        /// </summary>
        /// <value>The offset amount.</value>
        public decimal OffsetAmount
        {
            get
            {
                if (offsetAmount == null)
                {
                    offsetAmount = GetOffsetAmount(WholeDigits);
                }
                return (decimal) offsetAmount;
            }
        }

        internal string Format
        {
            get
            {
                if (format == null)
                {
                    format = GetFormatString(WholeDigits, DecimalDigits, ApplyOffset);
                }
                return format;
            }
        }

        /// <summary>
        /// Converts a numeric value to a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted object</returns>
        /// <exception cref="ArgumentNullException">If the value parameter is null</exception>
        /// <exception cref="ArgumentException">If the value parameter is an unsupported type</exception>
        public virtual string ToString(object value)
        {
            var formattable = (IFormattable) value;

            if (ApplyOffset)
            {
                try
                {
                    var adjusted = (decimal)Convert.ChangeType(value, typeof(decimal), CultureInfo.InvariantCulture);
                    adjusted += OffsetAmount;
                    formattable = adjusted;
                }
                catch (OverflowException)
                {
                    string valueString;
                    if (!TryFormatOverflow(formattable, out valueString))
                    {
                        throw;
                    }
                    return valueString;
                }
            }
            return formattable.ToString(Format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a string to a numeric value. 
        /// </summary>
        /// <param name="valueString">The string value to convert.</param>
        /// <param name="expected">The expected type of the returned object.</param>
        /// <returns>The converted object or null</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="valueString"/> parameter is null</exception>
        /// <exception cref="ArgumentException">If the <paramref name="expected"/> parameter is an unsupported type</exception>
        public virtual object ToType(string valueString, Type expected)
        {
            object value = valueString;
            if (ApplyOffset)
            {
                try
                {
                    var adjusted = (decimal) Convert.ChangeType(valueString, typeof (decimal), CultureInfo.InvariantCulture);
                    adjusted -= OffsetAmount;
                    value = adjusted;
                }
                catch (FormatException)
                {
                    if (!TryParseOverflow(valueString, expected, out value))
                    {
                        throw;
                    }
                    return value;
                }
            }
            return Convert.ChangeType(value, expected, CultureInfo.InvariantCulture);
        }

        private static string GetFormatString(byte wholeDigits, byte decimalDigits, bool applyOffset)
        {
            var format = new StringBuilder(30);
            for (int k = 0; k < wholeDigits; k++)
            {
                format.Append('0');
            }
            // offset numbers get stored with one extra digit
            if (applyOffset)
            {
                format.Append('0');
            }
            if (decimalDigits > 0)
            {
                format.Append('.');
            }
            for (int k = 0; k < decimalDigits; k++)
            {
                format.Append('#');
            }
            return format.ToString();
        }

        private static decimal GetOffsetAmount(byte wholeDigits)
        {
            return (decimal) Math.Pow(10, wholeDigits);
        }

        private static bool TryFormatOverflow(IFormattable value, out string valueString)
        {
            valueString = null;

            bool isFpOverflow = false;
            if (value is double)
            {
                var d = (double) value;
                isFpOverflow = double.IsInfinity(d) || double.IsNaN(d);
            }
            else if (value is float)
            {
                var f = (float) value;
                isFpOverflow = float.IsInfinity(f) || float.IsNaN(f);
            }
            if (isFpOverflow)
            {
                valueString = value.ToString(null, CultureInfo.InvariantCulture);
                return true;
            }

            return false;
        }

        private static bool TryParseOverflow(string valueString, Type expected, out object value)
        {
            if (FloatingOverflows.TryGetValue(expected.Name + "." + valueString, out value))
            {
                return true;
            }
            return false;
        }
    }
}