using NUnit.Framework;
using Rhino.Mocks;
using AmazonAttribute=Amazon.SimpleDB.Model.Attribute;

namespace Simol.Core
{
    [TestFixture]
    public class DecoratingSimolTest
    {
        private class TestDecoratingSimol : DecoratingSimol
        {
            public TestDecoratingSimol(ISimolInternal decoratedSimol)
                : base(decoratedSimol)
            {
            }
        }

        private ISimolInternal decoratedSimol;
        private DecoratingSimol decoratingSimol;

        [SetUp]
        public void SetUp()
        {
            decoratedSimol = MockRepository.GenerateMock<ISimolInternal>();
            decoratingSimol = new TestDecoratingSimol(decoratedSimol);
        }

        [TearDown]
        public void TearDown()
        {
            decoratedSimol.VerifyAllExpectations();
        }

        [Test]
        public void AllPassThrough()
        {
            decoratedSimol.Expect(d => d.DeleteAttributes(null, null, null));
            decoratedSimol.Expect(d => d.GetAttributes(null, null, null)).Return(null);
            decoratedSimol.Expect(d => d.PutAttributes(null, null));
            decoratedSimol.Expect(d => d.SelectAttributes(null)).Return(null);
            decoratedSimol.Expect(d => d.SelectScalar(null)).Return(null);

            decoratingSimol.DeleteAttributes(null, null, null);
            decoratingSimol.GetAttributes(null, null, null);
            decoratingSimol.PutAttributes(null, null);
            decoratingSimol.SelectAttributes(null);
            decoratingSimol.SelectScalar(null);
        }
    }
}