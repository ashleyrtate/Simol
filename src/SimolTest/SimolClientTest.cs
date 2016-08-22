using System;
using System.Collections.Generic;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Coditate.Common.Util;
using Simol.Indexing;
using Simol.TestSupport;
using Coditate.TestSupport;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using AmazonAttribute = Amazon.SimpleDB.Model.Attribute;

namespace Simol
{
    /// <summary>
    /// Basic tests of top-level Simol implementation class to ensure
    /// calls are passed through decorators.
    /// </summary>
    [TestFixture]
    public class SimolClientTest
    {
        private IIndexer indexer;
        private SimolClient simol;
        private AmazonSimpleDB simpleDb;

        [SetUp]
        public void SetUp()
        {
            indexer = MockRepository.GenerateMock<IIndexer>();
            simpleDb = MockRepository.GenerateMock<AmazonSimpleDB>();

            var config = new SimolConfig
                {
                    Indexer = indexer,
                    Cache = null,
                    AutoCreateDomains = false
                };
            simol = new SimolClient(simpleDb, config);

            // reset the constraint call counts before each 
            // test since the type mappings are cached
            ItemMapping mapping = ItemMapping.Create(typeof(A2));
            ((TestDomainConstraint)mapping.Constraint).Reset();
            mapping = ItemMapping.Create(typeof(A));
            ((TestDomainConstraint)mapping.Constraint).Reset();
        }

        [TearDown]
        public void TearDown()
        {
            simpleDb.VerifyAllExpectations();
        }

        [Test]
        public void Constructor_StringString()
        {
            string awsId = "abc";
            string awsKey = "xyz";
            var ss = new SimolClient(awsId, awsKey);

            Assert.IsNotNull(ss.SimpleDB);
            Assert.IsNotNull(ss.Config);
        }

        [Test]
        public void Delete()
        {
            DeleteAttributesRequest deleteRequest;
            object itemName;
            CreateDeleteRequest(out deleteRequest, out itemName);

            // set mock expectations
            simpleDb.Expect(x => x.DeleteAttributes(deleteRequest)).Constraints(
                Property.AllPropertiesMatch(deleteRequest)).Return(null).Repeat.Twice();

            simol.Delete<A2>(itemName);
            simol.Delete<A2>(new List<object> { itemName });
        }

        [Test]
        public void DeleteAttributes()
        {
            DeleteAttributesRequest deleteRequest;
            object itemName;
            CreateDeleteRequest(out deleteRequest, out itemName);

            // set mock expectations
            simpleDb.Expect(x => x.DeleteAttributes(deleteRequest)).Constraints(
                Property.AllPropertiesMatch(deleteRequest)).Return(null).Repeat.Twice();

            ItemMapping mapping = ItemMapping.Create(typeof(A2));
            simol.DeleteAttributes(mapping, itemName);

            var testConstraint = (TestDomainConstraint)mapping.Constraint;
            Assert.AreEqual(1, testConstraint.BeforeDeleteCount);

            simol.DeleteAttributes(mapping, new List<object> { itemName });
        }

        [Test]
        public void DeleteAttributesT()
        {
            DeleteAttributesRequest deleteRequest;
            object itemName;
            CreateDeleteRequest(out deleteRequest, out itemName);

            // set mock expectations
            simpleDb.Expect(x => x.DeleteAttributes(deleteRequest)).Constraints(
                Property.AllPropertiesMatch(deleteRequest)).Return(null).Repeat.Twice();

            simol.DeleteAttributes<A2>(itemName);
            simol.DeleteAttributes<A2>(new List<object> { itemName });
        }

        [Test]
        public void Get()
        {
            GetAttributesResponse getResponse;
            A2 a1;
            CreateGetResponse(out getResponse, out a1);

            // set mock expectations
            simpleDb.Expect(x => x.GetAttributes(null)).Constraints(new Anything()).
                Return(getResponse);

            var a2 = simol.Get<A2>(a1.ItemName);

            AssertEqual(a1, a2);
        }

