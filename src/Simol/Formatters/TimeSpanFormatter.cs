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
    /// Formats <see cref="TimeSpan"/> objects for storage in SimpleDB.
    /// </summary>
    internal class TimeSpanFormatter : ITypeFormatter
    {
        public string ToString(object value)
        {
            Arg.CheckIsType("value", value, typeof(TimeSpan));
            
            return value.ToString();
        }

        public object ToType(string value, Type expected)
        {
            Arg.CheckNullOrEmpty("value", value);
            Arg.CheckIsAssignableTo("expected", expected, typeof(TimeSpan));

            return TimeSpan.Parse(value);
        }
    }
}