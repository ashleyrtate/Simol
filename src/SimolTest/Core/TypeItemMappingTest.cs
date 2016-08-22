using System.Collections.Generic;
using Simol.TestSupport;
using NUnit.Framework;
using System.Linq;

namespace Simol.Core
{
    /// <summary>
    /// Tests DataDescripor handling of unusual mapping cases and error conditions.
    /// </summary>
    [TestFixture]
    public class TypeItemMappingTest
    {
        [Test,
         ExpectedException(typeof (SimolConfigurationException),
             ExpectedMessage =
                 "Only public, read/write properties may be used with Simol. The following non-conforming properties of type 'Simol.Core.TypeItemMappingTest+InvalidPropertyAttributesData' have been marked with SimolAttributes: 'ReadOnlyInt' 'WriteOnlyInt'"
             )]
        public void CreateMapping_AttributesOnInvalidProperties()
        {
            TypeItemMapping.GetMapping(typeof (InvalidPropertyAttributesData));
        }

        [Test]
        public void CreateMapping_CrossMappedProperties()
        {
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof (CrossMappedPropertyData));

            Assert.AreEqual(2, mapping.AttributeMappings.Count);

            Assert.AreEqual(mapping.AttributeMappings[0].AttributeName,
                            mapping.AttributeMappings[1].PropertyName);
            Assert.AreEqual(mapping.AttributeMappings[1].AttributeName,
                            mapping.AttributeMappings[0].PropertyName);
        }

        [Test]
        public void CreateMapping_SpannedProperties()
        {
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof (SpanPropertyData));

