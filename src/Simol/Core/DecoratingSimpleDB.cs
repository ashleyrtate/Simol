/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Coditate.Common.Util;
using System;

namespace Simol.Core
{
    /// <summary>
    /// Base class which allows decoration of <see cref="AmazonSimpleDB"/>
    /// implementations for cleanly segregated functions.
    /// </summary>
    internal abstract class DecoratingSimpleDB : AmazonSimpleDB
    {
        public DecoratingSimpleDB(AmazonSimpleDB decorated)
        {
            Arg.CheckNull("decorated", decorated);

            Decorated = decorated;
        }

        public AmazonSimpleDB Decorated { get; protected set; }

        public void Dispose()
        {
            Decorated.Dispose();
        }

        public virtual CreateDomainResponse CreateDomain(CreateDomainRequest request)
        {
            return Decorated.CreateDomain(request);
        }

        public virtual ListDomainsResponse ListDomains(ListDomainsRequest request)
        {
            return Decorated.ListDomains(request);
        }

        public virtual DomainMetadataResponse DomainMetadata(DomainMetadataRequest request)
        {
            return Decorated.DomainMetadata(request);
        }

        public virtual DeleteDomainResponse DeleteDomain(DeleteDomainRequest request)
        {
            return Decorated.DeleteDomain(request);
        }

        public virtual PutAttributesResponse PutAttributes(PutAttributesRequest request)
        {
            return Decorated.PutAttributes(request);
        }

        public virtual BatchPutAttributesResponse BatchPutAttributes(BatchPutAttributesRequest request)
        {
            return Decorated.BatchPutAttributes(request);
        }

        public virtual BatchDeleteAttributesResponse BatchDeleteAttributes(BatchDeleteAttributesRequest request)
        {
            return Decorated.BatchDeleteAttributes(request);
        }

        public virtual GetAttributesResponse GetAttributes(GetAttributesRequest request)
        {
            return Decorated.GetAttributes(request);
        }

        public virtual DeleteAttributesResponse DeleteAttributes(DeleteAttributesRequest request)
        {
            return Decorated.DeleteAttributes(request);
        }

        public virtual SelectResponse Select(SelectRequest request)
        {
            return Decorated.Select(request);
        }

        public IAsyncResult BeginBatchDeleteAttributes(BatchDeleteAttributesRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginBatchPutAttributes(BatchPutAttributesRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginCreateDomain(CreateDomainRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginDeleteAttributes(DeleteAttributesRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginDeleteDomain(DeleteDomainRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginDomainMetadata(DomainMetadataRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginGetAttributes(GetAttributesRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginListDomains(ListDomainsRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginPutAttributes(PutAttributesRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginSelect(SelectRequest request, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public BatchDeleteAttributesResponse EndBatchDeleteAttributes(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public BatchPutAttributesResponse EndBatchPutAttributes(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public CreateDomainResponse EndCreateDomain(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public DeleteAttributesResponse EndDeleteAttributes(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public DeleteDomainResponse EndDeleteDomain(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public DomainMetadataResponse EndDomainMetadata(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public GetAttributesResponse EndGetAttributes(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public ListDomainsResponse EndListDomains(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public PutAttributesResponse EndPutAttributes(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public SelectResponse EndSelect(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }
    }
}