/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System.Collections.Generic;
namespace Simol.Consistency
{
    /// <summary>
    /// Base implementation of <see cref="IDomainConstraint"/>
    /// for convenience when only one or two validation methods are 
    /// needed. 
    /// </summary>
    /// <seealso cref="IDomainConstraint"/>
    public abstract class DomainConstraintBase : IDomainConstraint
    {
        /// <summary>
        /// For usage details see <see cref="IDomainConstraint"/>.
        /// </summary>
        public virtual void AfterLoad(PropertyValues values)
        {
        }

        /// <summary>
        /// For usage details see <see cref="IDomainConstraint"/>.
        /// </summary>
        public virtual void BeforeSave(PropertyValues values)
        {
        }

        /// <summary>
        /// For usage details see <see cref="IDomainConstraint"/>.
        /// </summary>
        public virtual void BeforeDelete(object itemName, List<string> propertyNames)
        {
        }
    }
}