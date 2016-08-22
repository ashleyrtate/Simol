using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Coditate.Common.Util;
using Simol.Formatters;
using Simol.TestSupport;
using Coditate.TestSupport;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using AmazonAttribute=Amazon.SimpleDB.Model.Attribute;

namespace Simol.Core
{
    [TestFixture]
    public class SimpleDbSimolTest
    {
        private static readonly string[] ExcludedAProperties = {"ObjectValue"};
        private SimpleDbSimol simol;
        private AmazonSimpleDB simpleDb;
        private SpanUtils spanUtils;

        [SetUp]
        public void SetUp()
        {
            simpleDb = MockRepository.GenerateMock<AmazonSimpleDB>();
            var config = new SimolConfig();
            config.NullPutBehavior = RandomData.EnumValue<NullBehavior>();
            simol = new SimpleDbSimol(config, simpleDb);
            spanUtils = new SpanUtils(config);
        }

        [TearDown]
        public void TearDown()
        {
            simpleDb.VerifyAllExpectations();
        }

        private void AssertEqual(A a, A a2)
        {
            Assert.AreEqual(a.IntGenericCollection.Count, a2.IntGenericCollection.Count);
            foreach (int i in a.IntGenericCollection)
            {
                Assert.Contains(i, a2.IntGenericCollection);
            }
            Assert.AreEqual(a.IntGenericList.Count, a2.IntGenericList.Count);
            foreach (int i in a.IntGenericList)
            {
                Assert.Contains(i, a2.IntGenericList);
            }

            PropertyMatcher.MatchResult result = PropertyMatcher.AreEqual(a, a2, ExcludedAProperties);
            Assert.IsTrue(result.Equal, result.Message);
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

        private List<A> CreateAPlusSelectItems(int count, List<Item> items)
        {
            var aList = new List<A>();
            for (int k = 0; k < count; k++)
            {
                var i = new Item();
                A a = CreateAPlusAttributes(null, i.Attribute);
                i.Name = a.ItemName.ToString();

                items.Add(i);
                aList.Add(a);
            }
            return aList;
        }

        private A CreateAPlusAttributes(List<ReplaceableAttribute> putAttributes, List<AmazonAttribute> getAttributes)
        {
            var a = new A
                {
                    BooleanValue = true,
                    ByteValue = 123,
                    CharValue = 'z',
                    DateTimeValue = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    DecimalValue = 123456789012345678.0123456789m,
                    DoubleValue = 1234567.12345678,
                    EnumValue = TestEnum.Second,
                    FloatValue = 123.1234f,
                    IntArray = new[] {1, 2, 3},
                    IntGenericCollection = new Collection<int> {7, 8, 9},
                    IntGenericList = new List<int> {4, 5, 6},
                    IntValue = 1234567890,
                    LongValue = 1234567890123456789,
                    ObjectValue = new object(),
                    SByteValue = 123,
                    ShortValue = 12345,
                    StringValue = "the quick red fox",
                    UIntValue = 1234567890,
                    ULongValue = 12345678901234567890,
                    UShortValue = 12345,
                    NullableIntValue = 1234567890,
                    NullableEnumValue = TestEnum.First,
                    NullableIntGenericList = new List<int?> {10, 11, 12},
                    stringvalue = "jumped over the lazy brown dog"
                };
            // add in alphabetical order so Property.AllPropertiesMatch constraint works
            AddAttributes(putAttributes, getAttributes, "BooleanValue", "True");
            AddAttributes(putAttributes, getAttributes, "ByteValue", "123");
            AddAttributes(putAttributes, getAttributes, "CharValue", "z");
            AddAttributes(putAttributes, getAttributes, "DateTimeValue", "2000-01-01T00:00:00.000-05:00");
            AddAttributes(putAttributes, getAttributes, "DecimalValue", "1123456789012345678.0123456789");
            AddAttributes(putAttributes, getAttributes, "DoubleValue", "11234567.12345678");
            AddAttributes(putAttributes, getAttributes, "EnumValue", "Second");
            AddAttributes(putAttributes, getAttributes, "FloatValue", "1123.1234");
            AddAttributes(putAttributes, getAttributes, "IntGenericCollection", "10000000007");
            AddAttributes(putAttributes, getAttributes, "IntGenericCollection", "10000000008");
            AddAttributes(putAttributes, getAttributes, "IntGenericCollection", "10000000009");
            AddAttributes(putAttributes, getAttributes, "IntGenericList", "10000000004");
            AddAttributes(putAttributes, getAttributes, "IntGenericList", "10000000005");
            AddAttributes(putAttributes, getAttributes, "IntGenericList", "10000000006");
            AddAttributes(putAttributes, getAttributes, "IntValue", "11234567890");
            AddAttributes(putAttributes, getAttributes, "LongValue", "11234567890123456789");
            AddAttributes(putAttributes, getAttributes, "NullableEnumValue", "First");

            AddAttributes(putAttributes, getAttributes, "NullableIntGenericList", "10000000010");
            AddAttributes(putAttributes, getAttributes, "NullableIntGenericList", "10000000011");
            AddAttributes(putAttributes, getAttributes, "NullableIntGenericList", "10000000012");

            AddAttributes(putAttributes, getAttributes, "NullableIntValue", "11234567890");
            AddAttributes(putAttributes, getAttributes, "SByteValue", "1123");
            AddAttributes(putAttributes, getAttributes, "ShortValue", "112345");
            AddAttributes(putAttributes, getAttributes, "stringvalue", "jumped over the lazy brown dog");
            AddAttributes(putAttributes, getAttributes, "StringValue", "the quick red fox");
            AddAttributes(putAttributes, getAttributes, "UIntValue", "1234567890");
            AddAttributes(putAttributes, getAttributes, "ULongValue", "12345678901234567890");
            AddAttributes(putAttributes, getAttributes, "UShortValue", "12345");

            return a;
        }

        [Test]
        public void DeleteAttributes()
        {
            Guid itemName = Guid.NewGuid();
            var deleteRequest = new DeleteAttributesRequest
                {
                    DomainName = typeof (A).Name,
                    ItemName = itemName.ToString()
                };

            // set mock expectations
            simpleDb.Expect(x => x.DeleteAttributes(deleteRequest)).Constraints(
                Property.AllPropertiesMatch(deleteRequest)).
                Return(null);

            // generate expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (A));
            simol.DeleteAttributes(mapping, itemName.ToUniList(), null);
        }

        [Test]
        public void DeleteAttributes_Batch()
        {
            var itemNames = new List<object> { Guid.NewGuid(), Guid.NewGuid()};
            var deleteRequest = new BatchDeleteAttributesRequest
            {
                DomainName = typeof(A).Name,
                Item = {
                    new DeleteableItem{ItemName=itemNames[0].ToString()},
                    new DeleteableItem{ItemName=itemNames[1].ToString()}
                }
            };

            // set mock expectations
            simpleDb.Expect(x => x.BatchDeleteAttributes(deleteRequest)).Constraints(
                Property.AllPropertiesMatch(deleteRequest)).
                Return(null);

            // generate expected calls
            ItemMapping mapping = ItemMapping.Create(typeof(A));
            simol.DeleteAttributes(mapping, itemNames, null);
        }

        [Test]
        public void DeleteAttributes_Batch_PropertyNames()
        {
            var itemNames = new List<object> { Guid.NewGuid(), Guid.NewGuid() };
            var deleteRequest = new BatchDeleteAttributesRequest
            {
                DomainName = typeof(A).Name,
                Item = {
                    new DeleteableItem{ItemName=itemNames[0].ToString(), Attribute = {new AmazonAttribute {Name = "StringValue"}}},
                    new DeleteableItem{ItemName=itemNames[1].ToString(), Attribute = {new AmazonAttribute {Name = "StringValue"}}}
                }
            };

            // set mock expectations
            simpleDb.Expect(x => x.BatchDeleteAttributes(deleteRequest)).Constraints(
                Property.AllPropertiesMatch(deleteRequest)).
                Return(null);

            // generate expected calls
            ItemMapping mapping = ItemMapping.Create(typeof(A));
            simol.DeleteAttributes(mapping, itemNames, "StringValue".ToUniList());
        }

        [Test]
        public void DeleteAttributes_PropertyNames()
        {
            Guid itemName = Guid.NewGuid();
            var deleteRequest = new DeleteAttributesRequest
                {
                    DomainName = typeof (A).Name,
                    ItemName = itemName.ToString(),
                    Attribute = {new AmazonAttribute {Name = "StringValue"}}
                };

            // set mock expectations
            simpleDb.Expect(x => x.DeleteAttributes(deleteRequest)).Constraints(
                Property.AllPropertiesMatch(deleteRequest)).
                Return(null);

            // generate expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (A));
            simol.DeleteAttributes(mapping, itemName.ToUniList(), "StringValue".ToUniList());
        }

        [Test]
        public void Get_ExtraAttributesReturned()
        {
            Guid itemName = Guid.NewGuid();
            var getRequest = new GetAttributesRequest
                {
                    DomainName = typeof (A).Name,
                    ItemName = itemName.ToString()
                };

            var getResponse = new GetAttributesResponse
                {
                    GetAttributesResult = new GetAttributesResult()
                };

            AddAttributes(null, getResponse.GetAttributesResult.Attribute, "ExtraAttribute1", "true");
            AddAttributes(null, getResponse.GetAttributesResult.Attribute, "ExtraAttribute2", "123");

            // set mock expectations
            simpleDb.Expect(x => x.GetAttributes(getRequest)).Constraints(Property.AllPropertiesMatch(getRequest)).
                Return(getResponse);

            // generate expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (A));
            PropertyValues values = simol.GetAttributes(mapping, itemName, null);
            object a = PropertyValues.CreateItem(typeof (A), values);

            Assert.IsNotNull(a);
        }

        [Test]
        public void Get_NoAttributesReturned()
        {
            Guid itemName = Guid.NewGuid();
            var getRequest = new GetAttributesRequest
                {
                    DomainName = typeof (A).Name,
                    ItemName = itemName.ToString()
                };

            var getResponse = new GetAttributesResponse
                {
                    GetAttributesResult = new GetAttributesResult()
                };

            // set mock expectations
            simpleDb.Expect(x => x.GetAttributes(getRequest)).Constraints(Property.AllPropertiesMatch(getRequest)).
                Return(getResponse);

            // generate expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (A));
            PropertyValues values = simol.GetAttributes(mapping, itemName,null);

            Assert.IsNull(values);
        }

        [Test]
        public void Put_Batch()
        {
            var putRequest = new BatchPutAttributesRequest
                {
                    DomainName = typeof (A).Name
                };

            var allAs = new List<A>();
            int count = RandomData.Generator.Next(2, 10);
            for (int k = 0; k < count; k++)
            {
                var item = new ReplaceableItem();
                A a = CreateAPlusAttributes(item.Attribute, null);
                foreach (var att in item.Attribute)
                {
                    att.Replace = simol.Config.BatchReplaceAttributes;
                }
                // vary at least one data field
                a.BooleanValue = RandomData.Bool();
                item.Attribute[0].Value = a.BooleanValue.ToString();
                item.ItemName = a.ItemName.ToString();

                putRequest.Item.Add(item);
                allAs.Add(a);
            }

            // set mock expectations
            simpleDb.Expect(x => x.BatchPutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null);

            // generate expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (A));
            var allValues = new List<PropertyValues>();
            foreach (A a in allAs)
            {
                PropertyValues values = PropertyValues.CreateValues(a);
                allValues.Add(values);
            }
            simol.PutAttributes(mapping, allValues);
        }

        [Test]
        public void PutAttributes_NoPropertyValues()
        {
            // simply verifies that call is NOT passed through to mock
            ItemMapping mapping = ItemMapping.Create(typeof (A));
            simol.PutAttributes(mapping, new List<PropertyValues> { });
        }

        [Test]
        public void PutAttributes_MissingValuesIgnored()
        {
            // PutAttributes with an ItemMapping that maps properties not included in the PropertyValues collection
            ItemMapping mapping = ItemMapping.Create(typeof (A));
            var values = new PropertyValues(Guid.NewGuid());
            values["IntValue"] = 2;

            Predicate<PutAttributesRequest> putPredicate = delegate(PutAttributesRequest request)
                {
                    Assert.AreEqual(1, request.Attribute.Count);
                    ReplaceableAttribute intAttribute =
                        request.Attribute.Where(a => a.Name == "IntValue").FirstOrDefault();
                    Assert.AreEqual(intAttribute.Value, "10000000002");

                    return true;
                };
            simpleDb.Expect(s => s.PutAttributes(Arg<PutAttributesRequest>.Matches(p => putPredicate.Invoke(p))));

            simol.PutAttributes(mapping, values.ToUniList());
        }

        [Test]
        public void PutGet_Attributes()
        {
            var putRequest = new PutAttributesRequest
                {
                    DomainName = typeof (A).Name
                };
            var getRequest = new GetAttributesRequest
                {
                    DomainName = typeof (A).Name,
                    AttributeName = {"StringValue"}
                };

            var getResponse = new GetAttributesResponse
                {
                    GetAttributesResult = new GetAttributesResult()
                };

            var a = new A {StringValue = RandomData.AsciiString(10)};
            putRequest.ItemName = a.ItemName.ToString();
            getRequest.ItemName = a.ItemName.ToString();

            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "StringValue", a.StringValue);

            // set mock expectations
            simpleDb.Expect(x => x.PutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null);
            simpleDb.Expect(x => x.GetAttributes(getRequest)).Constraints(Property.AllPropertiesMatch(getRequest)).
                Return(getResponse);

            // generate expected calls
            var values1 = new PropertyValues(a.ItemName);
            values1["StringValue"] = a.StringValue;
            ItemMapping mapping = ItemMapping.Create(typeof (A), new List<string> {"StringValue"});
            simol.PutAttributes(mapping, values1.ToUniList());

            PropertyValues values2 = simol.GetAttributes(mapping, values1.ItemName, "StringValue".ToUniList());

            Assert.AreEqual(values1.ItemName, values2.ItemName);
            Assert.AreEqual(values1["StringValue"], values2["StringValue"]);
        }