        [Test]
        public void GetAttributes()
        {
            GetAttributesResponse getResponse;
            A2 a1;
            CreateGetResponse(out getResponse, out a1);

            // set mock expectations
            simpleDb.Expect(x => x.GetAttributes(null)).Constraints(new Anything()).
                Return(getResponse);

            ItemMapping mapping = ItemMapping.Create(typeof(A2));
            PropertyValues values = simol.GetAttributes(mapping, a1.ItemName);

            AssertEqual(values, a1);
            var testConstraint = (TestDomainConstraint)mapping.Constraint;
            Assert.AreEqual(1, testConstraint.AfterLoadCount);
        }

        [Test]
        public void GetAttributes_CustomMapping()
        {
            GetAttributesResponse getResponse;
            A2 a1;
            CreateGetResponse(out getResponse, out a1);

            // set mock expectations
            simpleDb.Expect(x => x.GetAttributes(null)).Constraints(new Anything()).
                Return(getResponse);

            ItemMapping mapping = ItemMapping.Create("A2", AttributeMapping.Create("ItemName", typeof(Guid)));
            mapping.AttributeMappings.Add(AttributeMapping.Create("BooleanValue", typeof(bool)));
            mapping.AttributeMappings.Add(AttributeMapping.Create("IntValue", typeof(int)));
            mapping.AttributeMappings.Add(AttributeMapping.Create("StringValue", typeof(string)));

            PropertyValues values = simol.GetAttributes(mapping, a1.ItemName, "StringValue", "IntValue", "BooleanValue");

            AssertEqual(values, a1);
        }

        [Test]
        public void GetAttributesT()
        {
            GetAttributesResponse getResponse;
            A2 a1;
            CreateGetResponse(out getResponse, out a1);

            // set mock expectations
            simpleDb.Expect(x => x.GetAttributes(null)).Constraints(new Anything()).
                Return(getResponse);

            PropertyValues values = simol.GetAttributes<A>(a1.ItemName);

            AssertEqual(values, a1);
        }

        [Test]
        public void Put()
        {
            PutAttributesRequest putRequest;
            A2 a;
            CreatePutRequest(out putRequest, out a);

            // set mock expectations
            simpleDb.Expect(x => x.PutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null).Repeat.Twice();

            simol.Put(a);
            simol.Put<A2>(new List<A2> { a });
        }

        [Test,
         ExpectedException(typeof(SimolDataException),
             ExpectedMessage = "Item name of item 'Simol.TestSupport.A2' is null or an empty string.")]
        public void Put_NullItemName()
        {
            var a = new A2 { ItemName = null };
            simol.Put(a);
        }

        [Test,
         ExpectedException(typeof(ArgumentException),
             ExpectedMessage = "The item list may not contain null values.")]
        public void Put_NullItems()
        {
            simol.Put(new List<A2> { new A2(), null });
        }

        [Test]
        public void PutAttributes()
        {
            PutAttributesRequest putRequest;
            A2 a;
            CreatePutRequest(out putRequest, out a);

            // set mock expectations
            simpleDb.Expect(x => x.PutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null).Repeat.Twice();

            PropertyValues values = PropertyValues.CreateValues(a);
            ItemMapping mapping = ItemMapping.Create(typeof(A2));
            simol.PutAttributes(mapping, values);

            var testConstraint = (TestDomainConstraint)mapping.Constraint;
            Assert.AreEqual(1, testConstraint.BeforeSaveCount);

            simol.PutAttributes(mapping, new List<PropertyValues> { values });
        }

        [Test,
         ExpectedException(typeof(ArgumentException),
             ExpectedMessage =
                 "Invalid ItemName type. The ItemName 'abc' is expected to be a 'System.Guid' but is actually a 'System.String'."
             )]
        public void PutAttributes_InvalidItemName()
        {
            var propValues = new PropertyValues("abc");
            simol.PutAttributes<A>(propValues);
        }

