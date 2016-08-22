using System;
using System.Collections.Generic;
using System.Linq;
using Coditate.Common.Util;
using Simol.Cache;
using Simol.TestSupport;
using NUnit.Framework;
using Rhino.Mocks;
using AmazonAttribute=Amazon.SimpleDB.Model.Attribute;

namespace Simol.Core
{
    [TestFixture]
    public class CachingSimolTest
    {
        private ISimolInternal decoratedSimol;
        private CachingSimol cachingSimol;

        [SetUp]
        public void SetUp()
        {
            var config = new SimolConfig
                {
                    Cache = new SimpleCache()
                };

            decoratedSimol = MockRepository.GenerateMock<ISimolInternal>();
            decoratedSimol.Expect(x => x.Config).Return(config).Repeat.Any();

            cachingSimol = new CachingSimol(decoratedSimol);
        }

        [TearDown]
        public void TearDown()
        {
            decoratedSimol.VerifyAllExpectations();
        }

        [Test]
        public void DeleteAttributesFlushesCache()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.PutAttributes(mapping, values.ToUniList()));
            decoratedSimol.Expect(x => x.DeleteAttributes(mapping, a.ItemName.ToUniList(), "IntValue".ToUniList()));
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, ListUtils.EmptyStringList)).Return(values);

            cachingSimol.PutAttributes(mapping, new List<PropertyValues> { values });
            cachingSimol.DeleteAttributes(mapping, a.ItemName.ToUniList(), "IntValue".ToUniList());
            cachingSimol.GetAttributes(mapping, a.ItemName, null);
        }

        [Test]
        public void Get_Deleted()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.PutAttributes(mapping, new List<PropertyValues> { values }));
            decoratedSimol.Expect(x => x.DeleteAttributes(mapping, a.ItemName.ToUniList(), null));

            cachingSimol.PutAttributes(mapping, values.ToUniList());
            cachingSimol.DeleteAttributes(mapping, a.ItemName.ToUniList(), null);
            PropertyValues values2 = cachingSimol.GetAttributes(mapping, a.ItemName, null);

            Assert.IsNull(values2);
        }

        [Test]
        public void Get_NotCached()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, ListUtils.EmptyStringList)).Return(values);

            PropertyValues values2 = cachingSimol.GetAttributes(mapping, a.ItemName, null);

            Assert.AreSame(values, values2);
        }

        [Test]
        public void GetGet()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, ListUtils.EmptyStringList)).Return(values);

            PropertyValues values1 = cachingSimol.GetAttributes(mapping, a.ItemName, null);
            PropertyValues values2 = cachingSimol.GetAttributes(mapping, a.ItemName, null);

            Assert.AreSame(values, values1);
            Assert.AreSame(values1, values2);
        }

        /// <summary>
        /// Gets all attributes then specifically requests a single, non-existent attribute. The cached values should
        /// still be returned.
        /// </summary>
        [Test]
        public void GetGet_Null()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof(A));
            mapping.AttributeMappings.Add(AttributeMapping.Create("NonexistentProperty", typeof(string)));

            // set mock expectations
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, ListUtils.EmptyStringList)).Return(values);
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, new List<string>{"NonexistentProperty"})).Return(null);

            PropertyValues values1 = cachingSimol.GetAttributes(mapping, a.ItemName, null);
            PropertyValues values2 = cachingSimol.GetAttributes(mapping, a.ItemName, new List<string> { "NonexistentProperty" });

            Assert.AreSame(values, values1);
            Assert.AreSame(values1, values2);
        }

        [Test]
        public void GetGet_PartialPropertySet()
        {
            // verifies that cached value is used when cached set is marked as incomplete 
            // but otherwise matches request
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            values.IsCompleteSet = false;
            ItemMapping mapping = ItemMapping.Create(typeof(A));

            // set mock expectations
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, ListUtils.EmptyStringList)).Return(values);

            PropertyValues values1 = cachingSimol.GetAttributes(mapping, a.ItemName, null);
            PropertyValues values2 = cachingSimol.GetAttributes(mapping, a.ItemName, mapping.AttributeMappings.Select(p => p.PropertyName).ToList());

            Assert.AreSame(values, values1);
            Assert.AreSame(values1, values2);
        }

        [Test]
        public void GetGet_CacheMiss()
        {
            var a = new A();
            var values1 = new PropertyValues(a.ItemName);
            values1["StringValue"] = "abc 123";

            var values2 = new PropertyValues(a.ItemName);
            values2["StringValue"] = "abc 123";
            values2["BooleanValue"] = true;

            var values3 = new PropertyValues(a.ItemName);
            values3["IntValue"] = 100;

            var values4 = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof(A));

            var values5 = new PropertyValues(a.ItemName);
            values5["ExtraProperty"] = "extra property value";
            ItemMapping mapping2 = ItemMapping.Create("A", AttributeMapping.Create("ItemName", typeof(Guid)));
            mapping2.AttributeMappings.Add(AttributeMapping.Create("ExtraProperty", typeof(string)));

            // set mock expectations
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, "StringValue".ToUniList())).Return(values1);
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, new List<string>{"StringValue", "BooleanValue"})).Return(values2);
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, "IntValue".ToUniList())).Return(values3);
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, ListUtils.EmptyStringList)).Return(values4);
            decoratedSimol.Expect(x => x.GetAttributes(mapping2, a.ItemName, ListUtils.EmptyStringList)).Return(values5);

            PropertyValues values1a = cachingSimol.GetAttributes(mapping, a.ItemName, "StringValue".ToUniList());
            // force a cache miss by getting an additional property
            PropertyValues values2a = cachingSimol.GetAttributes(mapping, a.ItemName, new List<string>{"StringValue", "BooleanValue"});
            // force a cache miss by getting one property not in the cached set 
            PropertyValues values3a = cachingSimol.GetAttributes(mapping, a.ItemName, "IntValue".ToUniList());
            // force a cache miss by getting all properties
            PropertyValues values4a = cachingSimol.GetAttributes(mapping, a.ItemName, null);
            // force a cache miss by getting an EXTRA property
            PropertyValues values5a = cachingSimol.GetAttributes(mapping2, a.ItemName, null);

            Assert.AreSame(values1, values1a);
            Assert.AreSame(values1a, values2a);
            Assert.AreSame(values3a, values4a);
            Assert.AreSame(values4a, values5a);
        }

        /// <summary>
        /// Verifies that a call to PutAttributes() removes cached item.
        /// </summary>
        [Test]
        public void PutAttributesUpdatesCache()
        {
            var a = new A();
            var values1 = new PropertyValues(a.ItemName);
            values1["IntValue"] = a.IntValue;
            PropertyValues values2 = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof (A), values1.ToList());

            // set mock expectations
            decoratedSimol.Expect(x => x.PutAttributes(mapping, values1.ToUniList()));
            decoratedSimol.Expect(x => x.PutAttributes(mapping, values2.ToUniList()));

            cachingSimol.PutAttributes(mapping, values1.ToUniList());
            cachingSimol.PutAttributes(mapping, values2.ToUniList());
            PropertyValues values3 = cachingSimol.GetAttributes(mapping, a.ItemName, null);

            Assert.AreSame(values2, values3);
        }

        [Test]
        public void PutGet()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.PutAttributes(mapping, values.ToUniList()));

            cachingSimol.PutAttributes(mapping, values.ToUniList());
            PropertyValues values2 = cachingSimol.GetAttributes(mapping, a.ItemName, null);

            Assert.AreSame(values, values2);
        }

        [Test]
        public void SelectGet()
        {
            var a = new A();
            PropertyValues values1 = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof(A));
            var command = new SelectCommand(mapping, "select * from A");
            var results1 = new SelectResults<PropertyValues>();
            results1.Items.Add(values1);

            // set mock expectations
            decoratedSimol.Expect(x => x.SelectAttributes(command)).Return(results1);

            SelectResults<PropertyValues> results2 = cachingSimol.SelectAttributes(command);
            PropertyValues values2 = cachingSimol.GetAttributes(mapping, a.ItemName, null);

            Assert.AreSame(values1, results2.Items[0]);
            Assert.AreSame(values1, values2);
        }
    }
}