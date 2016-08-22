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
    /// Converts <see cref="Enum"/> values to strings and back.
    /// </summary>
    internal class EnumFormatter : ITypeFormatter
    {
        public string ToString(object value)
        {
            Arg.CheckIsType("value", value, typeof (Enum));

            return value.ToString();
        }

        public object ToType(string value, Type expected)
        {
            Arg.CheckNullOrEmpty("value", value);
            Arg.CheckIsAssignableTo("expected", expected, typeof (Enum));

            return Enum.Parse(expected, value);
        }
    }
}