        [Test,
         ExpectedException(typeof(ArgumentException),
             ExpectedMessage =
                 "'items[0]' has a problem: Invalid ItemName type. The ItemName 'abc' is expected to be a 'System.Guid' but is actually a 'System.String'."
             )]
        public void PutAttributesList_InvalidItemName()
        {
            var propValues = new PropertyValues("abc");
            simol.PutAttributes<A>(new List<PropertyValues> { propValues });
        }

        [Test]
        public void PutAttributesT()
        {
            PutAttributesRequest putRequest;
            A2 a;
            CreatePutRequest(out putRequest, out a);

            // set mock expectations
            simpleDb.Expect(x => x.PutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null).Repeat.Twice();

            PropertyValues values = PropertyValues.CreateValues(a);
            simol.PutAttributes<A2>(values);

            simol.PutAttributes<A2>(new List<PropertyValues> { values });
        }

        [Test]
        public void Select_CommandT()
        {
            SelectResponse selectResponse;
            List<A2> aList;
            CreateSelectResponse(out selectResponse, out aList);

            // set mock expectations
            simpleDb.Expect(x => x.Select(null)).Constraints(new Anything()).
                Return(selectResponse);

            SelectResults<A2> results = simol.Select(new SelectCommand<A2>("select from A"));

            AssertEqual(aList[0], results[0]);
            AssertEqual(aList[1], results[1]);
            AssertEqual(aList[2], results[2]);
        }

        [Test]
        public void Find()
        {
            GetAttributesResponse getResponse;
            A2 a1;
            CreateGetResponse(out getResponse, out a1);

            string itemId = a1.ItemName.ToString();
            ItemMapping mapping = ItemMapping.Create(typeof(A2));
            var itemIds = new List<string>
                {
                    itemId,
                    itemId,
                    itemId
                };
            string propertyName = RandomData.AlphaNumericString(5, false);
            string queryText = RandomData.AlphaNumericString(10, true);
            int resultStartIndex = RandomData.Generator.Next(0, 100);
            int resultCount = RandomData.Generator.Next(1, 100);

            // set mock expectations
            indexer.Expect(i => i.FindItems(mapping.DomainName, queryText, resultStartIndex, resultCount, propertyName))
                .Return(itemIds);
            simpleDb.Expect(x => x.GetAttributes(null)).IgnoreArguments().Return(getResponse).Repeat.Times(3);

            List<A2> items = simol.Find<A2>(queryText, resultStartIndex, resultCount, propertyName);

            Assert.AreEqual(itemIds.Count, items.Count);
            AssertEqual(items[0], a1);
            AssertEqual(items[1], a1);
            AssertEqual(items[2], a1);
        }

        [Test]
        public void FindAttributes()
        {
            // FindAttributes exercises almost exactly the same code as Find so don't bother passing back any items from mocks
            GetAttributesResponse getResponse;
            A2 a1;
            CreateGetResponse(out getResponse, out a1);
            ItemMapping mapping = ItemMapping.Create(typeof(A2));

            string propertyName = RandomData.AlphaNumericString(5, false);
            string queryText = RandomData.AlphaNumericString(10, true);
            int resultStartIndex = RandomData.Generator.Next(0, 100);
            int resultCount = RandomData.Generator.Next(1, 100);

            // set mock expectations
            indexer.Expect(i => i.FindItems(mapping.DomainName, queryText, resultStartIndex, resultCount, propertyName))
                .Return(new List<string>());

            List<PropertyValues> items = simol.FindAttributes<A2>(queryText, resultStartIndex, resultCount,
                                                                   propertyName);

            Assert.IsEmpty(items);
        }

        [Test,
         ExpectedException(typeof(SimolConfigurationException),
             ExpectedMessage =
                 "No full-text indexer is installed. You must provide an indexer via SimolConfig.Indexer before invoking the 'Find' methods."
             )]
        public void Find_NoIndexer()
        {
            simol.Config.Indexer = null;
            simol.Find<H>("abc", 0, 1, "");
        }

