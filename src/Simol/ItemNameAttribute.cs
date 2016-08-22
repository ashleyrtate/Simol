/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;

namespace Simol
{
    /// <summary>
    /// Supports selection of the item property used as the SimpleDB item name.
    /// </summary>
    /// <remarks>
    /// At least one property of a mapped type <em>must</em> be marked with this attribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ItemNameAttribute : SimolAttribute
    {
    }
}