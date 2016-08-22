/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using Coditate.Common.Util;
using Simol.Consistency;

namespace Simol
{
    /// <summary>
    /// Supports custom data constraint/validation as items are loaded, saved, 
    /// and deleted by Simol.
    /// </summary>
    /// <remarks>
    /// Mark item classes with this attribute to force the use of a custom <see cref="IDomainConstraint"/>
    /// when items are loaded, saved, and deleted by Simol.
    /// </remarks>
    /// <seealso cref="IDomainConstraint"/>
    /// <seealso cref="DomainConstraintBase"/>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class ConstraintAttribute : SimolAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstraintAttribute"/> class.
        /// </summary>
        /// <param name="constraintType">The constraint type, which must implement <see cref="IDomainConstraint"/>.</param>
        /// <param name="constraintArgs">Arguments to pass to the custom constraint class constructor.</param>
        public ConstraintAttribute(Type constraintType, params object[] constraintArgs)
        {
            Arg.CheckIsAssignableTo("constraintType", constraintType, typeof (IDomainConstraint));

            try
            {
                Constraint = (IDomainConstraint) Activator.CreateInstance(constraintType, constraintArgs);
            }
            catch (Exception ex)
            {
                string arguments = StringUtils.Join(", ", constraintArgs);
                string message =
                    string.Format(
                        "Unable to instantiate domain constraint '{0}' with '{1}' constructor argument(s). The argument values were '{2}'.",
                        constraintType.FullName, constraintArgs.Length, arguments);
                throw new ArgumentException(message, ex);
            }
        }

        /// <summary>
        /// Gets or sets the custom domain constraint.
        /// </summary>
        /// <value>The constraint.</value>
        public IDomainConstraint Constraint { get; private set; }
    }
}