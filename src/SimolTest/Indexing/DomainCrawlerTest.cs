using System;
using System.Collections.Generic;
using Coditate.Common.Util;
using Simol.Data;
using Simol.TestSupport;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;

namespace Simol.Indexing
{
    [TestFixture]
    public class DomainCrawlerTest
    {
        private SimolConfig config;
        private DomainCrawler crawler;
        private IIndexer indexer;
        private ItemMapping mapping;
        private ISimol simol;

        [SetUp]
        public void SetUp()
        {
            indexer = MockRepository.GenerateStrictMock<IIndexer>();
            simol = MockRepository.GenerateStrictMock<ISimol>();
            config = new SimolConfig
                {
                    Indexer = indexer
                };
            simol.Expect(s => s.Config).Repeat.Any().Return(config);
            mapping = ItemMapping.Create(typeof (H));
            crawler = new DomainCrawler(simol, mapping);
        }

        [TearDown]
        public void TearDown()
        {
            simol.VerifyAllExpectations();
        }

        [Test,
         ExpectedException(typeof (InvalidOperationException),
             ExpectedMessage =
                 "Mapping has missing or invalid version property. To support full-text indexing object mappings must include at least one DateTime property marked with VersionAttribute."
             )]
        public void InvalidMapping_NoVersion()
        {
            ItemMapping invalidMapping = ItemMapping.Create("Test", AttributeMapping.Create("Test", typeof (string)));

            crawler = new DomainCrawler(simol, invalidMapping);
        }

        [Test,
         ExpectedException(typeof (InvalidOperationException),
             ExpectedMessage =
                 "Mapping has missing or invalid version property. To support full-text indexing object mappings must include at least one DateTime property marked with VersionAttribute."
             )]
        public void InvalidMapping_NonDateVersion()
        {
            ItemMapping invalidMapping = ItemMapping.Create("Test", AttributeMapping.Create("Test", typeof (string)));
            AttributeMapping versionMapping = AttributeMapping.Create("Version", typeof (int));
            versionMapping.Versioning = VersioningBehavior.None;
            invalidMapping.AttributeMappings.Add(versionMapping);

            crawler = new DomainCrawler(simol, invalidMapping);
        }

        [Test,
         ExpectedException(typeof (InvalidOperationException),
             ExpectedMessage =
                 "Mapping has no indexed properties. To support full-text indexing object mappings must include at least one String property marked with IndexAttribute."
             )]
        public void InvalidMapping_NoIndex()
        {
            ItemMapping invalidMapping = ItemMapping.Create("Test", AttributeMapping.Create("Test", typeof (string)));
            AttributeMapping versionMapping = AttributeMapping.Create("Version", typeof (DateTime));
            versionMapping.Versioning = VersioningBehavior.None;
            invalidMapping.AttributeMappings.Add(versionMapping);

            crawler = new DomainCrawler(simol, invalidMapping);
        }

        [Test]
        public void BuildIndexValues()
        {
            var values = new PropertyValues("abc");
            values["Property1"] = "abc";
            values["Property2"] = 123;
            values["Property3"] = new List<string> {"x", "y", "z"};
            var allValues = new List<PropertyValues> {values};

            List<IndexValues> indexValues = crawler.BuildIndexValues(allValues);

            Assert.AreEqual(1, indexValues.Count);
            Assert.AreEqual("abc", indexValues[0]["Property1"]);
            Assert.AreEqual("123", indexValues[0]["Property2"]);
            Assert.AreEqual("x y z", indexValues[0]["Property3"]);
        }

        [Test]
        public void Crawl()
        {
            var indexStateResults = new SelectResults<PropertyValues>();
            var state = new IndexState();
            PropertyValues stateValues = PropertyValues.CreateValues(state);
            indexStateResults.Items.Add(stateValues);

            var indexItemResults1 = new SelectResults<PropertyValues>
                {
                    PaginationToken = RandomData.NumericString(10)
                };
            var itemValues = new PropertyValues(Guid.NewGuid());
            itemValues["ModifiedAt"] = DateTime.Now;
            itemValues["TextValue"] = RandomData.AlphaNumericString(2000, true);
            indexItemResults1.Items.Add(itemValues);

            var indexItemResults2 = new SelectResults<PropertyValues>();
            indexItemResults2.Items.Add(itemValues);

            var indexValues = new IndexValues(itemValues.ItemName.ToString());
            indexValues["TextValue"] = (string) itemValues["TextValue"];
            var valuesList = new List<IndexValues> {indexValues};

            // get index state
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(indexStateResults).Repeat.Once();
            // get and index first part of batch
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(indexItemResults1).Repeat.Once();
            indexer.Expect(i => i.IndexItems("H", valuesList)).Constraints(Is.Equal("H"),
                                                                           Property.AllPropertiesMatch(valuesList)).Repeat.Once();
            // update index state
            simol.Expect(s => s.PutAttributes(null, (PropertyValues)null)).IgnoreArguments().Repeat.Once();
            // get and index second part of batch
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(indexItemResults2).Repeat.Once();
            indexer.Expect(i => i.IndexItems("H", valuesList)).Constraints(Is.Equal("H"),
                                                                           Property.AllPropertiesMatch(valuesList)).
                Repeat.Once();
            // update index state
            simol.Expect(s => s.PutAttributes(null, (PropertyValues)null)).IgnoreArguments().Repeat.Once();

            crawler.Crawl(null);
        }
    }
}