        [Test]
        public void PutGet_CustomFormats()
        {
            var tempBuffer = new byte[100];
            RandomData.Generator.NextBytes(tempBuffer);
            var b = new B
                {
                    FormattedIntItemName = 99,
                    ByteArrayValue = tempBuffer,
                    DictionaryValue = new Dictionary<string, string> {{"key1", "value1"}, {"key2", "value2"}},
                    ConvertedIntValue = 1,
                    FormattedIntValue = 00002,
                    SizedDoubleValue = .123456789012345,
                    SizedIntValue = 3,
                    IncludedIntValue = 4,
                    ExcludedIntValue = -1,
                    RenamedIntValue = 5,
                    LongStringValue = RandomData.AlphaNumericString(RandomData.Generator.Next(0, 10000), true)
                };

            var putRequest = new PutAttributesRequest
                {
                    DomainName = "CustomB",
                    ItemName = "0000000099"
                };
            var getRequest = new GetAttributesRequest
                {
                    DomainName = "CustomB",
                    ItemName = "0000000099"
                };
            var getResponse = new GetAttributesResponse
                {
                    GetAttributesResult = new GetAttributesResult()
                };

            // add in alphabetical order so property-based expectation matching works
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "ByteArrayValue",
                          Convert.ToBase64String(tempBuffer));
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "ConvertedIntValue", "1");
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "CustomAttributeNameInt", "5");
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "DictionaryValue",
                          "key1|value1");
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "DictionaryValue",
                          "key2|value2");
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "FormattedIntValue", "2");
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "IncludedIntValue",
                          "10000000004");

            List<string> chunks = spanUtils.SplitPropertyValue(b.LongStringValue, SpanType.Span);
            foreach (string chunk in chunks)
            {
                AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "LongStringValue",
                              chunk);
            }

            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "SizedDoubleValue",
                          ".123456789012345");
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "SizedIntValue", "13");

            simpleDb.Expect(x => x.PutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null);
            simpleDb.Expect(x => x.GetAttributes(getRequest)).Constraints(Property.AllPropertiesMatch(getRequest)).
                Return(getResponse);

            // generate the expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (B));
            PropertyValues values = PropertyValues.CreateValues(b);

            simol.PutAttributes(mapping, values.ToUniList());
            PropertyValues values2 = simol.GetAttributes(mapping, b.FormattedIntItemName, null);
            var b2 = (B) PropertyValues.CreateItem(typeof (B), values2);

            Assert.AreEqual(b.ByteArrayValue, b2.ByteArrayValue);
            Assert.AreEqual(b.DictionaryValue.Count, b2.DictionaryValue.Count);
            foreach (string key in b.DictionaryValue.Keys)
            {
                Assert.AreEqual(b.DictionaryValue[key], b2.DictionaryValue[key]);
            }

            Assert.AreEqual(b.ConvertedIntValue, b2.ConvertedIntValue);
            Assert.AreNotEqual(b.ExcludedIntValue, b2.ExcludedIntValue);
            Assert.AreEqual(b.FormattedIntItemName, b2.FormattedIntItemName);
            Assert.AreEqual(b.FormattedIntValue, b2.FormattedIntValue);
            Assert.AreEqual(b.IncludedIntValue, b2.IncludedIntValue);
            Assert.AreEqual(b.SizedDoubleValue, b2.SizedDoubleValue);
            Assert.AreEqual(b.SizedIntValue, b2.SizedIntValue);
        }

        [Test]
        public void PutGet_DefaultFormat()
        {
            var putRequest = new PutAttributesRequest
                {
                    DomainName = typeof (A).Name
                };
            var getRequest = new GetAttributesRequest
                {
                    DomainName = typeof (A).Name,
                };

            var getResponse = new GetAttributesResponse
                {
                    GetAttributesResult = new GetAttributesResult()
                };

            A a = CreateAPlusAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute);
            putRequest.ItemName = a.ItemName.ToString();
            getRequest.ItemName = a.ItemName.ToString();

            // set mock expectations
            simpleDb.Expect(x => x.PutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null);
            simpleDb.Expect(x => x.GetAttributes(getRequest)).Constraints(Property.AllPropertiesMatch(getRequest)).
                Return(getResponse);

            // generate expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (A));
            PropertyValues values = PropertyValues.CreateValues(a);
            simol.PutAttributes(mapping, values.ToUniList());

            PropertyValues values2 = simol.GetAttributes(mapping, a.ItemName, null);
            var a2 = (A) PropertyValues.CreateItem(typeof (A), values2);

            AssertEqual(a, a2);
        }

        [Test]
        public void PutGet_ExcludedAttributes()
        {
            var e = new E
                {
                    BooleanValue = true,
                    DoubleValue = 1.1
                };

            var putRequest = new PutAttributesRequest
                {
                    DomainName = typeof (E).Name,
                    ItemName = e.ItemName.ToString()
                };
            var getRequest = new GetAttributesRequest
                {
                    DomainName = typeof (E).Name,
                    ItemName = e.ItemName.ToString()
                };
            var getResponse = new GetAttributesResponse
                {
                    GetAttributesResult = new GetAttributesResult()
                };

            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "BooleanValue", "True");
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "DoubleValue", "10000001.1");

            simpleDb.Expect(x => x.PutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null);
            simpleDb.Expect(x => x.GetAttributes(getRequest)).Constraints(Property.AllPropertiesMatch(getRequest)).
                Return(getResponse);

            // generate the expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (E));
            PropertyValues values = PropertyValues.CreateValues(e);
            simol.PutAttributes(mapping, values.ToUniList());

            PropertyValues values2 = simol.GetAttributes(mapping, e.ItemName, null);
            var e2 = (E) PropertyValues.CreateItem(typeof (E), values2);

            Assert.AreEqual(e.BooleanValue, e2.BooleanValue);
            Assert.AreEqual(e.DoubleValue, e2.DoubleValue);
            Assert.AreEqual(e.IntValue, e2.IntValue);
            Assert.AreEqual(e.ItemName, e2.ItemName);
        }

        [Test]
        public void PutGet_IncludedAttributes()
        {
            var d = new D
                {
                    IntValue = 2
                };

            var putRequest = new PutAttributesRequest
                {
                    DomainName = typeof (D).Name,
                    ItemName = d.ItemName.ToString()
                };
            var getRequest = new GetAttributesRequest
                {
                    DomainName = typeof (D).Name,
                    ItemName = d.ItemName.ToString()
                };
            var getResponse = new GetAttributesResponse
                {
                    GetAttributesResult = new GetAttributesResult()
                };

            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "IntValue", "10000000002");

            simpleDb.Expect(x => x.PutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null);
            simpleDb.Expect(x => x.GetAttributes(getRequest)).Constraints(Property.AllPropertiesMatch(getRequest)).
                Return(getResponse);

            // generate the expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (D));
            PropertyValues values = PropertyValues.CreateValues(d);
            simol.PutAttributes(mapping, values.ToUniList());

            PropertyValues values2 = simol.GetAttributes(mapping, d.ItemName, null);
            var d2 = (D) PropertyValues.CreateItem(typeof (D), values2);

            Assert.AreEqual(d.BooleanValue, d2.BooleanValue);
            Assert.AreEqual(d.DoubleValue, d2.DoubleValue);
            Assert.AreEqual(d.IntValue, d2.IntValue);
            Assert.AreEqual(d.ItemName, d2.ItemName);
        }

        [Test]
        public void PutGet_NullAndEmptyAttributes()
        {
            var c = new C
                {
                    StringValue = "",
                    ListOfNullsValue = new List<int?> {null, null, null},
                    LongStringValue2 = ""
                };

            var putRequest = new PutAttributesRequest
                {
                    DomainName = typeof (C).Name,
                    ItemName = c.ItemName.ToString()
                };
            var getRequest = new GetAttributesRequest
                {
                    DomainName = typeof (C).Name,
                    ItemName = c.ItemName.ToString()
                };
            var getResponse = new GetAttributesResponse
                {
                    GetAttributesResult = new GetAttributesResult()
                };
            if (simol.Config.NullPutBehavior == NullBehavior.MarkAsNull)
            {
                AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "BooleanValue",
                              PropertyFormatter.NullString);
                AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "DoubleValue",
                              PropertyFormatter.NullString);
                AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "EmptyListValue",
                              PropertyFormatter.NullString);

                AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "IntValue",
                              PropertyFormatter.NullString);
                AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "ListOfNullsValue",
                              PropertyFormatter.NullString);
                AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "LongStringValue",
                              PropertyFormatter.NullString);

                getResponse.GetAttributesResult.Attribute[0].Value = PropertyFormatter.Base64NullString;
                getResponse.GetAttributesResult.Attribute[1].Value = PropertyFormatter.Base64NullString;
                getResponse.GetAttributesResult.Attribute[2].Value = PropertyFormatter.Base64NullString;
                getResponse.GetAttributesResult.Attribute[3].Value = PropertyFormatter.Base64NullString;
                getResponse.GetAttributesResult.Attribute[4].Value = PropertyFormatter.Base64NullString;
                getResponse.GetAttributesResult.Attribute[5].Value = PropertyFormatter.Base64NullString;
            }
            // include empty long and normal string attributes - both to test and ensure we get a response back
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "LongStringValue2", "000");
            AddAttributes(putRequest.Attribute, getResponse.GetAttributesResult.Attribute, "StringValue", "");

            simpleDb.Expect(x => x.PutAttributes(putRequest)).Constraints(Property.AllPropertiesMatch(putRequest)).
                Return(null);
            simpleDb.Expect(x => x.GetAttributes(getRequest)).Constraints(Property.AllPropertiesMatch(getRequest)).
                Return(getResponse);

            // generate the expected calls
            ItemMapping mapping = ItemMapping.Create(typeof (C));
            PropertyValues values = PropertyValues.CreateValues(c);
            simol.PutAttributes(mapping, values.ToUniList());

            PropertyValues values2 = simol.GetAttributes(mapping, c.ItemName, null);
            var c2 = (C) PropertyValues.CreateItem(typeof (C), values2);

            Assert.IsNotNull(c2);
            Assert.IsNull(c2.BooleanValue);
            Assert.IsNull(c2.DoubleValue);
            Assert.IsNull(c2.IntValue);
            Assert.IsEmpty(c2.EmptyListValue);
            Assert.IsEmpty(c2.ListOfNullsValue);
            Assert.AreEqual(c.StringValue, c2.StringValue);
            Assert.AreEqual(c.ItemName, c2.ItemName);
            Assert.IsNull(c2.LongStringValue);
            Assert.AreEqual(c.LongStringValue2, c2.LongStringValue2);
        }

        [Test]
        public void Select()
        {
            string commandText = "select * from A";
            var selectRequest = new SelectRequest
                {
                    SelectExpression = commandText
                };

            var selectResponse = new SelectResponse
                {
                    SelectResult = new SelectResult()
                };

            List<A> aList = CreateAPlusSelectItems(3, selectResponse.SelectResult.Item);

            simpleDb.Expect(x => x.Select(selectRequest)).Constraints(Property.AllPropertiesMatch(selectRequest)).Return
                (selectResponse);

            var aList2 = new List<A>();
            var command = new SelectCommand(typeof (A), commandText);
            SelectResults<PropertyValues> results = simol.SelectAttributes(command);
            foreach (PropertyValues result in results)
            {
                Assert.IsTrue(result.IsCompleteSet);
                var item = (A) PropertyValues.CreateItem(typeof (A), result);
                aList2.Add(item);
            }

            for (int k = 0; k < aList.Count; k++)
            {
                AssertEqual(aList[k], aList2[k]);
            }
        }

        /// <summary>
        /// Verifies that command cancellation terminates a request early.
        /// </summary>
        [Test]
        public void Select_Cancelled()
        {
            string commandText = "select from A";
            var selectRequest = new SelectRequest
                {
                    SelectExpression = commandText
                };

            var selectResponse = new SelectResponse
                {
                    SelectResult = new SelectResult
                        {
                            NextToken = "123",
                            Item = {new Item {Name = Guid.NewGuid().ToString()}}
                        }
                };

            var command = new SelectCommand(typeof (A), commandText);
            SelectResults<PropertyValues> results = null;

            // setup function to cancel command before returning first response
            Func<SelectRequest, SelectResponse> waitAndReturn = delegate
                {
                    command.Cancel();
                    return selectResponse;
                };

            // set mock expectations to invoke function
            simpleDb.Expect(x => x.Select(selectRequest)).Constraints(new Anything()).Do(waitAndReturn);

            // setup delegate to call Select from new thread
            ParameterizedThreadStart threadStart = delegate { results = simol.SelectAttributes(command); };
            var threadRunner = new TestThreadRunner();
            threadRunner.AddThread(threadStart, null);
            threadRunner.Run();

            // verify that command cancellation was propagate to result object
            Assert.IsTrue(results.WasCommandCancelled);
        }

        [Test]
        public void Select_MultipleRequests()
        {
            string commandText = "select from A where BooleanValue = @BooleanValue";
            string expandedCommand = "select from A where BooleanValue = 'True'";

            var aListAll = new List<A>();
            string initialPageToken = RandomData.AsciiString(10);
            string pageToken = initialPageToken;
            int requests = RandomData.Generator.Next(1, 10);
            int countPerRequest = RandomData.Generator.Next(1, 20);
            for (int k = 0; k < requests; k++)
            {
                var selectRequest = new SelectRequest
                    {
                        NextToken = pageToken,
                        SelectExpression = expandedCommand
                    };

                pageToken = RandomData.AsciiString(10);
                var selectResponse = new SelectResponse
                    {
                        SelectResult = new SelectResult
                            {
                                NextToken = pageToken,
                                Item = {}
                            }
                    };

                List<A> alist = CreateAPlusSelectItems(countPerRequest, selectResponse.SelectResult.Item);
                aListAll.AddRange(alist);

                simpleDb.Expect(x => x.Select(selectRequest)).Constraints(Property.AllPropertiesMatch(selectRequest)).
                    Return(selectResponse);
            }

            var command = new SelectCommand(typeof (A), commandText, new CommandParameter("BooleanValue", true))
                {MaxResultPages = requests, PaginationToken = initialPageToken};
            SelectResults<PropertyValues> results = simol.SelectAttributes(command);

            Assert.AreEqual(command.MaxResultPages*countPerRequest, results.Count);

            // verify that the returned page token is the one set on the last returned response
            Assert.AreEqual(pageToken, results.PaginationToken);

            // verify SelectResults enumerator
            foreach (PropertyValues result in results)
            {
                Assert.IsNotNull(result);
            }

            // convert to items and verify equality
            var aList2 = new List<A>();
            foreach (PropertyValues result in results)
            {
                var item = (A) PropertyValues.CreateItem(typeof (A), result);
                aList2.Add(item);
            }

            for (int k = 0; k < results.Count; k++)
            {
                AssertEqual(aListAll[k], aList2[k]);
            }
        }

        [Test]
        public void Select_ReturnsAllResults()
        {
            var aListAll = new List<A>();
            int requests = RandomData.Generator.Next(5);
            int countPerRequest = RandomData.Generator.Next(50);

            for (int k = 0; k < requests; k++)
            {
                var selectResponse = new SelectResponse
                    {
                        SelectResult = new SelectResult
                            {
                                NextToken = RandomData.AsciiString(10)
                            }
                    };

                List<A> alist = CreateAPlusSelectItems(countPerRequest, selectResponse.SelectResult.Item);
                aListAll.AddRange(alist);

                simpleDb.Expect(x => x.Select(null)).IgnoreArguments().Return(selectResponse).Repeat.Once();
            }
            // mock an empty response WITH a NextToken. Simol should keep returning results as long 
            // as the next token indicates data is available, as some complex operations may time out before 
            // getting ANY results
            simpleDb.Expect(x => x.Select(null)).IgnoreArguments().Return(new SelectResponse
                {SelectResult = new SelectResult {NextToken = RandomData.AsciiString(10)}}).Repeat.Once();

            // mock an empty response WITHOUT a NextToken to terminate the select operation
            simpleDb.Expect(x => x.Select(null)).IgnoreArguments().Return(new SelectResponse
                {SelectResult = new SelectResult()}).Repeat.Once();

            var command = new SelectCommand(typeof (A), "select from A");
            SelectResults<PropertyValues> results = simol.SelectAttributes(command);
            Assert.AreEqual(aListAll.Count, results.Count);
        }

        [Test]
        public void SelectScalar()
        {
            int expectedValue = 1000;
            string commandText = "select IntValue from A";
            var selectRequest = new SelectRequest
                {
                    SelectExpression = commandText
                };

            var selectResponse = new SelectResponse
                {
                    SelectResult = new SelectResult()
                };

            var i = new Item
                {
                    Attribute = {new AmazonAttribute {Name = "IntValue", Value = "10000001000"}},
                    Name = "A"
                };
            selectResponse.SelectResult.Item.Add(i);

            simpleDb.Expect(x => x.Select(selectRequest)).Constraints(Property.AllPropertiesMatch(selectRequest)).Return
                (selectResponse);

            var command = new SelectCommand(typeof (A), commandText);
            var actualValue = (int) simol.SelectScalar(command);

            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void SelectScalar_Count()
        {
            int expectedValue = 100;
            string commandText = "select count(*) from A";
            var selectRequest = new SelectRequest
                {
                    SelectExpression = commandText
                };

            var selectResponse = new SelectResponse
                {
                    SelectResult = new SelectResult()
                };

            var i = new Item
                {
                    Attribute = {new AmazonAttribute {Name = "Count", Value = expectedValue.ToString()}},
                    Name = "Domain"
                };
            selectResponse.SelectResult.Item.Add(i);

            simpleDb.Expect(x => x.Select(selectRequest)).Constraints(Property.AllPropertiesMatch(selectRequest)).Return
                (selectResponse);

            var command = new SelectCommand(typeof (A), commandText);
            var actualValue = (int) simol.SelectScalar(command);

            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void SelectScalar_ItemName()
        {
            Guid expectedValue = Guid.NewGuid();
            string commandText = "select Id from A";
            var selectRequest = new SelectRequest
                {
                    SelectExpression = commandText
                };

            var selectResponse = new SelectResponse
                {
                    SelectResult = new SelectResult()
                };

            var i = new Item
                {
                    Name = expectedValue.ToString()
                };
            selectResponse.SelectResult.Item.Add(i);

            simpleDb.Expect(x => x.Select(selectRequest)).Constraints(Property.AllPropertiesMatch(selectRequest)).Return
                (selectResponse);

            var command = new SelectCommand(typeof (A), commandText);
            var actualValue = (Guid) simol.SelectScalar(command);

            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void Put_VersionIncrement()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (IntVersionItem));
            var i = new IntVersionItem
                {
                    Version = 1
                };
            PropertyValues values = PropertyValues.CreateValues(i);

            Func<PutAttributesRequest, PutAttributesResponse> checkRequest = delegate(PutAttributesRequest request)
                {
                    Assert.IsNotNull(request.Expected);
                    Assert.AreEqual("Version", request.Expected.Name);
                    Assert.AreEqual("10000000001", request.Expected.Value);
                    string newVersion =
                        request.Attribute.Where(a => a.Name == "Version").Select(a => a.Value).FirstOrDefault();
                    Assert.AreEqual("10000000002", newVersion);
                    return null;
                };

            simpleDb.Expect(x => x.PutAttributes(null)).IgnoreArguments().Do(checkRequest);

            simol.PutAttributes(mapping, values.ToUniList());
        }

        [Test]
        public void PutBatch_VersionIncrement()
        {
            ItemMapping mapping = ItemMapping.Create(typeof (IntVersionItem));
            var i = new IntVersionItem
                {
                    Version = 1
                };
            var allValues = new List<PropertyValues>();
            for (int k = 0; k < 3; k++)
            {
                PropertyValues values = PropertyValues.CreateValues(i);
                allValues.Add(values);
            }

            Func<BatchPutAttributesRequest, BatchPutAttributesResponse> checkRequest =
                delegate(BatchPutAttributesRequest request)
                    {
                        foreach (ReplaceableItem item in request.Item)
                        {
                            string newVersion =
                                item.Attribute.Where(a => a.Name == "Version").Select(a => a.Value).FirstOrDefault();
                            Assert.AreEqual("10000000002", newVersion);
                        }

                        return null;
                    };

            simpleDb.Expect(x => x.BatchPutAttributes(null)).IgnoreArguments().Do(checkRequest);

            simol.PutAttributes(mapping, allValues);
        }

        public class IntVersionItem : TestItemBase
        {
            [Version(VersioningBehavior.AutoIncrementAndConditionallyUpdate)]
            public int Version { get; set; }

            public int IntValue { get; set; }
        }

        public class MyDictionary : Dictionary<int, int>
        {
            public void Add(int i)
            {
            }
        }
    }
}