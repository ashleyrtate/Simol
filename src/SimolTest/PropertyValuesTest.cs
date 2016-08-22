using System;
using System.Collections;
using Coditate.Common.Util;
using Simol.Core;
using Simol.TestSupport;
using NUnit.Framework;

namespace Simol
{
    [TestFixture]
    public class PropertyValuesTest
    {
        [Test]
        public void EnumeratePropertyNames()
        {
            var values = new PropertyValues("abc");
            values["0"] = 0;
            values["1"] = 1;
            values["2"] = 2;

            int count = 0;
            IEnumerator enumerator = ((IEnumerable) values).GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.AreEqual(values[count.ToString()], count);
                count++;
            }
        }

        [Test]
        public void IsTypeCompatible()
        {
            var values = new PropertyValues(Guid.Empty);
            values["IntValue"] = 1;
            values["InvalidPropertyName"] = 2;

            string expectedMessage1 =
                "Invalid property value. The property named 'InvalidPropertyName' was not found on the mapped item type 'Simol.TestSupport.A'.";
            string errorMessage;
            bool result;
            result = values.IsTypeCompatible(typeof (A), out errorMessage);

            Assert.IsFalse(result);
            Assert.AreEqual(expectedMessage1, errorMessage);

            string expectedMessage2 =
                "Invalid ItemName type. The ItemName 'abc' is expected to be a 'System.Guid' but is actually a 'System.String'.";

            values = new PropertyValues("abc");
            result = values.IsTypeCompatible(typeof (A), out errorMessage);

            Assert.IsFalse(result);
            Assert.AreEqual(expectedMessage2, errorMessage);
        }

        [Test]
        public void CreateValues()
        {
            var item = new A2();
            var mapping = (ValuesItemMapping) ItemMapping.Create(typeof (A2));

            PropertyValues values = PropertyValues.CreateValues(mapping, item);

            Assert.AreEqual(item.IntValue, values["IntValue"]);
        }

        [Test]
        public void CreateValues_Partial()
        {
            var item = new A2();
            var mapping = (ValuesItemMapping)ItemMapping.Create(typeof(A2));

            PropertyValues values = PropertyValues.CreateValues(item, "BooleanValue");

            Assert.AreEqual(item.BooleanValue, values["BooleanValue"]);
            Assert.IsFalse(values.ContainsProperty("IntValue"));
            Assert.IsFalse(values.ContainsProperty("StringValue"));
        }

        [Test]
        public void CreateItem()
        {
            var item = new A2();
            var mapping = (ValuesItemMapping) ItemMapping.Create(typeof (A2));

            PropertyValues values = PropertyValues.CreateValues(item);

            var item2 = (A2) PropertyValues.CreateItem(mapping, typeof (A2), values);
            Assert.AreEqual(item.IntValue, item2.IntValue);
        }

        [Test]
        public void Copy()
        {
            var values1 = new PropertyValues(Guid.NewGuid());
            values1.IsCompleteSet = true;
            values1["Property1"] = RandomData.AlphaNumericString(10, false);
            values1["Property2"] = RandomData.AlphaNumericString(10, false);

            var values2 = new PropertyValues(Guid.NewGuid());

            PropertyValues.Copy(values1, values2);

            Assert.AreNotEqual(values1.ItemName, values2.ItemName);
            Assert.AreEqual(values1.IsCompleteSet, values2.IsCompleteSet);
            foreach (string property in values1)
            {
                Assert.AreEqual(values1[property], values2[property]);
            }
        }

        [Test]
        public void CreateValues_IsCompleteSet()
        {
            PropertyValues values = PropertyValues.CreateValues(new CreateValuesItem());
            Assert.IsTrue(values.IsCompleteSet);

            ItemMapping mapping = ItemMapping.Create(typeof (CreateValuesItem));
            values = PropertyValues.CreateValues(mapping, new CreateValuesItem());
            Assert.IsFalse(values.IsCompleteSet);
        }

        public class CreateValuesItem : TestItemBase
        {
        }
    }
}