        [Test,
         ExpectedException(typeof(InvalidOperationException),
             ExpectedMessage =
                 "Unable to find items of type 'Simol.TestSupport.B' because the type has no indexed properties. At least one property must be marked with an IndexAttribute."
             )]
        public void Find_NoIndexedProperty()
        {
            simol.Find<B>("abc", 0, 1, "");
        }

        [Test]
        public void Select_Text()
        {
            SelectResponse selectResponse;
            List<A2> aList1;
            CreateSelectResponse(out selectResponse, out aList1);

            // set mock expectations
            simpleDb.Expect(x => x.Select(null)).Constraints(new Anything()).
                Return(selectResponse);

            List<A2> aList2 = simol.Select<A2>("select from A");

            AssertEqual(aList1[0], aList2[0]);
            AssertEqual(aList1[1], aList2[1]);
            AssertEqual(aList1[2], aList2[2]);
        }

        [Test]
        public void SelectAttributes_Command()
        {
            SelectResponse selectResponse;
            List<A2> aList;
            CreateSelectResponse(out selectResponse, out aList);

            // set mock expectations
            simpleDb.Expect(x => x.Select(null)).Constraints(new Anything()).
                Return(selectResponse);


            var command = new SelectCommand(typeof(A), "select from A");
            SelectResults<PropertyValues> results =
                simol.SelectAttributes(command);

            AssertEqual(results[0], aList[0]);
            AssertEqual(results[1], aList[1]);
            AssertEqual(results[2], aList[2]);

            var testConstraint = (TestDomainConstraint)command.Mapping.Constraint;
            Assert.AreEqual(results.Count, testConstraint.AfterLoadCount);
        }

        [Test]
        public void SelectAttributes_CommandT()
        {
            SelectResponse selectResponse;
            List<A2> aList;
            CreateSelectResponse(out selectResponse, out aList);

            // set mock expectations
            simpleDb.Expect(x => x.Select(null)).Constraints(new Anything()).
                Return(selectResponse);

            SelectResults<PropertyValues> results = simol.SelectAttributes(new SelectCommand<A>("select from A"));

            AssertEqual(results[0], aList[0]);
            AssertEqual(results[1], aList[1]);
            AssertEqual(results[2], aList[2]);
        }

        [Test]
        public void SelectAttributes_TextT()
        {
            SelectResponse selectResponse;
            List<A2> aList;
            CreateSelectResponse(out selectResponse, out aList);

            // set mock expectations
            simpleDb.Expect(x => x.Select(null)).Constraints(new Anything()).
                Return(selectResponse);

            List<PropertyValues> results = simol.SelectAttributes<A>("select from A");

            AssertEqual(results[0], aList[0]);
            AssertEqual(results[1], aList[1]);
            AssertEqual(results[2], aList[2]);
        }

        [Test]
        public void SelectScalar()
        {
            string itemName = Guid.NewGuid().ToString();
            string expectedValue = RandomData.AlphaNumericString(10, false);
            var selectResponse = new SelectResponse
                {
                    SelectResult = new SelectResult()
                };
            var item = new Item { Attribute = { new AmazonAttribute { Name = "StringValue", Value = expectedValue } }, Name = itemName };
            selectResponse.SelectResult.Item = new List<Item> { item };

            // set mock expectations
            simpleDb.Expect(x => x.Select(null)).Constraints(new Anything()).
                Return(selectResponse);

            var value = (string)simol.SelectScalar(new SelectCommand(typeof(A2), "select StringValue from A2"));

            Assert.AreEqual(expectedValue, value);
        }

