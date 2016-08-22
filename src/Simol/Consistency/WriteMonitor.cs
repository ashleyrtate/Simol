/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Coditate.Common.Util;
using Simol.Data;
using Common.Logging;

namespace Simol.Consistency
{
    /// <summary>
    /// Monitors reliable-write operations and retries the propagation of failed writes in a background thread.
    /// </summary>
    /// <remarks>
    /// Applications that use reliable-writes will normally create and <see cref="Start"/> one instance of 
    /// this class at application startup. The same instance may be passed to all <see cref="ReliableWriteScope"/>
    /// instances created by the application.
    /// <para>
    /// It is also acceptable to create a new instance of this class for each <c>ReliableWriteScope</c> 
    /// instance. However, when using multiple instances in this manner <em>do not</em> start more than a single instance.
    /// </para>
    /// </remarks>
    /// <seealso cref="ReliableWriteScope"/>
    public class WriteMonitor
    {
        /// <summary>
        /// Default value for <see cref="ReprocessDelay"/>. The value is 1 minute.
        /// </summary>
        public static readonly TimeSpan DefaultReprocessDelay = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Default value for <see cref="RetryInterval"/>. The value is 15 minutes.
        /// </summary>
        public static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromMinutes(15);

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly object synchLock = new object();

        private bool run;
        private bool sleeping;
        private Thread thread;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMonitor"/> class.
        /// </summary>
        /// <param name="simol">The Simol instance to use for managing reliable-writes.</param>
        public WriteMonitor(ISimol simol) 
        {
            Arg.CheckNull("simol", simol);
            Arg.CheckNull("simol.Config", simol.Config);
            Arg.CheckNull("simol.SimpleDB", simol.SimpleDB);

            Simol = simol;
            RetryInterval = DefaultRetryInterval;
            ReprocessDelay = DefaultReprocessDelay;
        }

        internal ISimol Simol { get; private set; }

        /// <summary>
        /// Gets or sets the interval at which to retry failed write operations.
        /// </summary>
        /// <value>The retry interval.</value>
        /// <remarks>The default value is <see cref="DefaultRetryInterval"/>.</remarks>
        public TimeSpan RetryInterval { get; set; }

        /// <summary>
        /// Gets or sets the time to wait before attempting to reprocess a failed write.
        /// </summary>
        /// <value>The delay time.</value>
        /// <remarks>The default value is <see cref="DefaultReprocessDelay"/>.</remarks>
        public TimeSpan ReprocessDelay { get; set; }

        /// <summary>
        /// Propagates the specified write steps. The overall batch is propagated 
        /// synchronously, but each item of the batch is processed in parallel.
        /// </summary>
        internal void Propagate(List<ReliableWriteStep> writes)
        {
            Arg.CheckNull("writes", writes);

            Log.Debug(s => s("Starting propagation of {0} reliable write steps.", writes.Count));

            Action<ReliableWriteStep> propagateAction = (r => Propagate(r));
            var results = new List<IAsyncResult>();
            foreach (ReliableWriteStep step in writes)
            {
                IAsyncResult result = propagateAction.BeginInvoke(step, null, null);
                results.Add(result);
            }

            foreach (IAsyncResult result in results)
            {
                var resultImpl = (AsyncResult) result;
                var action = resultImpl.AsyncDelegate as Action<ReliableWriteStep>;
                action.EndInvoke(result);
            }
        }

