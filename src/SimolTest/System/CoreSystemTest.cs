using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.SimpleDB.Model;
using Simol.Async;
using NUnit.Framework;
using Coditate.Common.Util;

namespace Simol.System
{
    [TestFixture, Explicit]
    public class CoreSystemTest
    {
        private const int itemCount = 100;
        private List<SystemTestItem> dataItems;
        private SimolClient simol;
        private SelectUtils selectUtils;

        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            simol = SystemTestUtils.GetSimol();

            var request = new DeleteDomainRequest { DomainName = "SystemTest" };
            simol.SimpleDB.DeleteDomain(request);
            simol.Config.ReadConsistency = ConsistencyBehavior.Immediate;

            selectUtils = new SelectUtils(simol);
        }

        [TestFixtureTearDown]
        public void TearDownFixture()
        {
            var request = new DeleteDomainRequest { DomainName = "SystemTest" };
            simol.SimpleDB.DeleteDomain(request);
        }

        [Test]
        public void SystemTest()
        {
            DateTime start = DateTime.Now;

            Console.WriteLine("Starting test...");

            start = DateTime.Now;
            Put();
            Log("Putting...", ref start);

            Get();
            Log("Getting...", ref start);

            GetAsync();
            Log("Getting asynchronously...", ref start);

            SelectAll();
            Log("Selecting all...", ref start);

            SelectDateRange();
            Log("Selecting by date range...", ref start);

            SelectIntRange();
            Log("Selecting by integer range...", ref start);

            SelectDoubleRange();
            Log("Selecting by double range...", ref start);

            SelectIntList();
            Log("Selecting int list...", ref start);

            SelectSkipResults();
            Log("Selecting with skip...", ref start);

            SelectByNull();
            Log("Selecting by null attribute...", ref start);

            SelectCount();
            Log("Selecting count...", ref start);

            SelectCountNonScalar();
            Log("Selecting count non-scalar...", ref start);

            SelectId();
            Log("Selecting scalar id...", ref start);

            Update();
            Log("Updating...", ref start);

            Get();
            Log("Getting...", ref start);

            Delete();
            Log("Deleting...", ref start);
        }

        private void Log(string message, ref DateTime start)
        {
            DateTime end = DateTime.Now;

            Console.WriteLine(message);
            Console.WriteLine("\t" + (end - start).TotalSeconds + " sec");
            start = DateTime.Now;
        }

        private void Put()
        {
            dataItems = new List<SystemTestItem>();
            for (int k = 0; k < itemCount; k++)
            {
                SystemTestItem item = SystemTestItem.Create();
                dataItems.Add(item);
            }

            // add first half as single batch
            int batchPutItemCount = itemCount / 2;
            simol.Put(dataItems.Take(batchPutItemCount).ToList());

            // add second half with single-item puts
            foreach (SystemTestItem item in dataItems.Skip(batchPutItemCount))
            {
                simol.Put(item);
            }
        }

        private void Update()
        {
            foreach (SystemTestItem item in dataItems)
            {
                item.VersionValue = DateUtils.Round(DateTime.Now, DateRounding.Second);
                item.StringValue = null;
                item.NullableLongValue = null;
                item.DoubleValue = SystemTestUtils.RandomDouble();
                item.DateValue = SystemTestUtils.RandomDate();
                item.IntValue = SystemTestUtils.RandomInt();
            }

            simol.Put(dataItems);
        }

        private void Get()
        {
            foreach (SystemTestItem item1 in dataItems)
            {
                var item2 = simol.Get<SystemTestItem>(item1.ItemName);
                SystemTestUtils.AssertEqual(item1, item2);
            }
        }

