/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Reflection;
using Coditate.Common.Util;

namespace Simol
{
    /// <summary>
    /// Supports customization of the SimpleDB domain name used to store item objects. 
    /// </summary>
    /// <remarks>
    /// SimpleDB domain names default to the short name of the type being stored (i.e. <see cref="MemberInfo.Name"/>). 
    /// Mark your item classes with this attribute to customize the SimpleDB domain name.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class DomainNameAttribute : SimolAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainNameAttribute"/> class.
        /// </summary>
        /// <param name="domainName">Name of the domain.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="domainName"/> is an empty string</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="domainName"/> is null</exception>
        public DomainNameAttribute(string domainName)
        {
            Arg.CheckNullOrEmpty("domainName", domainName);

            DomainName = domainName;
        }

        /// <summary>
        /// Gets or sets the name of the domain.
        /// </summary>
        /// <value>The name of the domain.</value>
        public string DomainName { get; private set; }
    }
}