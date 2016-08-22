using Amazon.SimpleDB;
using NUnit.Framework;
using Rhino.Mocks;
using AmazonAttribute=Amazon.SimpleDB.Model.Attribute;

namespace Simol.Core
{
    [TestFixture]
    public class DecoratingSimpleDBTest
    {
        private AmazonSimpleDB decoratedSimpleDb;
        private DecoratingSimpleDB decoratingSimpleDB;

        [SetUp]
        public void SetUp()
        {
            decoratedSimpleDb = MockRepository.GenerateMock<AmazonSimpleDB>();
            decoratingSimpleDB = new TestDecoratingSimpleDB(decoratedSimpleDb);
        }

        [TearDown]
        public void TearDown()
        {
            decoratedSimpleDb.VerifyAllExpectations();
        }

        [Test]
        public void AllPassThrough()
        {
            decoratedSimpleDb.Expect(d => d.BatchPutAttributes(null)).Return(null);
            decoratedSimpleDb.Expect(d => d.CreateDomain(null)).Return(null);
            decoratedSimpleDb.Expect(d => d.DeleteAttributes(null)).Return(null);
            decoratedSimpleDb.Expect(d => d.BatchDeleteAttributes(null)).Return(null);
            decoratedSimpleDb.Expect(d => d.DeleteDomain(null)).Return(null);
            decoratedSimpleDb.Expect(d => d.DomainMetadata(null)).Return(null);
            decoratedSimpleDb.Expect(d => d.GetAttributes(null)).Return(null);
            decoratedSimpleDb.Expect(d => d.ListDomains(null)).Return(null);
            decoratedSimpleDb.Expect(d => d.PutAttributes(null)).Return(null);
            decoratedSimpleDb.Expect(d => d.Dispose());

            decoratingSimpleDB.BatchPutAttributes(null);
            decoratingSimpleDB.CreateDomain(null);
            decoratingSimpleDB.DeleteAttributes(null);
            decoratingSimpleDB.BatchDeleteAttributes(null);
            decoratingSimpleDB.DeleteDomain(null);
            decoratingSimpleDB.DomainMetadata(null);
            decoratingSimpleDB.GetAttributes(null);
            decoratingSimpleDB.ListDomains(null);
            decoratingSimpleDB.PutAttributes(null);
            decoratingSimpleDB.Dispose();
        }

        private class TestDecoratingSimpleDB : DecoratingSimpleDB
        {
            public TestDecoratingSimpleDB(AmazonSimpleDB decoratedSimpleDb)
                : base(decoratedSimpleDb)
            {
            }
        }
    }
}