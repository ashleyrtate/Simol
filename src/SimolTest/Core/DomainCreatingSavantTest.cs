using System.Net;
using Amazon.SimpleDB;
using Simol.TestSupport;
using NUnit.Framework;
using Rhino.Mocks;
using AmazonAttribute=Amazon.SimpleDB.Model.Attribute;
using Amazon.SimpleDB.Model;
using System.Threading;
using Coditate.TestSupport;
using System;

namespace Simol.Core
{
    [TestFixture]
    public class DomainCreatingSimolTest
    {
        private ISimolInternal decoratedSimol;
        private AmazonSimpleDBException domainMissingException;
        private AmazonSimpleDB simpleDb;
        private DomainCreatingSimol domainSimol;
        private ListDomainsResponse listDomainsResponse;

        [SetUp]
        public void SetUp()
        {
            listDomainsResponse = new ListDomainsResponse()
            {
                ListDomainsResult = new ListDomainsResult
                {
                    DomainName = { }
                }
            };
            simpleDb = MockRepository.GenerateStrictMock<AmazonSimpleDB>();
            simpleDb.Expect(s => s.ListDomains(null)).IgnoreArguments().Return(listDomainsResponse);
            simpleDb.Expect(x => x.CreateDomain(null)).IgnoreArguments().Return(null);
            
            decoratedSimol = MockRepository.GenerateMock<ISimolInternal>();
            decoratedSimol.Expect(x => x.SimpleDB).Return(simpleDb).Repeat.Any();

            domainMissingException = new AmazonSimpleDBException("", HttpStatusCode.OK, "NoSuchDomain", null, null, null,
                                                                 null);
            domainSimol = new DomainCreatingSimol(decoratedSimol);
        }

        [TearDown]
        public void TearDown()
        {
            decoratedSimol.VerifyAllExpectations();
        }

        [Test]
        public void Delete()
        {
            var a = new A();
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.DeleteAttributes(mapping, a.ItemName.ToUniList(), null));

            domainSimol.DeleteAttributes(mapping, a.ItemName.ToUniList(), null);
        }

        [Test]
        public void Get()
        {
            var a = new A();
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.GetAttributes(mapping, a.ItemName, null)).Return(null);

            domainSimol.GetAttributes(mapping, a.ItemName, null);
        }

        [Test]
        public void Put()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.PutAttributes(mapping, values.ToUniList()));

            domainSimol.PutAttributes(mapping, values.ToUniList());
        }

        [Test]
        [ExpectedException(typeof (WebException))]
        public void Put_NonAmazonError()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.PutAttributes(mapping, values.ToUniList())).Throw(new WebException());

            domainSimol.PutAttributes(mapping, values.ToUniList());
        }

        /// <summary>
        /// Verify that domain is recreated on next attempt if deleted during processing.
        /// </summary>
        [Test]
        public void Put_DomainError()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof(A));

            // set mock expectations
            simpleDb.Expect(s => s.ListDomains(null)).IgnoreArguments().Return(listDomainsResponse);
            simpleDb.Expect(x => x.CreateDomain(null)).IgnoreArguments().Return(null);
            decoratedSimol.Expect(x => x.PutAttributes(mapping, values.ToUniList())).Throw(domainMissingException).Repeat.Once();
            decoratedSimol.Expect(x => x.PutAttributes(mapping, values.ToUniList()));

            try
            {
                domainSimol.PutAttributes(mapping, values.ToUniList());
                Assert.Fail("Expected domain error");
            } catch (AmazonSimpleDBException)
            {
                // ignore
            }

            domainSimol.PutAttributes(mapping, values.ToUniList());
        }

        [Test]
        [ExpectedException(typeof (AmazonSimpleDBException))]
        public void Put_NonDomainError()
        {
            var a = new A();
            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof (A));

            // set mock expectations
            decoratedSimol.Expect(x => x.PutAttributes(mapping, values.ToUniList())).Throw(new AmazonSimpleDBException(""));

            domainSimol.PutAttributes(mapping, values.ToUniList());
        }

        [Test]
        public void Select()
        {
            var command = new SelectCommand(typeof (A), "select from A");

            // set mock expectations
            decoratedSimol.Expect(x => x.SelectAttributes(command)).Return(null);

            domainSimol.SelectAttributes(command);
        }

        [Test]
        public void SelectScalar()
        {
            var command = new SelectCommand<A>("select from A");
            // set mock expectations
            decoratedSimol.Expect(x => x.SelectScalar(command)).Return(null);

            domainSimol.SelectScalar(command);
        }

        /// <summary>
        /// Verifies that only one list and create domain call are made when multiple threads are accessing.
        /// </summary>
        [Test]
        public void Select_MultiThreaded()
        {
            var command = new SelectCommand(typeof(A), "select from A");

            // set mock expectations
            decoratedSimol.Expect(x => x.SelectAttributes(command)).Return(null);
            
            int count = 100;
            int threads = 3;
            Exception error = null;
            ParameterizedThreadStart threadStart = delegate
            {
                for (int k = 0; k < count / threads; k++)
                {
                    try
                    {
                        domainSimol.SelectAttributes(command);
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                        break;
                    }
                }
            };

            var testRunner = new TestThreadRunner();
            testRunner.AddThreads(threadStart, null, threads);
            testRunner.Run();

            Assert.IsNull(error);
        }
    }
}