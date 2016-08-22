using System;
using System.Globalization;
using System.Linq;
using Amazon.SimpleDB.Model;
using Simol.Formatters;
using Simol.TestSupport;
using NUnit.Framework;

namespace Simol.Consistency
{
    [TestFixture]
    public class VersioningUtilsTest
    {
        private SimolConfig config;
        private PropertyFormatter formatter;

        [SetUp]
        public void SetUp()
        {
            config = new SimolConfig();
            formatter = new PropertyFormatter(config);
        }

        [Test]
        public void IncrementVersion_Date()
        {
            DateTime start = DateTime.UtcNow;

            ItemMapping mapping = ItemMapping.Create(typeof (DateVersionItem));
            var i = new DateVersionItem();
            PropertyValues values = PropertyValues.CreateValues(i);

            AttributeMapping versionMapping = VersioningUtils.GetVersionMapping(mapping);
            var oldVersion = (DateTime) values["Version"];
            var newVersion = (DateTime) VersioningUtils.IncrementVersion(versionMapping, oldVersion);

            Assert.AreEqual(DateTime.MinValue, oldVersion);
            Assert.GreaterOrEqual(newVersion, start);
            Assert.AreNotEqual(oldVersion, newVersion);
        }

        [Test]
        public void IncrementVersion_Int()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (IntVersionItem));

            AttributeMapping versionMapping = VersioningUtils.GetVersionMapping(mapping);
            object oldVersion = 0;
            object newVersion = (int) VersioningUtils.IncrementVersion(versionMapping, oldVersion);

            Assert.AreEqual(1, newVersion);
            Assert.AreNotEqual(oldVersion, newVersion);

            oldVersion = null;
            newVersion = (int) VersioningUtils.IncrementVersion(versionMapping, oldVersion);

            Assert.AreEqual(1, newVersion);
            Assert.AreNotEqual(oldVersion, newVersion);
        }

        [Test]
        public void ApplyVersioningBehavior_CheckValue()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (IntVersionItem));
            var values = new PropertyValues(Guid.NewGuid());

            // check int old version
            values["Version"] = 1;
            ApplyVersioningBehavior_CheckValue(mapping, values, "10000000001");

            // check date old version
            string oldVersionStr = "2010-02-12T15:48:24.739Z";
            DateTime oldVersion = DateTime.Parse(oldVersionStr, CultureInfo.InvariantCulture,
                                                 DateTimeStyles.RoundtripKind);
            mapping = ItemMapping.Create(typeof (DateVersionItem));

            values["Version"] = oldVersion;
            ApplyVersioningBehavior_CheckValue(mapping, values, oldVersionStr);
        }

        private void ApplyVersioningBehavior_CheckValue(ItemMapping mapping, PropertyValues values, string oldVersion)
        {
            var request = new PutAttributesRequest
                {
                    Attribute = {new ReplaceableAttribute {Name = "Version", Value = "x"}}
                };
            VersioningUtils.ApplyVersioningBehavior(formatter, mapping, values, request.Attribute, request);

            Assert.IsNotNull(request.Expected);
            Assert.AreEqual(false, request.Expected.Exists);
            Assert.AreEqual("Version", request.Expected.Name);
            Assert.AreEqual(oldVersion, request.Expected.Value);
            Assert.AreNotEqual(oldVersion, values["Version"]);
        }

        [Test]
        public void ApplyVersioningBehavior_CheckExists()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (IntVersionItem));
            var values = new PropertyValues(Guid.NewGuid());

            // check null old version with int mapping
            values["Version"] = null;
            ApplyVersioningBehavior_CheckExists(mapping, values);

            // check default value old version with int mapping
            values["Version"] = 0;
            ApplyVersioningBehavior_CheckExists(mapping, values);

            mapping = ItemMapping.Create(typeof (DateVersionItem));

            // check null old version with date mapping
            values["Version"] = null;
            ApplyVersioningBehavior_CheckExists(mapping, values);

            // check default value old version with date mapping
            values["Version"] = DateTime.MinValue;
            ApplyVersioningBehavior_CheckExists(mapping, values);
        }

        [Test]
        public void ApplyVersioningBehavior_NoneBehavior()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (VersionItemNoneBehavior));
            var i = new VersionItemNoneBehavior();
            PropertyValues values = PropertyValues.CreateValues(i);

            string oldVersionStr = "x";
            var request = new PutAttributesRequest
                {
                    Attribute = {new ReplaceableAttribute {Name = "Version", Value = oldVersionStr}}
                };
            VersioningUtils.ApplyVersioningBehavior(formatter, mapping, values, request.Attribute, request);

            string newVersionStr =
                request.Attribute.Where(a => a.Name == "Version").Select(a => a.Value).FirstOrDefault();

            Assert.AreEqual(oldVersionStr, newVersionStr);
            Assert.AreEqual(0, values["Version"]);
        }

        [Test]
        public void ApplyVersioningBehavior_NoVersionMapping()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (NoVersionItem));
            var i = new NoVersionItem();
            PropertyValues values = PropertyValues.CreateValues(i);

            var request = new PutAttributesRequest();
            VersioningUtils.ApplyVersioningBehavior(formatter, mapping, values, request.Attribute, request);
        }

        [Test]
        public void ApplyVersioningBehavior_NoVersionAttribute()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (IntVersionItem));
            var i = new IntVersionItem();
            var values = new PropertyValues(i.ItemName);

            var request = new PutAttributesRequest();
            VersioningUtils.ApplyVersioningBehavior(formatter, mapping, values, request.Attribute, request);

            Assert.AreEqual(0, values.Count);
        }

        private void ApplyVersioningBehavior_CheckExists(ItemMapping mapping, PropertyValues values)
        {
            var request = new PutAttributesRequest
                {
                    Attribute = {new ReplaceableAttribute {Name = "Version", Value = "x"}}
                };
            object oldVersion = values["Version"];
            VersioningUtils.ApplyVersioningBehavior(formatter, mapping, values, request.Attribute, request);

            Assert.IsNotNull(request.Expected);
            Assert.AreEqual(false, request.Expected.Exists);
            Assert.AreEqual("Version", request.Expected.Name);
            Assert.AreEqual(null, request.Expected.Value);
            Assert.AreNotEqual(oldVersion, values["Version"]);
        }

        public class DateVersionItem : TestItemBase
        {
            [Version(VersioningBehavior.AutoIncrementAndConditionallyUpdate)]
            public DateTime Version { get; set; }

            public int IntValue { get; set; }
        }

        public class IntVersionItem : TestItemBase
        {
            [Version(VersioningBehavior.AutoIncrementAndConditionallyUpdate)]
            public int Version { get; set; }

            public int IntValue { get; set; }
        }

        public class NoVersionItem : TestItemBase
        {
            public int IntValue { get; set; }
        }

        public class VersionItemNoneBehavior : TestItemBase
        {
            [Version]
            public int Version { get; set; }

            public int IntValue { get; set; }
        }
    }
}