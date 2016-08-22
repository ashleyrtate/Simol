/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
namespace Simol.Consistency
{
    /// <summary>
    /// Null domain constraint for use with mappings which don't provide a custom constraint.
    /// </summary>
    internal class NoOpConstraint : DomainConstraintBase
    {
        public static IDomainConstraint Default = new NoOpConstraint();

        private NoOpConstraint()
        {
        }
    }
}