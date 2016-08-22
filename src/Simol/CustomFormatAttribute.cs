/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using Coditate.Common.Util;

namespace Simol
{
    /// <summary>
    /// Supports custom formatting of item properties stored in SimpleDB.
    /// </summary>
    /// <remarks>
    /// Mark properties with this attribute to force the use of a custom format
    /// string or <see cref="ITypeFormatter"/> during property formatting and conversion.
    /// Use of the custom format string requires that the property type implement <see cref="IFormattable"/> 
    /// <em>and</em> the resulting string must be convertible back to the property type using <see cref="Convert.ChangeType(object, Type)"/>.
    /// </remarks>
    public class CustomFormatAttribute : SimolFormatAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomFormatAttribute"/> class.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is an empty string</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="format"/> is null</exception>
        public CustomFormatAttribute(string format)
        {
            Arg.CheckNullOrEmpty("format", format);

            Format = format;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomFormatAttribute"/> class.
        /// </summary>
        /// <param name="formatterType">Type of the formatter. The type must implement <see cref="ITypeFormatter"/>.</param>
        /// <param name="formatterArgs">The formatter constructor arguments.</param>
        /// <exception cref="ArgumentException">If <paramref name="formatterType"/> does not implement <c>ITypeFormatter</c>
        /// or can't be instantiated.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="formatterType"/> is null</exception>
        public CustomFormatAttribute(Type formatterType, params object[] formatterArgs)
        {
            Arg.CheckIsAssignableTo("formatterType", formatterType, typeof (ITypeFormatter));

            try
            {
                Formatter = (ITypeFormatter) Activator.CreateInstance(formatterType, formatterArgs);
            }
            catch (Exception ex)
            {
                string arguments = StringUtils.Join(", ", formatterArgs);
                string message =
                    string.Format(
                        "Unable to instantiate type formatter '{0}' with '{1}' constructor argument(s). The argument values were '{2}'.",
                        formatterType.FullName, formatterArgs.Length, arguments);
                throw new ArgumentException(message, ex);
            }
        }

        /// <summary>
        /// Gets or sets the custom formatter.
        /// </summary>
        /// <value>The formatter.</value>
        public ITypeFormatter Formatter { get; private set; }

        /// <summary>
        /// Gets or sets the custom format string.
        /// </summary>
        /// <value>The format string.</value>
        public string Format { get; private set; }
    }
}