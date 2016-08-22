using System;
using System.Collections.Generic;
using System.Threading;
using Coditate.Common.Util;
using Simol.TestSupport;
using Coditate.TestSupport;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;

namespace Simol.Async
{
    [TestFixture]
    public class AsyncExtensionsTest
    {
        private ISimol simol;
        private ISimol simol2;

        [SetUp]
        public void SetUp()
        {
            simol = MockRepository.GenerateMock<ISimol>();
            simol2 = MockRepository.GenerateMock<ISimol>();
        }

        [TearDown]
        public void TearDown()
        {
            simol.VerifyAllExpectations();
            simol2.VerifyAllExpectations();
        }

        [Test]
        public void BeginEndPut()
        {
            var aList = new List<A2> {new A2()};

            simol.Expect(s => s.Put(aList)).Constraints(Is.Same(aList));

            IAsyncResult result = simol.BeginPut(aList, null, null);
            simol.EndPut(result);
        }

        [Test]
        public void BeginEndGet()
        {
            var a = new A2();

            simol.Expect(s => s.Get<A2>(Arg<object>.Is.Equal(a.ItemName))).Return(a);

            IAsyncResult result = simol.BeginGet<A2>(a.ItemName, null, null);
            var a2 = simol.EndGet<A2>(result);

            Assert.AreSame(a, a2);
        }

        /// <summary>
        /// Since all of the async methods are driven through the same logic this test only provides
        /// minimal code execution of the functions that aren't covered by the meatier tests.
        /// </summary>
        [Test]
        public void BeginEnd_Minimal()
        {
            // GetAttributesT
            simol.Expect(s => s.GetAttributes<A2>(null)).IgnoreArguments().Return(null);
            IAsyncResult result = simol.BeginGetAttributes<A2>(null, null, null, null);
            simol.EndGetAttributes(result);

            // Delete
            simol.Expect(s => s.Delete<A2>((object)null)).IgnoreArguments();
            result = simol.BeginDelete<A2>((object)null, null, null);
            simol.EndDelete(result);

            // DeleteList
            simol.Expect(s => s.Delete<A2>((List<object>)null)).IgnoreArguments();
            result = simol.BeginDelete<A2>((List<object>)null, null, null);
            simol.EndDelete(result);

            // DeleteAttributes
            simol2.Expect(s => s.DeleteAttributes(null, (object)null, null)).IgnoreArguments();
            result = simol2.BeginDeleteAttributes(null, (object)null, null, null, null);
            simol2.EndDeleteAttributes(result);

            // DeleteAttributesList
            simol2.Expect(s => s.DeleteAttributes(null, (List<object>)null, null)).IgnoreArguments();
            result = simol2.BeginDeleteAttributes(null, (List<object>)null, null, null, null);
            simol2.EndDeleteAttributes(result);

            // DeleteAttributesT
            simol.Expect(s => s.DeleteAttributes<A2>((object)null)).IgnoreArguments();
            result = simol.BeginDeleteAttributes<A2>((object)null, null, null, null);
            simol.EndDeleteAttributes(result);

            // DeleteAttributesTList
            simol.Expect(s => s.DeleteAttributes<A2>((List<object>)null)).IgnoreArguments();
            result = simol.BeginDeleteAttributes<A2>((List<object>)null, null, null, null);
            simol.EndDeleteAttributes(result);

            // Find
            simol.Expect(s => s.Find<A2>(null, 0, 0, null)).IgnoreArguments().Return(null);
            result = simol.BeginFind<A2>(null, 0, 0, null, null, null);
            simol.EndFind<A2>(result);

            // FindAttributes
            simol.Expect(s => s.FindAttributes<A2>(null, 0, 0, null)).IgnoreArguments().Return(null);
            result = simol.BeginFindAttributes<A2>(null, 0, 0, null, null, null, null);
            simol.EndFindAttributes(result);

            // Select
            simol.Expect(s => s.Select((SelectCommand<A2>) null)).IgnoreArguments().Return(null);
            result = simol.BeginSelect<A2>(null, null, null);
            simol.EndSelect<A2>(result);

            // SelectAttributesT
            simol.Expect(s => s.SelectAttributes((SelectCommand<A2>) null)).IgnoreArguments().Return(null);
            result = simol.BeginSelectAttributes<A2>(null, null, null);
            simol.EndSelectAttributes(result);

            // SelectAttributes
            simol2.Expect(s => s.SelectAttributes(null)).IgnoreArguments().Return(null);
            result = simol2.BeginSelectAttributes(null, null, null);
            simol2.EndSelectAttributes(result);

            // SelectScalar
            simol2.Expect(s => s.SelectScalar(null)).IgnoreArguments().Return(null);
            result = simol2.BeginSelectScalar(null, null, null);
            simol2.EndSelectScalar(result);

            // SelectScalarT
            simol.Expect(s => s.SelectScalar<A2>(null, null)).IgnoreArguments().Return(null);
            result = simol.BeginSelectScalar<A2>(null, null, null, null);
            simol.EndSelectScalar(result);

            // PutAttributes
            simol.Expect(s => s.PutAttributes<A2>((PropertyValues)null)).IgnoreArguments();
            result = simol.BeginPutAttributes<A2>((PropertyValues)null, null, null);
            simol.EndPutAttributes(result);

            // PutAttributesList
            simol.Expect(s => s.PutAttributes<A2>((List<PropertyValues>)null)).IgnoreArguments();
            result = simol.BeginPutAttributes<A2>((List<PropertyValues>)null, null, null);
            simol.EndPutAttributes(result);
        }

