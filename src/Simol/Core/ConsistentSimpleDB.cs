/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Simol.Consistency;
using Simol.Data;

namespace Simol.Core
{
    /// <summary>
    /// Decorates calls to SimpleDB to implement consistency related features such 
    /// as consistent reads and reliable writes. 
    /// </summary>
    internal class ConsistentSimpleDB : DecoratingSimpleDB
    {
        public ConsistentSimpleDB(AmazonSimpleDB decorated, SimolConfig config) : base(decorated)
        {
            Config = config;
        }

        public SimolConfig Config { get; private set; }

        public override PutAttributesResponse PutAttributes(PutAttributesRequest request)
        {
            if (CheckAndInterceptWrite(request))
            {
                CheckCondtionalUpdate(request);
                return new PutAttributesResponse();
            }

            return base.PutAttributes(request);
        }

        public override BatchPutAttributesResponse BatchPutAttributes(BatchPutAttributesRequest request)
        {
            if (CheckAndInterceptWrite(request))
            {
                return new BatchPutAttributesResponse();
            }

            return base.BatchPutAttributes(request);
        }

        public override DeleteAttributesResponse DeleteAttributes(DeleteAttributesRequest request)
        {
            if (CheckAndInterceptWrite(request))
            {
                return new DeleteAttributesResponse();
            }

            return base.DeleteAttributes(request);
        }

        public override BatchDeleteAttributesResponse BatchDeleteAttributes(BatchDeleteAttributesRequest request)
        {
            if (CheckAndInterceptWrite(request))
            {
                return new BatchDeleteAttributesResponse();
            }

            return base.BatchDeleteAttributes(request);
        }

        public override GetAttributesResponse GetAttributes(GetAttributesRequest request)
        {
            if (UseConsistentRead(Config))
            {
                request.ConsistentRead = true;
            }

            return base.GetAttributes(request);
        }

        public override SelectResponse Select(SelectRequest request)
        {
            if (UseConsistentRead(Config))
            {
                request.ConsistentRead = true;
            }

            return base.Select(request);
        }

        private bool CheckAndInterceptWrite(object request)
        {
            ReliableWriteScope writeScope = ReliableWriteScope.GetCurrentScope();
            if (writeScope != null)
            {
                var step = new ReliableWriteStep
                    {
                        SimpleDBRequest = request
                    };

                writeScope.AddWriteStep(step);
                return true;
            }
            return false;
        }

        private bool UseConsistentRead(SimolConfig config)
        {
            return config.ReadConsistency == ConsistencyBehavior.Immediate ||
                   ConsistentReadScope.GetCurrentScope() != null;
        }

        private void CheckCondtionalUpdate(PutAttributesRequest request)
        {
            if (request.Expected != null)
            {
                string message =
                    string.Format(
                        "Conditional update ({0}.{1}) may not be used with reliable-writes. Condition was applied to domain.attribute: '{2}.{3}'",
                        typeof (VersioningBehavior).Name, VersioningBehavior.AutoIncrementAndConditionallyUpdate,
                        request.DomainName,
                        request.Expected.Name);
                throw new InvalidOperationException(message);
            }
        }
    }
}