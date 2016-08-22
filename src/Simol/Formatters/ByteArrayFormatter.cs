/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using Coditate.Common.Util;

namespace Simol.Formatters
{
    /// <summary>
    /// Converts <c>byte[]</c> values to strings and back.
    /// </summary>
    public class ByteArrayFormatter : ITypeFormatter
    {
        /// <summary>
        /// Converts a byte array to a base64-encoded string.
        /// </summary>
        public string ToString(object value)
        {
            Arg.CheckIsType("value", value, typeof (byte[]));

            return Convert.ToBase64String((byte[])value);
        }

        /// <summary>
        /// Converts a base64-encoded string to a byte array.
        /// </summary>
        public object ToType(string value, Type expected)
        {
            Arg.CheckNullOrEmpty("value", value);
            Arg.CheckIsAssignableTo("expected", expected, typeof (byte[]));

            return Convert.FromBase64String(value);
        }
    }
}