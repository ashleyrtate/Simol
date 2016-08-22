using System.Collections.Generic;
using Simol.TestSupport;
using Coditate.TestSupport;
using NUnit.Framework;

namespace Simol.Core
{
    /// <summary>
    /// Tests ValuesItemMapping handling of unusual mapping cases and error conditions.
    /// </summary>
    [TestFixture]
    public class ValuesItemMappingTest
    {
        [Test,
         ExpectedException(typeof (SimolDataException),
             ExpectedMessage =
                 "Item type 'Simol.TestSupport.A' has no property named 'InvalidPropertyName' mapped to SimpleDB."
             )]
        public void CreateInternal_CustomFormatStringNonFormattable()
        {
            var propertyNames = new List<string> {"IntValue", "InvalidPropertyName"};
            ValuesItemMapping.CreateInternal(typeof (A), propertyNames);
        }

        [Test]
        public void ValuesAttributeMapping_FullPropertyName()
        {
            List<string> propertyNames = TypeItemMapping.GetMappedProperties(typeof (A));
            ValuesItemMapping itemMapping = ValuesItemMapping.CreateInternal(typeof (A), propertyNames);

            var mapping = itemMapping["IntValue"] as ValuesAttributeMapping;

            Assert.AreEqual(mapping.FullPropertyName, "PropertyValues[IntValue]");
        }

        [Test]
        public void CeateInternal_NoProperties()
        {
            // verify that passing an empty property list returns a mapping with all properties on mapped type
            List<string> propertyNames = TypeItemMapping.GetMappedProperties(typeof (A));
            ValuesItemMapping itemMapping = ValuesItemMapping.CreateInternal(typeof (A), new List<string>());

            Assert.AreEqual(propertyNames.Count, itemMapping.AttributeMappings.Count);
        }

        [Test]
        public void CreateInternal_AllPropertiesMatch()
        {
            ItemMapping mapping1 = ItemMapping.Create(typeof (A));

            ValuesItemMapping mapping2 = ValuesItemMapping.CreateInternal(mapping1, new List<string>(), true);

            PropertyMatcher.MatchResult result = PropertyMatcher.AreEqual(mapping1, mapping2, "ItemNameMapping");
            Assert.IsTrue(result.Equal, result.Message);
            result = PropertyMatcher.AreEqual(mapping1.ItemNameMapping, mapping2.ItemNameMapping);
            Assert.IsTrue(result.Equal, result.Message);
            Assert.AreEqual(mapping1.AttributeMappings.Count, mapping2.AttributeMappings.Count);

            for (int k = 0; k < mapping1.AttributeMappings.Count; k++)
            {
                result = PropertyMatcher.AreEqual(mapping1.AttributeMappings[k], mapping2.AttributeMappings[k]);
                Assert.IsTrue(result.Equal, result.Message);
            }
        }
    }
}