        private void Propagate(ReliableWriteStep writeStep)
        {
            string requestType = writeStep.SimpleDBRequest.GetType().Name;

            Log.Debug(
                s =>
                s("Propagating step '{0}' of reliable write '{1}'. RequestType = {2}", writeStep.Id,
                  writeStep.ReliableWriteId, requestType));

            try
            {
                switch (requestType)
                {
                    case "PutAttributesRequest":
                        Simol.SimpleDB.PutAttributes((PutAttributesRequest) writeStep.SimpleDBRequest);
                        break;
                    case "BatchPutAttributesRequest":
                        Simol.SimpleDB.BatchPutAttributes((BatchPutAttributesRequest)writeStep.SimpleDBRequest);
                        break;
                    case "DeleteAttributesRequest":
                        Simol.SimpleDB.DeleteAttributes((DeleteAttributesRequest)writeStep.SimpleDBRequest);
                        break;
                    case "BatchDeleteAttributesRequest":
                        Simol.SimpleDB.BatchDeleteAttributes((BatchDeleteAttributesRequest)writeStep.SimpleDBRequest);
                        break;
                    default:
                        string message = string.Format("Request type '{0}' is not supported", requestType);
                        throw new InvalidOperationException(message);
                }

                ItemMapping mapping = ItemMapping.Create(writeStep.GetType());
                Simol.DeleteAttributes(mapping, writeStep.Id);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Propagation of reliable write step '{0}' failed: ", ex, writeStep.Id);
                throw;
            }
        }

        internal void Commit(List<ReliableWriteStep> writes)
        {
            Arg.CheckNull("writes", writes);

            ItemMapping mapping = ItemMapping.Create(typeof (ReliableWriteStep));
            List<PropertyValues> writeItems =
                writes.Select(w => PropertyValues.CreateValues(mapping, w)).ToList();
            Simol.PutAttributes(mapping, writeItems);
        }

        /// <summary>
        /// Starts this instance and returns.
        /// </summary>
        public void Start()
        {
            State.CheckTrue(run, "Monitor is already running");
            lock (synchLock)
            {
                run = true;
                thread = new Thread(Run);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            lock (synchLock)
            {
                if (thread == null)
                {
                    return;
                }
                run = false;
                if (sleeping)
                {
                    thread.Interrupt();
                }
                thread.Join();
                thread = null;
            }
        }

        private void Run()
        {
            Log.Info(s => s("Starting the reliable-write monitor"));

            while (run)
            {
                using (new ConsistentReadScope())
                {
                    PropagateFailedWrites();
                }

                Sleep(RetryInterval);
            }

            Log.Info(s => s("Stopping the reliable-write monitor"));
        }

        internal void PropagateFailedWrites()
        {
            SelectCommand command = GetWriteStepSelect();
            SelectResults<PropertyValues> results;
            List<ReliableWriteStep> writeSteps;

            bool batchComplete;
            do
            {
                results = Simol.SelectAttributes(command);
                command.PaginationToken = results.PaginationToken;
                batchComplete = results.PaginationToken == null;

                writeSteps =
                    results.Select(p => (ReliableWriteStep) PropertyValues.CreateItem(typeof (ReliableWriteStep), p)).
                        ToList();
                Propagate(writeSteps);
            } while (!batchComplete);
        }

        private SelectCommand GetWriteStepSelect()
        {
            string selectQuery =
                @"select * from SimolSystem where DataType = @DataType and MachineGuid = @MachineGuid and ModifiedAt <= @ModifiedAt order by ModifiedAt asc limit 100";
            var typeParam = new CommandParameter("DataType", typeof (ReliableWriteStep).Name);
            var machineParam = new CommandParameter("MachineGuid", SystemData.GetMachineGuid());
            var modifiedParam = new CommandParameter("ModifiedAt", DateTime.UtcNow - DefaultReprocessDelay);

            var writeStepSelect = new SelectCommand(typeof (ReliableWriteStep), selectQuery, typeParam, machineParam,
                                                    modifiedParam)
                {
                    MaxResultPages = 1
                };

            return writeStepSelect;
        }

        private void Sleep(TimeSpan interval)
        {
            sleeping = true;
            try
            {
                Thread.Sleep(interval);
            }
            catch (ThreadInterruptedException)
            {
                // ignore
            }
            finally
            {
                sleeping = false;
            }
        }
    }
}