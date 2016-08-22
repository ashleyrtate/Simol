using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Simol.Consistency;
using Coditate.TestSupport;
using NUnit.Framework;
using Rhino.Mocks;
using AmazonAttribute=Amazon.SimpleDB.Model.Attribute;

namespace Simol.Core
{
    [TestFixture]
    public class ConsistentSimpleDBTest
    {
        private SimolConfig config;
        private ConsistentSimpleDB consistentDb;
        private WriteMonitor monitor;
        private ISimol simol;
        private AmazonSimpleDB simpleDb;

        [SetUp]
        public void SetUp()
        {
            config = new SimolConfig();
            simpleDb = MockRepository.GenerateMock<AmazonSimpleDB>();
            consistentDb = new ConsistentSimpleDB(simpleDb, config);
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
        public void PutAttributes_NormalWrite()
        {
            simpleDb.Expect(s => s.PutAttributes(null)).IgnoreArguments().Return(null);

            var request = new PutAttributesRequest();
            consistentDb.PutAttributes(request);
        }

        [Test,
         ExpectedException(typeof (InvalidOperationException),
             ExpectedMessage =
                 "Conditional update (VersioningBehavior.AutoIncrementAndConditionallyUpdate) may not be used with reliable-writes. Condition was applied to domain.attribute: 'abc.123'"
             )]
        public void PutAttributes_Conditional()
        {
            using (new ReliableWriteScope(monitor))
            {
                var request = new PutAttributesRequest
                    {
                        DomainName = "abc",
                        Expected = new UpdateCondition {Name = "123"}
                    };
                consistentDb.PutAttributes(request);
            }
        }

        [Test]
        public void PutAttributes_ReliableWrite()
        {
            using (var writeScope = new ReliableWriteScope(monitor))
            {
                var request = new PutAttributesRequest();
                consistentDb.PutAttributes(request);
                consistentDb.PutAttributes(request);

                simol.Expect(s => s.PutAttributes(null, (List<PropertyValues>)null)).IgnoreArguments();

                Assert.AreEqual(2, writeScope.WriteSteps.Count);

                writeScope.Commit();
            }
        }

        [Test]
        public void BatchPutAttributes_NormalWrite()
        {
            simpleDb.Expect(s => s.BatchPutAttributes(null)).IgnoreArguments().Return(null);

            var request = new BatchPutAttributesRequest();
            consistentDb.BatchPutAttributes(request);
        }

        [Test]
        public void BatchPutAttributes_ReliableWrite()
        {
            using (var writeScope = new ReliableWriteScope(monitor))
            {
                var request = new BatchPutAttributesRequest();
                consistentDb.BatchPutAttributes(request);

                simol.Expect(s => s.PutAttributes(null, (List<PropertyValues>)null)).IgnoreArguments();

                Assert.AreEqual(1, writeScope.WriteSteps.Count);

                writeScope.Commit();
            }
        }

        [Test]
        public void DeleteAttributes_NormalWrite()
        {
            simpleDb.Expect(s => s.DeleteAttributes(null)).IgnoreArguments().Return(null);

            var request = new DeleteAttributesRequest();
            consistentDb.DeleteAttributes(request);
        }

        [Test]
        public void DeleteAttributes_ReliableWrite()
        {
            using (var writeScope = new ReliableWriteScope(monitor))
            {
                var request = new DeleteAttributesRequest();
                consistentDb.DeleteAttributes(request);

                simol.Expect(s => s.PutAttributes(null, (List<PropertyValues>)null)).IgnoreArguments();

                Assert.AreEqual(1, writeScope.WriteSteps.Count);

                writeScope.Commit();
            }
        }

        [Test]
        public void BatchDeleteAttributes_NormalWrite()
        {
            simpleDb.Expect(s => s.BatchDeleteAttributes(null)).IgnoreArguments().Return(null);

            var request = new BatchDeleteAttributesRequest();
            consistentDb.BatchDeleteAttributes(request);
        }

        [Test]
        public void BatchDeleteAttributes_ReliableWrite()
        {
            using (var writeScope = new ReliableWriteScope(monitor))
            {
                var request = new BatchDeleteAttributesRequest();
                consistentDb.BatchDeleteAttributes(request);

                simol.Expect(s => s.PutAttributes(null, (List<PropertyValues>)null)).IgnoreArguments();

                Assert.AreEqual(1, writeScope.WriteSteps.Count);

                writeScope.Commit();
            }
        }

        [Test]
        public void ApplyConsistencyBehavior_Config()
        {
            // ensure consistent read when config is immediate
            config.ReadConsistency = ConsistencyBehavior.Immediate;

            var request1 = new GetAttributesRequest();
            consistentDb.GetAttributes(request1);
            Assert.IsTrue(request1.ConsistentRead);

            var request2 = new SelectRequest();
            consistentDb.Select(request2);
            Assert.IsTrue(request2.ConsistentRead);

            // ensure eventual consistent read when config is eventual
            config.ReadConsistency = ConsistencyBehavior.Eventual;

            request1.ConsistentRead = false;
            consistentDb.GetAttributes(request1);
            Assert.IsFalse(request1.ConsistentRead);

            request2.ConsistentRead = false;
            consistentDb.GetAttributes(request1);
            Assert.IsFalse(request2.ConsistentRead);
        }

        [Test]
        public void ApplyConsistencyBehavior_Scoped()
        {
            config.ReadConsistency = ConsistencyBehavior.Eventual;

            int threadCount = 3;
            int runCount = 50;
            int errorCount = 0;
            ParameterizedThreadStart callback = delegate
                {
                    for (int k = 0; k < runCount; k++)
                    {
                        var request1 = new SelectRequest();
                        using (new ConsistentReadScope())
                        {
                            consistentDb.Select(request1);
                        }
                        if (!request1.ConsistentRead)
                        {
                            Interlocked.Increment(ref errorCount);
                        }

                        var request2 = new SelectRequest();
                        consistentDb.Select(request2);
                        if (request2.ConsistentRead)
                        {
                            Interlocked.Increment(ref errorCount);
                        }
                    }
                };
            var runner = new TestThreadRunner();
            runner.AddThreads(callback, null, threadCount);
            runner.Run();

            Assert.AreEqual(0, errorCount);
        }

        [Test]
        public void Select_ConsistentRead()
        {
            var response = new SelectResponse
                {
                    SelectResult = new SelectResult()
                };
            Func<SelectRequest, SelectResponse> checkRequest = delegate(SelectRequest request)
                {
                    Assert.IsTrue(request.ConsistentRead);
                    return response;
                };

            simpleDb.Expect(x => x.Select(null)).IgnoreArguments().Do(checkRequest);

            using (new ConsistentReadScope())
            {
                consistentDb.Select(new SelectRequest());
            }
        }

        [Test]
        public void Get_ConsistentRead()
        {
            var response = new GetAttributesResponse
                {
                    GetAttributesResult =
                        new GetAttributesResult {Attribute = new List<AmazonAttribute>()}
                };
            Func<GetAttributesRequest, GetAttributesResponse> checkRequest = delegate(GetAttributesRequest request)
                {
                    Assert.IsTrue(request.ConsistentRead);
                    return response;
                };

            simpleDb.Expect(x => x.GetAttributes(null)).IgnoreArguments().Do(checkRequest);

            using (new ConsistentReadScope())
            {
                consistentDb.GetAttributes(new GetAttributesRequest());
            }
        }
    }
}