        [Test]
        public void SelectScalarT()
        {
            string itemName = Guid.NewGuid().ToString();
            string expectedValue = RandomData.AlphaNumericString(10, false);
            var selectResponse = new SelectResponse
                {
                    SelectResult = new SelectResult()
                };
            var item = new Item { Attribute = { new AmazonAttribute { Name = "StringValue", Value = expectedValue } }, Name = itemName };
            selectResponse.SelectResult.Item = new List<Item> { item };

            // set mock expectations
            simpleDb.Expect(x => x.Select(null)).Constraints(new Anything()).
                Return(selectResponse);

            var value = (string)simol.SelectScalar<A>("select StringValue from A2");

            Assert.AreEqual(expectedValue, value);
        }

        private void AssertEqual(A2 a1, A2 a2)
        {
            PropertyMatcher.MatchResult result = PropertyMatcher.AreEqual(a1, a2);
            Assert.IsTrue(result.Equal, result.Message);
        }

        private void AssertEqual(PropertyValues values, A2 a)
        {
            Assert.AreEqual(a.BooleanValue, values["BooleanValue"]);
            Assert.AreEqual(a.StringValue, values["StringValue"]);
            Assert.AreEqual(a.IntValue, values["IntValue"]);
            Assert.AreEqual(a.ItemName, values.ItemName);
        }

        private void AddAttributes(List<ReplaceableAttribute> putAttributes, List<AmazonAttribute> getAttributes,
                                   string propertyName, string valueString)
        {
            if (putAttributes != null)
            {
                putAttributes.Add(new ReplaceableAttribute
                    {
                        Name = propertyName,
                        Replace = true,
                        Value = valueString
                    });
            }
            if (getAttributes != null)
            {
                getAttributes.Add(new AmazonAttribute
                    {
                        Name = propertyName,
                        Value = valueString
                    });
            }
        }

        private List<A2> CreateA2PlusSelectItems(int count, List<Item> items)
        {
            var aList = new List<A2>();
            for (int k = 0; k < count; k++)
            {
                var i = new Item();
                A2 a = CreateA2PlusAttributes(null, i.Attribute);
                i.Name = a.ItemName.ToString();

                items.Add(i);
                aList.Add(a);
            }
            return aList;
        }

        private A2 CreateA2PlusAttributes(List<ReplaceableAttribute> putAttributes, List<AmazonAttribute> getAttributes)
        {
            var a = new A2
                {
                    BooleanValue = true,
                    IntValue = 1234567890,
                    StringValue = "the quick red fox"
                };
            // add in alphabetical order so Property.AllPropertiesMatch constraint works
            AddAttributes(putAttributes, getAttributes, "BooleanValue", "True");
            AddAttributes(putAttributes, getAttributes, "IntValue", "11234567890");
            AddAttributes(putAttributes, getAttributes, "StringValue", "the quick red fox");

            return a;
        }

        private void CreatePutRequest(out PutAttributesRequest putRequest, out A2 a)
        {
            putRequest = new PutAttributesRequest
                {
                    DomainName = typeof(A2).Name
                };

            a = CreateA2PlusAttributes(putRequest.Attribute, null);
            putRequest.ItemName = a.ItemName.ToString();
        }

        private void CreateDeleteRequest(out DeleteAttributesRequest deleteRequest, out object itemName)
        {
            itemName = Guid.NewGuid();
            deleteRequest = new DeleteAttributesRequest
                {
                    DomainName = typeof(A2).Name,
                    ItemName = itemName.ToString()
                };
        }

        private void CreateGetResponse(out GetAttributesResponse getResponse, out A2 a)
        {
            getResponse = new GetAttributesResponse
                {
                    GetAttributesResult = new GetAttributesResult()
                };

            a = CreateA2PlusAttributes(null, getResponse.GetAttributesResult.Attribute);
        }

        private void CreateSelectResponse(out SelectResponse selectResponse, out List<A2> aList)
        {
            selectResponse = new SelectResponse
                {
                    SelectResult = new SelectResult()
                };
            var items = new List<Item>();
            aList = CreateA2PlusSelectItems(3, items);
            selectResponse.SelectResult.Item = items;
        }
    }
}