/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System.Collections;
using System.Collections.Generic;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;

namespace Simol.Core
{
    /// <summary>
    /// Decorating simol implementation that transparently creates domains
    /// as necessary.
    /// </summary>
    internal class DomainCreatingSimol : DecoratingSimol
    {
        private readonly Dictionary<string, string> domains = new Dictionary<string, string>();
        private readonly object syncLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainCreatingSimol"/> class.
        /// </summary>
        /// <param name="decoratedSimol">The decorated simol.</param>
        public DomainCreatingSimol(ISimolInternal decoratedSimol)
            : base(decoratedSimol)
        {
        }

        public override void PutAttributes(ItemMapping mapping, List<PropertyValues> values)
        {
            EnsureDomain(mapping.DomainName);

            try
            {
                DecoratedSimol.PutAttributes(mapping, values);
            }
            catch (AmazonSimpleDBException ex)
            {
                ClearIfDomainError(mapping.DomainName, ex);
                throw;
            }
        }

        public override PropertyValues GetAttributes(ItemMapping mapping, object itemName, List<string> propertyNames)
        {
            EnsureDomain(mapping.DomainName);

            try
            {
                return DecoratedSimol.GetAttributes(mapping, itemName, propertyNames);
            }
            catch (AmazonSimpleDBException ex)
            {
                ClearIfDomainError(mapping.DomainName, ex);
                throw;
            }
        }

        public override void DeleteAttributes(ItemMapping mapping, List<object> itemNames, List<string> propertyNames)
        {
            EnsureDomain(mapping.DomainName);
            try
            {
                DecoratedSimol.DeleteAttributes(mapping, itemNames, propertyNames);
            }
            catch (AmazonSimpleDBException ex)
            {
                ClearIfDomainError(mapping.DomainName, ex);
                throw;
            }
        }

        public override SelectResults<PropertyValues> SelectAttributes(SelectCommand command)
        {
            EnsureDomain(command.Mapping.DomainName);
            try
            {
                return DecoratedSimol.SelectAttributes(command);
            }
            catch (AmazonSimpleDBException ex)
            {
                ClearIfDomainError(command.Mapping.DomainName, ex);
                throw;
            }
        }

        public override object SelectScalar(SelectCommand command)
        {
            EnsureDomain(command.Mapping.DomainName);
            try
            {
                return DecoratedSimol.SelectScalar(command);
            }
            catch (AmazonSimpleDBException ex)
            {
                ClearIfDomainError(command.Mapping.DomainName, ex);
                throw;
            }
        }

        private void ClearIfDomainError(string domainName, AmazonSimpleDBException error)
        {
            if (error.ErrorCode == "NoSuchDomain")
            {
                lock (((ICollection)domains).SyncRoot)
                {
                    domains.Remove(domainName);
                }
            }
        }

        private void EnsureDomain(string domainName)
        {
            lock (syncLock)
            {
                if (domains.Count == 0)
                {
                    RefreshDomains();
                }
                if (domains.ContainsKey(domainName))
                {
                    return;
                }
                CreateDomain(domainName);
            }
        }

        private void RefreshDomains()
        {
            var request = new ListDomainsRequest();
            ListDomainsResponse response;
            do
            {
                response = SimpleDB.ListDomains(request);
                request.NextToken = response.ListDomainsResult.NextToken;
                foreach (var name in response.ListDomainsResult.DomainName)
                {
                    domains.Add(name, null);
                }
            } while (response.ListDomainsResult.NextToken != null);
        }

        private void CreateDomain(string domainName)
        {
            var request = new CreateDomainRequest
            {
                DomainName = domainName
            };
            SimpleDB.CreateDomain(request);
            domains[domainName] = null;
        }
    }
}