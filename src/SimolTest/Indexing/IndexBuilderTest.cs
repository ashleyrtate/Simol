using System;
using System.Threading;
using Simol.TestSupport;
using NUnit.Framework;
using Rhino.Mocks;

namespace Simol.Indexing
{
    [TestFixture]
    public class IndexBuilderTest
    {
        private IndexBuilder builder;
        private SimolConfig config;
        private ISimol simol;

        [SetUp]
        public void SetUp()
        {
            simol = MockRepository.GenerateMock<ISimol>();
            config = new SimolConfig();
            simol.Expect(s => s.Config).Return(config);
            builder = new IndexBuilder(simol)
                {
                    UpdateInterval = TimeSpan.FromMilliseconds(100)
                };
        }

        [TearDown]
        public void TearDown()
        {
            builder.Stop();
            // wait to let domain crawlers finish current operation
            Thread.Sleep(1000);
            simol.VerifyAllExpectations();
        }

        [TestFixtureTearDown]
        public void TearDownFixture()
        {
            // explicitly dispose indexer so that finalization disposal doesn't interfere with other tests
            config.Indexer.Dispose();
        }

        [Test]
        public void StartStop_OneMapping()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (H));
            builder.Register(mapping);

            // verfies that DomainCrawler is invoked exactly once when builder is started
            var results = new SelectResults<PropertyValues>();
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(results);

            builder.Start();

            Thread.Sleep(150);

            builder.Stop();
        }

        [Test]
        public void StartStop_NoMapping()
        {
            // verfies that DomainCrawler is not invoked when builder is started
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Repeat.Never();

            builder.Start();

            Thread.Sleep(150);

            builder.Stop();
        }

        [Test]
        public void RegisterUnregister()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (H));
            builder.Register(mapping);
            builder.Deregister(mapping);

            // verfies that DomainCrawler is not invoked when builder is started
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Repeat.Never();

            builder.Start();

            Thread.Sleep(150);

            builder.Stop();
        }

        [Test,
         ExpectedException(typeof (InvalidOperationException),
             ExpectedMessage = "Duplicate mapping registration. A mapping was already registered for domain 'H'.")]
        public void RegisterMappingTwice()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (H));
            builder.Register(mapping);
            builder.Register(mapping);
        }


        [Test,
         ExpectedException(typeof (InvalidOperationException),
             ExpectedMessage = "Mappings may not be registered/deregistered while builder is running")]
        public void RegisterWhileRunning()
        {
            var results = new SelectResults<PropertyValues>();
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(results).Repeat.Any();

            builder.Start();

            ItemMapping mapping = ItemMapping.Create(typeof (H));
            builder.Register(mapping);
        }

        [Test,
         ExpectedException(typeof (InvalidOperationException),
             ExpectedMessage = "Mappings may not be registered/deregistered while builder is running")]
        public void DeregisterWhileRunning()
        {
            var results = new SelectResults<PropertyValues>();
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(results).Repeat.Any();

            ItemMapping mapping = ItemMapping.Create(typeof (H));
            builder.Register(mapping);

            builder.Start();

            builder.Deregister(mapping);
        }
    }
}