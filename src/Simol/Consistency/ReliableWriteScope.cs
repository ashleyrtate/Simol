/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using Coditate.Common.Util;
using Simol.Data;

namespace Simol.Consistency
{
    /// <summary>
    /// Used to group two or more cross-domain write operations into a reliable-write that
    /// prevents partial-data loss in the event of a system failure.
    /// </summary>
    /// <remarks>
    /// To use a reliable-write for a group of operations, create 
    /// an instance of this class, invoke <see cref="Commit()"/>, and dispose the instance.
    /// 
    /// <para>All put and delete operations on the current thread for the life of the instance
    /// will be grouped into a single reliable-write. For example:
    /// <code>
    /// // all put operations in the using block will be part of the same reliable-write
    /// using (var write = new ReliableWriteScope(writeMonitor)) {
    ///     Simol.Put(person);
    ///     Simol.Put(employees);
    ///     write.Commit();
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// Note: A maxiumum of 25 puts, batch-puts, or deletes may be performed in the scope of a single reliable-write.
    /// </para>
    /// </remarks>
    /// <seealso cref="WriteMonitor"/>
    public class ReliableWriteScope : IDisposable
    {
        [ThreadStatic]
        private static ReliableWriteScope CurrentScope;

        private WriteMonitor monitor;
        private Guid writeId;
        private List<ReliableWriteStep> writeSteps = new List<ReliableWriteStep>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReliableWriteScope"/> class.
        /// </summary>
        /// <param name="monitor">The write monitor.</param>
        public ReliableWriteScope(WriteMonitor monitor)
        {
            Arg.CheckNull("monitor", monitor);

            this.monitor = monitor;
            writeId = Guid.NewGuid();
            CurrentScope = this;
        }

        internal List<ReliableWriteStep> WriteSteps
        {
            get { return writeSteps; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            CurrentScope = null;
        }

        /// <summary>
        /// Commits the current reliable-write to SimpleDB.
        /// </summary>
        /// <remarks>
        /// When this method is invoked all steps of the reliable-write are written
        /// to SimpleDB in a two-phased process. The overall commit is performed synchronously, 
        /// but each step of the operation is performed in parallel. In other words, this
        /// method will not return until all steps of the operation are complete (or the operation fails), 
        /// but each step of the write is processed in a separate thread.
        /// </remarks>
        public void Commit()
        {
            Commit(true);
        }

        internal void Commit(bool propagate)
        {
            // clearing current scope prevents our write to the SimolSystem table itself from being 
            // routed right back into this reliable-write
            CurrentScope = null;

            if (writeSteps.Count == 0)
            {
                return;
            }

            monitor.Commit(writeSteps);

            // option to skip propagation here is provided only for test purposes
            if (propagate)
            {
                monitor.Propagate(writeSteps);
            }
        }

        internal void AddWriteStep(ReliableWriteStep writeStep)
        {
            writeStep.ReliableWriteId = writeId;
            if (writeSteps.Count >= monitor.Simol.Config.BatchPutMaxCount)
            {
                string message = string.Format("Reliable-writes may include a maximum of {0} operations.",
                                               monitor.Simol.Config.BatchPutMaxCount);
                throw new InvalidOperationException(message);
            }

            writeSteps.Add(writeStep);
        }

        internal static ReliableWriteScope GetCurrentScope()
        {
            return CurrentScope;
        }
    }
}