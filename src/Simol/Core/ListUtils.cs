/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simol.Core
{
    internal static class ListUtils
    {

        public static readonly List<string> EmptyStringList = new List<string>().AsReadOnly().ToList();

        public static readonly List<object> EmptyObjectList = new List<object>().AsReadOnly().ToList();

        /// <summary>
        /// Wrap PropertyValues in a generic list.
        /// </summary>
        public static List<PropertyValues> ToUniList(this PropertyValues values)
        {
            return new List<PropertyValues> { values };
        }

        /// <summary>
        /// Wrap object in a generic list.
        /// </summary>
        public static List<object> ToUniList(this object o)
        {
            return new List<object> { o };
        }

        /// <summary>
        /// Wrap string in a generic list.
        /// </summary>
        public static List<string> ToUniList(this string s)
        {
            return new List<string> { s };
        }
    }
}
