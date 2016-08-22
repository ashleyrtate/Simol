using System;
using Amazon.SimpleDB;
using Simol.Data;
using NUnit.Framework;
using Rhino.Mocks;

namespace Simol.Consistency
{
    [TestFixture]
    public class ReliableWriteScopeTest
    {
        private SimolConfig config;
        private WriteMonitor monitor;
        private ISimol simol;
        private AmazonSimpleDB simpleDb;
        private ReliableWriteScope writeScope;

        [SetUp]
        public void SetUp()
        {
            config = new SimolConfig();
            simpleDb = MockRepository.GenerateMock<AmazonSimpleDB>();
            simol = MockRepository.GenerateMock<ISimol>();
            simol.Expect(s => s.Config).Return(config);
            simol.Expect(s => s.SimpleDB).Return(simpleDb);

            monitor = new WriteMonitor(simol);

            writeScope = new ReliableWriteScope(monitor);
        }

        [TearDown]
        public void TearDown()
        {
            writeScope.Dispose();
            simpleDb.VerifyAllExpectations();
            simol.VerifyAllExpectations();
        }

        [Test]
        public void Commit_NoSteps()
        {
            writeScope.Commit();
        }

        [Test, ExpectedException(typeof (InvalidOperationException))]
        public void AddTooManyWriteSteps()
        {
            for (int k = 0; k < config.BatchPutMaxCount + 1; k++)
            {
                writeScope.AddWriteStep(new ReliableWriteStep());
            }
        }
    }
}