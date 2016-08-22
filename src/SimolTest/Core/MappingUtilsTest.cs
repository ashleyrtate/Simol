using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Coditate.Common.Util;
using Simol.Formatters;
using Simol.TestSupport;
using NUnit.Framework;
using AmazonAttribute=Amazon.SimpleDB.Model.Attribute;

namespace Simol.Core
{
    [TestFixture]
    public class MappingUtilsTest
    {
        private PropertyFormatter formatter;
        private TypeItemMapping mapping;

        [SetUp]
        public void SetUp()
        {
            mapping = TypeItemMapping.GetMapping(typeof (TestItem));
            formatter = new PropertyFormatter(new SimolConfig());
        }

        [Test]
        public void AddListPropertyValue_NullValue()
        {
            TypeItemMapping itemMapping = TypeItemMapping.GetMapping(typeof (C));
            var values = new PropertyValues(Guid.NewGuid());
            values["EmptyListValue"] = new List<int>();

            MappingUtils.AddListPropertyValue(itemMapping["EmptyListValue"], values, null);

            Assert.IsEmpty((ICollection) values["EmptyListValue"]);
        }

        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "Unable to instantiate item of type 'Simol.Core.MappingUtilsTest+NoDefaultConstructorItem'. Does the type have a public no-arg constructor?"
             )]
        public void CreateInstance_Error()
        {
            MappingUtils.CreateInstance(typeof (NoDefaultConstructorItem));
        }

        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "Item name of item 'Simol.Core.MappingUtilsTest+TestItem' is null or an empty string."
             )]
        public void GetPropertyValues_NullItemName()
        {
            var g = new TestItem();
            MappingUtils.GetPropertyValues(TypeItemMapping.GetMapping(typeof (TestItem)), g);
        }

        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "Item name of type 'System.Boolean' could not be converted to a string using the formatter configured for item name property 'Simol.Core.MappingUtilsTest+TestItem.ItemName'."
             )]
        public void ItemNameToString_Error()
        {
            MappingUtils.ItemNameToString(formatter, mapping.ItemNameMapping, true);
        }

        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "Property 'Simol.Core.MappingUtilsTest+TestItem.IntValue' with value 'True' could not be converted to a string. The property type is 'System.Int32'."
             )]
        public void PropertyValueToString_Error()
        {
            AttributeMapping intValueMapping =
                mapping.AttributeMappings.Where(p => p.AttributeName == "IntValue").FirstOrDefault();
            MappingUtils.PropertyValueToString(formatter, intValueMapping, true);
        }

        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "Error adding value to list property 'Simol.Core.MappingUtilsTest+TestItem.AlwaysErrorListValue'. The property type is 'System.Collections.Generic.List`1[System.Int32]'. The value type is 'System.String'."
             )]
        public void AddListProperty_Error()
        {
            var values = new PropertyValues(Guid.NewGuid());
            values["AlwaysErrorListValue"] = new List<int>();
            AttributeMapping errorValueMapping =
                mapping.AttributeMappings.Where(p => p.AttributeName == "AlwaysErrorListValue").FirstOrDefault();
            MappingUtils.AddListPropertyValue(errorValueMapping, values, "");
        }

        [Test]
        public void SetPropertyValues_ExtraValuesIgnored()
        {
            var values = new PropertyValues(0L);
            values["InvalidProperty"] = "My test property value";
            values["IntValue"] = RandomData.Generator.Next();

            var g = new TestItem();
            MappingUtils.SetPropertyValues(TypeItemMapping.GetMapping(typeof (TestItem)), values, g);

            Assert.AreEqual(g.IntValue, values["IntValue"]);
        }

        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "String value 'True' could not be converted to expected property type 'System.Int32' for property 'Simol.Core.MappingUtilsTest+TestItem.IntValue'."
             )]
        public void StringToPropertyValue_Error()
        {
            AttributeMapping intValueMapping =
                mapping.AttributeMappings.Where(p => p.AttributeName == "IntValue").FirstOrDefault();
            MappingUtils.StringToPropertyValue(formatter, intValueMapping, "True");
        }

        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "Item type 'System.Object' has no public, read/write property named 'abc'"
             )]
        public void GetPropertyValue_InvalidName()
        {
            AttributeMapping mapping = AttributeMapping.Create("abc", typeof (int));
            MappingUtils.GetPropertyValue(new object(), mapping);
        }

        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "Error reading property 'Simol.Core.MappingUtilsTest+AlwaysErrorPropertyItem.IntValue'. The property type is 'System.Int32'."
             )]
        public void GetPropertyValue_InvalidValue()
        {
            AttributeMapping mapping = new TypeAttributeMapping(typeof (AlwaysErrorPropertyItem).GetProperty("IntValue"));
            MappingUtils.GetPropertyValue(new AlwaysErrorPropertyItem(), mapping);
        }

        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "Error setting property 'Simol.Core.MappingUtilsTest+AlwaysErrorPropertyItem.IntValue'. The declared property type is 'System.Int32'. The actual value type is 'System.String'."
             )]
        public void SetPropertyValue_InvalidValue()
        {
            AttributeMapping mapping = new TypeAttributeMapping(typeof (AlwaysErrorPropertyItem).GetProperty("IntValue"));
            MappingUtils.SetPropertyValue(new AlwaysErrorPropertyItem(), mapping, "");
        }

        public class AlwaysErrorPropertyItem : TestItemBase
        {
            public int IntValue
            {
                get { throw new Exception(); }
                set { throw new Exception(); }
            }
        }

        public class NoDefaultConstructorItem : TestItemBase
        {
            public NoDefaultConstructorItem(int value)
            {
            }
        }

        public class TestItem
        {
            [NumberFormat(1, 1, false)]
            [ItemName]
            public long? ItemName { get; set; }

            [NumberFormat(1, 1, false)]
            public int IntValue { get; set; }

            public int AlwaysErrorIntValue
            {
                set { throw new Exception(); }
                get { throw new Exception(); }
            }

            public List<int> AlwaysErrorListValue
            {
                set { throw new Exception(); }
                get { throw new Exception(); }
            }
        }
    }
}