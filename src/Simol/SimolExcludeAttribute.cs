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
    /// Supports explicit exclusion of item properties from the SimpleDB mapping.
    /// </summary>
    /// <remarks>
    /// Use this attribute on item properties you wish to explicitly mark as non-persistent. 
    /// 
    /// <para>This attribute is most useful when you wish to exclude a small number of
    /// properties from an item type that has many automatically included properties.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SimolExcludeAttribute : SimolAttribute
    {
    }
}