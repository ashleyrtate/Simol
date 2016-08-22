/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;

namespace Simol.Consistency
{
    /// <summary>
    /// Used to enforce SimpleDB consistent reads with limited scope.
    /// </summary>
    /// <remarks>
    /// To use consistent reads for a single operation or set of operations, create 
    /// an instance of this class and dispose it when you wish to return to using "normal" 
    /// reads. All read operations on the current thread will be performed using consistent reads
    /// for the life of the instance. For example:
    /// <code>
    /// // all read operations in the using block will return "consistent" data
    /// using (new ConsistentReadScope()) {
    ///     Person p = Simol.Get&lt;Person&gt;(personId);
    ///     List&lt;Employee&gt; employees = Simol.Select&lt;Employee&gt;("select * from Employees");
    /// }
    /// </code>
    /// </remarks>
    /// <seealso cref="SimolConfig.ReadConsistency"/>
    public class ConsistentReadScope : IDisposable
    {
        [ThreadStatic]
        private static ConsistentReadScope CurrentScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsistentReadScope"/> class.
        /// </summary>
        public ConsistentReadScope()
        {
            CurrentScope = this;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            CurrentScope = null;
        }

        internal static ConsistentReadScope GetCurrentScope()
        {
            return CurrentScope;
        }
    }
}