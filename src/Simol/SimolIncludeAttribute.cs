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
    /// Supports explicit inclusion of item properties in the SimpleDB mapping.
    /// </summary>
    /// <remarks>
    /// Use this attribute on item properties you wish to explicitly mark as persistent. 
    /// Once this attribute is used on any property of an item type, all other properties of the
    /// type must also be explicitly included. Properties are explicitly included when you use <em>any 
    /// property-level Simol attribute</em> (e.g. <see cref="NumberFormatAttribute"/>, <see cref="AttributeNameAttribute"/>, etc.).
    /// 
    /// <para>Therefore, this attribute is most useful when you wish to persist a small number of
    /// properties from an item type that has many properties.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SimolIncludeAttribute : SimolAttribute
    {
    }
}