        private void GetAsync()
        {
            var results = new List<IAsyncResult>();

            for (int k = 0; k < dataItems.Count; k++)
            {
                Guid id = dataItems[k].ItemName;
                var result = simol.BeginGet<SystemTestItem>(id, null, null);
                results.Add(result);
            }
            for (int k = 0; k < dataItems.Count; k++)
            {
                SystemTestItem item = simol.EndGet<SystemTestItem>(results[k]);
                if (item == null)
                {
                    Console.WriteLine("Null: " + k);
                    continue;
                }
                SystemTestUtils.AssertEqual(dataItems[k], item);
            }
        }

        private void SelectAll()
        {
            var tempItems = simol.Select<SystemTestItem>("select * from SystemTest");
            foreach (SystemTestItem item1 in dataItems)
            {
                SystemTestItem item2 = tempItems.Where(i => i.ItemName == item1.ItemName).FirstOrDefault();
                Assert.IsNotNull(item2);
                SystemTestUtils.AssertEqual(item1, item2);
            }
        }

        private void SelectDateRange()
        {
            DateTime dateValue = SystemTestUtils.RandomDate();
            string selectText = "select * from SystemTest where DateValue > @DateValue order by DateValue";
            var command = new SelectCommand<SystemTestItem>(selectText);
            command.AddParameter("DateValue", dateValue);

            SelectResults<SystemTestItem> results = simol.Select(command);
            List<SystemTestItem> selectedItems =
                dataItems.Where(i => i.DateValue > dateValue).OrderBy(i => i.DateValue).ToList();

            Assert.AreEqual(selectedItems.Count, results.Count, "Command: " + command.ExpandedCommandText);

            for (int k = 0; k < selectedItems.Count; k++)
            {
                SystemTestUtils.AssertEqual(selectedItems[k], results[k]);
            }
        }

        private void SelectIntRange()
        {
            int intValue = SystemTestUtils.RandomInt();
            string selectText = "select * from SystemTest where IntValue > @IntValue order by IntValue";
            var command = new SelectCommand<SystemTestItem>(selectText);
            command.AddParameter("IntValue", intValue);

            SelectResults<SystemTestItem> results = simol.Select(command);
            List<SystemTestItem> selectedItems =
                dataItems.Where(i => i.IntValue > intValue).OrderBy(i => i.IntValue).ToList();

            Assert.AreEqual(selectedItems.Count, results.Count, "Command: " + command.ExpandedCommandText);

            for (int k = 0; k < selectedItems.Count; k++)
            {
                SystemTestUtils.AssertEqual(selectedItems[k], results[k]);
            }
        }

        private void SelectDoubleRange()
        {
            double doubleValue = SystemTestUtils.RandomDouble();
            string selectText = "select * from SystemTest where DoubleValue > @DoubleValue order by DoubleValue";
            var command = new SelectCommand<SystemTestItem>(selectText);
            command.AddParameter("DoubleValue", doubleValue);

            SelectResults<SystemTestItem> results = simol.Select(command);
            List<SystemTestItem> selectedItems =
                dataItems.Where(i => i.DoubleValue > doubleValue).OrderBy(i => i.DoubleValue).ToList();

            Assert.AreEqual(selectedItems.Count, results.Count, "Command: " + command.ExpandedCommandText);
            for (int k = 0; k < selectedItems.Count; k++)
            {
                SystemTestUtils.AssertEqual(selectedItems[k], results[k]);
            }
        }

        private void SelectIntList()
        {
            List<int> selectedInts =
                dataItems.OrderBy(i => i.IntValue).Select(i => i.IntValue).ToList();
            string selectText = "select * from SystemTest where IntValue in (@IntValue) order by IntValue";
            var command = new SelectCommand<SystemTestItem>(selectText);
            command.AddParameter("IntValue", null);

            var results = selectUtils.SelectWithList<SystemTestItem, int>(command, selectedInts, "IntValue");

            Assert.AreEqual(selectedInts.Count, results.Count, "Command: " + command.CommandText);
            for (int k = 0; k < selectedInts.Count; k++)
            {
                Assert.AreEqual(selectedInts[k], results[k].IntValue);
            }
        }

