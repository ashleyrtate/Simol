using System;
using System.Collections;
using Coditate.Common.Util;
using Simol.Core;
using Simol.TestSupport;
using NUnit.Framework;
using Rhino.Mocks;
using System.Collections.Generic;
using System.Linq;

namespace Simol
{
    [TestFixture]
    public class SelectUtilsTest
    {

        private ISimol simol;
        private SimolConfig config;
        private SelectUtils selectUtils;
        private SelectCommand<A> command;

        [SetUp]
        public void SetUp()
        {
            simol = MockRepository.GenerateMock<ISimol>();
            config = new SimolConfig();
            simol.Expect(s => s.Config).Return(config);
            selectUtils = new SelectUtils(simol);

            command = new SelectCommand<A>("select * from A where IntValue in (@IntValue)");
            command.AddParameter("IntValue", null);
        }

        [TearDown]
        public void TearDown()
        {
            simol.VerifyAllExpectations();
        }

        [Test]
        public void SelectCountNextToken()
        {
            var values1 = new PropertyValues("Domain");
            values1["Count"] = 0u;
            var result1 = new SelectResults<PropertyValues>()
            {
                Items = { values1 },
                PaginationToken = RandomData.AsciiString(10)
            };
            var values2 = new PropertyValues("Domain");
            values2["Count"] = 1u;
            var result2 = new SelectResults<PropertyValues>()
            {
                Items = { values2 },
                PaginationToken = RandomData.AsciiString(10)
            };
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Repeat.Once().Return(result1);
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Repeat.Once().Return(result2);

            var token = selectUtils.SelectCountNextToken<A>("select count(*) from A");

            Assert.AreEqual(result2.PaginationToken, token);
        }

        [Test]
        public void SelectWithList()
        {
            int count = RandomData.Generator.Next(10000);
            var keys1 = new List<int>();
            var keys2 = new List<int>();
            for (int k = 0; k < count; k++)
            {
                keys1.Add(k);
                keys2.Add(k);
            }
            Predicate<SelectCommand> pred = delegate(SelectCommand cmd) 
            {
                lock(((ICollection)keys2).SyncRoot) 
                {
                    cmd.Parameters[0].Values.ForEach(v => keys2.Remove((int)v));
                }
                return true;
            };
            var a = new A { IntValue = RandomData.Int() };
            var selectResults = new SelectResults<PropertyValues>()
            {
                Items = { PropertyValues.CreateValues(a) }
            };
            int selectCount = count / config.MaxSelectComparisons;
            if (count % config.MaxSelectComparisons > 0)
            {
                selectCount++;
            }
            simol.Expect(s => s.SelectAttributes(Arg<SelectCommand>.Matches(p => pred.Invoke(p)))).Repeat.Times(selectCount).Return(selectResults);

            // execute query
            var items = selectUtils.SelectWithList<A, int>(command, keys1, "IntValue");

            // verify that we got one result with expected value from each select call
            Assert.IsNotNull(items);
            Assert.AreEqual(selectCount, items.Count);
            foreach (var item in items)
            {
                Assert.AreEqual(a.IntValue, item.IntValue);
            }
            // all keys should have been removed by predicate code
            Assert.IsEmpty(keys2);
        }

        [Test]
        public void SelectWithList_NoKeys()
        {
            var selectResults = new SelectResults<PropertyValues>();
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Repeat.Times(0).Return(selectResults);
            
            var items = selectUtils.SelectWithList<A, int>(command, new List<int>(), "IntValue");
        }
    }
}