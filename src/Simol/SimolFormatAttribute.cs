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
    /// Base class for all Simol attributes related to property formatting.
    /// </summary>
    /// <seealso cref="CustomFormatAttribute"/>
    /// <seealso cref="NumberFormatAttribute"/>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class SimolFormatAttribute : SimolAttribute
    {
    }
}