        /// <summary>
        /// Uses select COUNT and pagination token to skip past first half of results.
        /// </summary>
        private void SelectSkipResults()
        {
            int skipCount = dataItems.Count / 2;
            string selectCountText = string.Format("select count(*) from SystemTest where IntValue is not null order by IntValue limit {0}", skipCount);
            var nextToken = selectUtils.SelectCountNextToken<SystemTestItem>(selectCountText);

            string selectText = "select * from SystemTest where IntValue is not null order by IntValue";
            var command = new SelectCommand<SystemTestItem>(selectText);
            command.PaginationToken = nextToken;

            var results = simol.Select(command);

            List<SystemTestItem> selectedItems = dataItems.OrderBy(i => i.IntValue).Skip(skipCount).ToList();
            Assert.AreEqual(selectedItems.Count, results.Count, "Command: " + command.ExpandedCommandText);
            for (int k = 0; k < selectedItems.Count; k++)
            {
                SystemTestUtils.AssertEqual(selectedItems[k], results[k]);
            }
        }

        /// <summary>
        /// Selects all items using a null parameter against a null attribute value.
        /// </summary>
        private void SelectByNull()
        {
            var items = simol.Select<SystemTestItem>("select * from SystemTest where NullableLongValue = @NullableLongValue",
                new CommandParameter("NullableLongValue", null));
            foreach (SystemTestItem item in dataItems)
            {
                var item2 = items.Where(i => i.ItemName == item.ItemName).FirstOrDefault();
                Assert.IsNotNull(item2);
                SystemTestUtils.AssertEqual(item, item2);
            }
        }

        private void SelectCount()
        {
            int intValue = SystemTestUtils.RandomInt();
            string selectText = "select count(*) from SystemTest where IntValue > @IntValue";

            var count =
                (int)simol.SelectScalar<SystemTestItem>(selectText, new CommandParameter("IntValue", intValue));
            int localCount =
                dataItems.Where(i => i.IntValue > intValue).Count();

            Assert.AreEqual(localCount, count, "Command: " + selectText);
        }

        private void SelectCountNonScalar()
        {
            int intValue = SystemTestUtils.RandomInt();
            string selectText = "select count(*) from SystemTest where IntValue > @IntValue";

            AttributeMapping itemNameMapping = AttributeMapping.Create("Domain", typeof(string));
            ItemMapping mapping = ItemMapping.Create("SystemTest", itemNameMapping);
            mapping.AttributeMappings.Add(AttributeMapping.Create("Count", typeof(uint)));
            mapping.AttributeMappings.Add(AttributeMapping.Create("IntValue", typeof(int)));

            SelectResults<PropertyValues> values =
                simol.SelectAttributes(new SelectCommand(mapping, selectText,
                                                          new CommandParameter("IntValue", intValue)));
            var count = (uint)values.Items.First()["Count"];
            int localCount =
                dataItems.Where(i => i.IntValue > intValue).Count();

            Assert.AreEqual(localCount, count, "Command: " + selectText);
        }

        private void SelectId()
        {
            int intValue = dataItems.Select(i => i.IntValue).FirstOrDefault();
            Guid id = dataItems.Select(i => i.ItemName).FirstOrDefault();
            string selectText = "select Id from SystemTest where IntValue = @IntValue";

            var selectedId =
                (Guid?)simol.SelectScalar<SystemTestItem>(selectText, new CommandParameter("IntValue", intValue));

            Assert.AreEqual(id, selectedId, "Command: " + selectText);
        }

        private void Delete()
        {
            // delete first half as single batch
            int batchPutItemCount = itemCount / 2;
            var itemNames = dataItems.Take(batchPutItemCount).Select(i => i.ItemName).Cast<object>().ToList();
            simol.Delete<SystemTestItem>(itemNames);

            // delete rest one at a time
            foreach (SystemTestItem item1 in dataItems)
            {
                simol.Delete<SystemTestItem>(item1.ItemName);
            }
        }
    }
}