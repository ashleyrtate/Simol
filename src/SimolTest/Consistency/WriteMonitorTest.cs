using System;
using System.Threading;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Coditate.Common.Util;
using Simol.Data;
using NUnit.Framework;
using Rhino.Mocks;

namespace Simol.Consistency
{
    [TestFixture]
    public class WriteMonitorTest
    {
        private SimolConfig config;
        private WriteMonitor monitor;
        private ISimol simol;
        private AmazonSimpleDB simpleDb;

        [SetUp]
        public void SetUp()
        {
            config = new SimolConfig();
            simpleDb = MockRepository.GenerateMock<AmazonSimpleDB>();
            simol = MockRepository.GenerateMock<ISimol>();
            simol.Expect(s => s.Config).Return(config);
            simol.Expect(s => s.SimpleDB).Return(simpleDb);
            monitor = new WriteMonitor(simol);
        }

        [TearDown]
        public void TearDown()
        {
            simpleDb.VerifyAllExpectations();
            simol.VerifyAllExpectations();
        }

        [Test]
        public void StartStop_NoWrites()
        {
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(new SelectResults<PropertyValues>());

            monitor.Start();

            Thread.Sleep(150);

            monitor.Stop();
        }

        [Test]
        public void StartStop_WithWrites()
        {
            var writeStep1 = new ReliableWriteStep
                {
                    SimpleDBRequest = new PutAttributesRequest(),
                    ReliableWriteId = Guid.NewGuid(),
                };
            var writeStep2 = new ReliableWriteStep
                {
                    SimpleDBRequest = new DeleteAttributesRequest(),
                    ReliableWriteId = Guid.NewGuid(),
                };
            var writeStepResults1 = new SelectResults<PropertyValues>
                {
                    PaginationToken = RandomData.NumericString(10)
                };
            writeStepResults1.Items.Add(PropertyValues.CreateValues(writeStep1));

            var writeStepResults2 = new SelectResults<PropertyValues>();
            writeStepResults2.Items.Add(PropertyValues.CreateValues(writeStep2));

            // get first "batch" to propagate (one write step from system domain)
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(writeStepResults1).Repeat.Once();
            // propagate put request and delete from system domain
            simpleDb.Expect(s => s.PutAttributes(null)).IgnoreArguments().Return(null);
            simol.Expect(s => s.DeleteAttributes(null, (object)null, null)).IgnoreArguments().Repeat.Once();

            // get second "batch" to propagate (one write step from system domain)
            simol.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(writeStepResults2).Repeat.Once();
            // propagate delete request and delete from system domain
            simpleDb.Expect(s => s.DeleteAttributes(null)).IgnoreArguments().Return(null);
            simol.Expect(s => s.DeleteAttributes(null, (object)null, null)).IgnoreArguments().Repeat.Once();

            // run test call
            monitor.PropagateFailedWrites();
        }
    }
}