            Assert.AreEqual(mapping["NoSpanValue"].SpanAttributes, SpanType.None);
            Assert.AreEqual(mapping["SpanValue"].SpanAttributes, SpanType.Span);
            Assert.AreEqual(mapping["SpanCompressValue"].SpanAttributes, SpanType.Span | SpanType.Compress);
            Assert.AreEqual(mapping["SpanCompressEncryptValue"].SpanAttributes, SpanType.Span | SpanType.Compress | SpanType.Encrypt);
            Assert.AreEqual(mapping["SpanEncryptValue"].SpanAttributes, SpanType.Span | SpanType.Encrypt);
        }

        [Test]
        public void CreateMapping_CustomizedReferenceTypeIncluded()
        {
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof (CustomizedReferenceTypePropertyData));

            Assert.AreEqual(1, mapping.AttributeMappings.Count);
        }

        [Test,
         ExpectedException(typeof (SimolConfigurationException),
             ExpectedMessage =
                 "CustomFormatAttribute.Format cannot be used with property 'Simol.Core.TypeItemMappingTest+CustomFormatStringNonFormattableData.BooleanValue'. The property type does not implement 'System.IFormattable'."
             )]
        public void CreateMapping_CustomFormatStringNonFormattable()
        {
            TypeItemMapping.GetMapping(typeof (CustomFormatStringNonFormattableData));
        }

        [Test,
         ExpectedException(typeof (SimolConfigurationException),
             ExpectedMessage =
                 "IndexAttribute may only be used with string properties. The property 'Simol.Core.TypeItemMappingTest+NonStringIndexedPropertyData.IntValue' has a scalar type of 'System.Int32'."
             )]
        public void CreateMapping_NonStringIndexedProperty()
        {
            TypeItemMapping.GetMapping(typeof (NonStringIndexedPropertyData));
        }

        [Test,
         ExpectedException(typeof (SimolConfigurationException), 
             ExpectedMessage =
                 "SpanAttribute may not be used with list properties. The property 'Simol.Core.TypeItemMappingTest+SpanListPropertyData.StringListValue' has a type of 'System.Collections.Generic.List`1[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]'."
             )]
        public void CreateMapping_SpanListProperty()
        {
            TypeItemMapping.GetMapping(typeof (SpanListPropertyData));
        }

        [Test,
         ExpectedException(typeof (SimolConfigurationException),
             ExpectedMessage =
                 "Type 'Simol.Core.TypeItemMappingTest+DuplicateItemNameData' has multiple properties marked with an ItemNameAttribute."
             )]
        public void CreateMapping_DuplicateItemName()
        {
            TypeItemMapping.GetMapping(typeof (DuplicateItemNameData));
        }

        [Test]
        public void CreateMapping_InvalidExcludedProperty()
        {
            // create mapping for a type with an excluded, invalid property
            // without getting a SimolConfigurationException
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof (InvalidExcludedPropertyData));

            Assert.IsNull(mapping["ReadOnlyInt"]);
        }

        [Test,
         ExpectedException(typeof (SimolConfigurationException),
             ExpectedMessage =
                 "Type 'Simol.Core.TypeItemMappingTest+MissingItemNameData' has no property marked with an ItemNameAttribute."
             )]
        public void CreateMapping_MissingItemName()
        {
            TypeItemMapping.GetMapping(typeof (MissingItemNameData));
        }

        [Test,
         ExpectedException(typeof (SimolConfigurationException),
             ExpectedMessage =
                 "NumberFormatAttribute cannot be used with property 'Simol.Core.TypeItemMappingTest+NumberFormatterNonNumberData.BoolValue'. The property type is not numeric."
             )]
        public void CreateMapping_NumberFormatterNonNumber()
        {
            TypeItemMapping.GetMapping(typeof (NumberFormatterNonNumberData));
        }

        [Test,
         ExpectedException(typeof (SimolConfigurationException),
             ExpectedMessage =
                 "NumberFormatAttribute specifies too many whole and/or decimal digits for property 'Simol.Core.TypeItemMappingTest+NumberFormatterTooManyDigitsData.IntValue'. A maximum of 10 digits is supported for numeric type 'System.Int32'."
             )]
        public void CreateMapping_NumberFormatterTooManyDigits()
        {
            TypeItemMapping.GetMapping(typeof (NumberFormatterTooManyDigitsData));
        }

        [Test,
         ExpectedException(typeof (SimolConfigurationException),
             ExpectedMessage =
                 "Type 'Simol.Core.TypeItemMappingTest+PropertyMappingConflictData' has multiple properties mapped to SimpleDB attribute 'IntValue'."
             )]
        public void CreateMapping_PropertyMappingConflict()
        {
            TypeItemMapping.GetMapping(typeof (PropertyMappingConflictData));
        }

        [Test,
         ExpectedException(typeof (SimolConfigurationException),
             ExpectedMessage =
                 "VersionAttribute may only be used with DateTime or int properties. The property 'Simol.Core.TypeItemMappingTest+ListVersionData.IntListValue' has a type of 'System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]'."
             )]
        public void CreateMapping_ListVersionProperty()
        {
            TypeItemMapping.GetMapping(typeof (ListVersionData));
        }

        [Test]
        public void CreateMapping_ByteArrayProperty()
        {
            // verify that primitive array types are not automatically included in mappings
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof (ByteArrayPropertyData));

            Assert.AreEqual(0, mapping.AttributeMappings.Count);
        }

        [Test]
        public void MappingsAreCached()
        {
            TypeItemMapping mapping1 = TypeItemMapping.GetMapping(typeof (A));
            TypeItemMapping mapping2 = TypeItemMapping.GetMapping(typeof (A));

            Assert.AreSame(mapping1, mapping2);
        }

        [Test]
        public void DomainConstraintAdded()
        {
            TypeItemMapping mapping = TypeItemMapping.GetMapping(typeof (ConstrainedPropertyData));

            Assert.IsNotNull(mapping.Constraint);
            Assert.IsTrue(mapping.Constraint is TestDomainConstraint);
        }

        private class ByteArrayPropertyData : TestItemBase
        {
            public byte[] ByteArrayValue { get; set; }
        }

        [Constraint(typeof (TestDomainConstraint))]
        private class ConstrainedPropertyData : TestItemBase
        {
        }

        private class CrossMappedPropertyData : TestItemBase
        {
            [AttributeName("IntValue2")]
            public int IntValue1 { get; set; }

            [AttributeName("IntValue1")]
            public int IntValue2 { get; set; }
        }

        private class CustomFormatStringNonFormattableData : TestItemBase
        {
            [CustomFormat("abc")]
            public bool BooleanValue { get; set; }
        }

        private class DuplicateItemNameData : TestItemBase
        {
            [ItemName]
            public bool ItemNameA { get; set; }

            [ItemName]
            public bool ItemNameB { get; set; }
        }

        private class InvalidExcludedPropertyData : TestItemBase
        {
            [SimolExclude]
            public int ReadOnlyInt
            {
                get { return 0; }
            }
        }

        private class InvalidPropertyAttributesData : TestItemBase
        {
            [SimolInclude]
            public int ReadOnlyInt
            {
                get { return 0; }
            }

            [SimolInclude]
            public int WriteOnlyInt
            {
                set { }
            }
        }

        private class ListVersionData : TestItemBase
        {
            [Version]
            public List<int> IntListValue { get; set; }
        }

        private class MissingItemNameData
        {
        }

        private class NonStringIndexedPropertyData : TestItemBase
        {
            [Index]
            public int IntValue { get; set; }
        }

        private class NumberFormatterNonNumberData : TestItemBase
        {
            [NumberFormat(1, 1, false)]
            public bool BoolValue { get; set; }
        }

        private class NumberFormatterTooManyDigitsData : TestItemBase
        {
            [NumberFormat(100, 1, false)]
            public int IntValue { get; set; }
        }

        private class PropertyMappingConflictData : TestItemBase
        {
            [AttributeName("IntValue")]
            public int IntValue1 { get; set; }

            [AttributeName("IntValue")]
            public int IntValue2 { get; set; }
        }

        private class SpanListPropertyData : TestItemBase
        {
            [Span(false)]
            public List<string> StringListValue { get; set; }
        }

        private class SpanPropertyData : TestItemBase
        {
            public string NoSpanValue { get; set; }
            
            [Span]
            public string SpanValue { get; set; }

            [Span(true)]
            public string SpanCompressValue { get; set; }

            [Span(true, true)]
            public string SpanCompressEncryptValue { get; set; }

            [Span(false, true)]
            public string SpanEncryptValue { get; set; }
        }

        private class CustomizedReferenceTypePropertyData : TestItemBase
        {
            [Span(false)]
            public object ObjectValue { get; set; }
        }
    }
}