/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coditate.Common.Util;
using Simol.Consistency;
using Simol.Core;
using Simol.Data;
using Common.Logging;

namespace Simol.Indexing
{
    /// <summary>
    /// Crawls updated items in a single domain and feeds their indexed attributes
    /// to the indexer.
    /// </summary>
    /// <remarks>
    /// <see cref="IndexBuilder"/> instantiates a single instance of this class for each domain registered for indexing.
    /// </remarks>
    internal class DomainCrawler
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly object crawlLock = new object();
        private readonly AttributeMapping versionMapping;
        private SelectCommand domainSelect;
        private SelectCommand indexStateSelect;
        private IndexState lastIndexState;

        public DomainCrawler(ISimol simol, ItemMapping mapping)
        {
            Arg.CheckNull("simol", simol);
            Arg.CheckNull("simol.Config", simol.Config);
            Arg.CheckNull("mapping", mapping);

            Simol = simol;
            Mapping = mapping;
            IndexBatchSize = IndexBuilder.DefaultIndexBatchSize;

            versionMapping = VersioningUtils.GetVersionMapping(mapping);

            CheckMappingValid();
        }

        public ItemMapping Mapping { get; private set; }
        public ISimol Simol { get; private set; }

        public int IndexBatchSize { get; set; }

        private void CheckMappingValid()
        {
            if (versionMapping == null || versionMapping.FormatType != typeof (DateTime))
            {
                string message =
                    string.Format(
                        "Mapping has missing or invalid version property. To support full-text indexing object mappings must include at least one DateTime property marked with {0}.",
                        typeof (VersionAttribute).Name);
                throw new InvalidOperationException(message);
            }
            if (!Mapping.AttributeMappings.Where(a => a.IsIndexed).Any())
            {
                string message =
                    string.Format(
                        "Mapping has no indexed properties. To support full-text indexing object mappings must include at least one String property marked with {0}.",
                        typeof (IndexAttribute).Name);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Invoked by ThreadPool workers to crawl the domain.
        /// </summary>
        public void Crawl(object state)
        {
            // ensure that only one work thread crawls the domain at once in
            // case crawling takes longer than IndexBuilder.UpdateInterval
            lock (crawlLock)
            {
                // use consistent reads for entire indexing operation
                using (new ConsistentReadScope())
                {
                    CrawlImpl();
                }
            }
        }

        private void CrawlImpl()
        {
            IndexState indexState = GetIndexState();
            DateTime lastIndexedVersion = indexState.LastIndexedVersion;
            SelectResults<PropertyValues> results;
            bool batchComplete;
            string nextToken = null;
            do
            {
                SelectCommand command = GetDomainSelect(lastIndexedVersion);
                command.PaginationToken = nextToken;
                results = Simol.SelectAttributes(command);
                nextToken = results.PaginationToken;
                batchComplete = results.PaginationToken == null;
                IndexValues(results.Items);

                // update index state in system domain each time we finish indexing a batch
                PropertyValues lastItem = results.Items.LastOrDefault();
                UpdateIndexState(indexState, lastItem, batchComplete);
            } while (!batchComplete);
        }

        private void UpdateIndexState(IndexState indexState, PropertyValues lastItem, bool batchComplete)
        {
            if (lastItem != null)
            {
                indexState.LastIndexedVersion = (DateTime) lastItem[versionMapping.PropertyName];
            }
            // if our index batch has ended with the same version twice, then increment the index state version by 1 ms.
            // This keeps us from retrieving the last updated items repeatedly when the indexed domain is
            // infrequently updated. There is a slight risk that some records could be skipped if
            // a huge batch of items is inserted with the exact same version (but only if the batch insertion itself spans two complete
            // indexing cycles).
            if (batchComplete && lastIndexState != null &&
                (indexState.LastIndexedVersion == lastIndexState.LastIndexedVersion))
            {
                indexState.LastIndexedVersion += TimeSpan.FromMilliseconds(1);
            }

            ItemMapping indexStateMapping = ItemMapping.Create(typeof (IndexState));
            PropertyValues indexStateValues = PropertyValues.CreateValues(indexState);

            Simol.PutAttributes(indexStateMapping, indexStateValues);

            if (batchComplete)
            {
                lastIndexState = indexState;
            }
        }

        private void IndexValues(List<PropertyValues> allValues)
        {
            List<IndexValues> indexItems = BuildIndexValues(allValues);

            Log.Debug(
                m => m("Found {0} new/updated item(s) to index in domain '{1}'", allValues.Count, Mapping.DomainName));

            Simol.Config.Indexer.IndexItems(Mapping.DomainName, indexItems);
        }

        private IndexState GetIndexState()
        {
            SelectCommand command = GetIndexStateSelect();
            SelectResults<PropertyValues> results = Simol.SelectAttributes(command);
            PropertyValues values = results.Items.FirstOrDefault();
            IndexState state = null;

            if (values != null)
            {
                state = (IndexState) PropertyValues.CreateItem(typeof (IndexState), values);

                Log.Debug(m => m("Found previous index state where DomainName = '{0}' and MachineGuid = '{1}': LastIndexedVersion = '{2:yyyy/MM/dd HH:mm:ss.fffK}'",
                      Mapping.DomainName, IndexState.GetMachineGuid(), state.LastIndexedVersion));
            }
            if (state == null)
            {
                Log.Debug(m => m("Found no previous index state where DomainName = '{0}' and MachineGuid = '{1}'",
                      Mapping.DomainName, IndexState.GetMachineGuid()));
                
                state = new IndexState
                    {
                        DomainName = Mapping.DomainName,
                        HostName = IndexState.GetHostName(),
                        LastIndexedVersion = DateTime.MinValue,
                        MachineGuid = IndexState.GetMachineGuid()
                    };
            }
            return state;
        }

        private SelectCommand GetIndexStateSelect()
        {
            if (indexStateSelect == null)
            {
                string selectQuery =
                    "select * from SimolSystem where DataType = @DataType and MachineGuid = @MachineGuid and DomainName = @DomainName";
                var typeParam = new CommandParameter("DataType", null);
                var machineParam = new CommandParameter("MachineGuid", null);
                var domainParam = new CommandParameter("DomainName", null);

                indexStateSelect = new SelectCommand(typeof (IndexState), selectQuery, typeParam, machineParam,
                                                     domainParam);
            }
            indexStateSelect.Reset();
            indexStateSelect.GetParameter("DataType").Values.Add(typeof (IndexState).Name);
            indexStateSelect.GetParameter("MachineGuid").Values.Add(IndexState.GetMachineGuid());
            indexStateSelect.GetParameter("DomainName").Values.Add(Mapping.DomainName);

            return indexStateSelect;
        }

        private SelectCommand GetDomainSelect(DateTime lastIndexTime)
        {
            if (domainSelect == null)
            {
                List<string> attributeNames =
                    Mapping.AttributeMappings.Where(a => a.IsIndexed).Select(a => a.AttributeName).ToList();
                attributeNames.Add(versionMapping.AttributeName);
                string attributeList = StringUtils.Join(", ", attributeNames);
                string selectQuery =
                    string.Format("select {0} from {1} where {2} >= @LastIndexedVersion order by {2} asc limit {3}",
                                  attributeList, Mapping.DomainName, versionMapping.AttributeName, IndexBatchSize);
                var parameter = new CommandParameter("LastIndexedVersion", versionMapping.PropertyName, null);
                domainSelect = new SelectCommand(Mapping, selectQuery, parameter);
                domainSelect.MaxResultPages = 1;
            }
            domainSelect.Reset();
            domainSelect.GetParameter("LastIndexedVersion").Values.Add(lastIndexTime);

            return domainSelect;
        }

        internal List<IndexValues> BuildIndexValues(List<PropertyValues> allValues)
        {
            var indexItems = new List<IndexValues>();

            foreach (PropertyValues values in allValues)
            {
                string id = MappingUtils.ItemNameToString(Simol.Config.Formatter, Mapping.ItemNameMapping, values.ItemName);
                var indexItem = new IndexValues(id);
                foreach (string propertyName in values)
                {
                    if (propertyName == versionMapping.PropertyName)
                    {
                        continue;
                    }
                    // concatenate all attribute values into a single string for indexing
                    ICollection valueList = MappingUtils.ToList(values[propertyName]);
                    indexItem[propertyName] = StringUtils.Join(" ", valueList);
                }
                indexItems.Add(indexItem);
            }

            return indexItems;
        }
    }
}