        [Test, ExpectedException(typeof (InvalidOperationException), ExpectedMessage =
            "The provided IAsyncResult was returned from a Begin method that does not match the current End method.")]
        public void BeginEndFunction_Mismatch()
        {
            IAsyncResult result = simol.BeginPut(new List<A2>(), null, null);
            simol.EndGet<A2>(result);
        }

        [Test, ExpectedException(typeof (InvalidOperationException), ExpectedMessage =
            "The provided IAsyncResult was returned from a Begin method that does not match the current End method.")]
        public void BeginEndAction_Mismatch()
        {
            IAsyncResult result = simol.BeginGet<A2>(null, null, null);
            simol.EndPut(result);
        }

        [Test]
        public void BeginEndPutAttributes()
        {
            var a = new A2();
            ItemMapping mapping = ItemMapping.Create(typeof (A2));
            var valuesList = new List<PropertyValues> {PropertyValues.CreateValues(a)};

            simol2.Expect(s => s.PutAttributes(mapping, valuesList)).Constraints(Is.Equal(mapping),
                                                                                  Is.Equal(valuesList));

            IAsyncResult result = simol2.BeginPutAttributes(mapping, valuesList, null, null);
            simol2.EndPutAttributes(result);
        }

        /// <summary>
        /// Test an async method with callback and state.
        /// </summary>
        [Test]
        public void BeginEndPut_Callback()
        {
            var a = new A2();

            simol.Expect(s => s.Put(a)).IgnoreArguments();

            string state1 = RandomData.AlphaNumericString(10, false);
            string state2 = null;
            AsyncCallback callback = delegate(IAsyncResult ar)
                {
                    state2 = (string) ar.AsyncState;
                    simol.EndPut(ar);
                };

            simol.BeginPut(a, callback, state1);

            WaitUtils.Default.WaitTillTrue(delegate { return state2 != null; });

            Assert.AreEqual(state1, state2);
        }

        [Test]
        public void BeginEndGet_Callback()
        {
            var a = new A2();

            simol.Expect(s => s.Get<A2>(a.ItemName)).IgnoreArguments().Return(a);

            string state1 = RandomData.AlphaNumericString(10, false);
            string state2 = null;
            AsyncCallback callback = delegate(IAsyncResult ar)
                {
                    state2 = (string) ar.AsyncState;
                    var a2 = simol.EndGet<A2>(ar);

                    Assert.AreSame(a, a2);
                };

            simol.BeginGet<A2>(a.ItemName, callback, state1);

            WaitUtils.Default.WaitTillTrue(delegate { return state2 != null; });

            Assert.AreEqual(state1, state2);
        }
    }
}