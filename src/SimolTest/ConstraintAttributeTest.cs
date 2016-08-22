using System;
using Coditate.TestSupport;
using NUnit.Framework;
using System.Collections.Generic;
using Coditate.Common.Util;

namespace Simol
{
    [TestFixture]
    public class ConstraintAttributeTest
    {
        private class TestConstraint : IDomainConstraint
        {
            public TestConstraint(string first, int second)
            {
                First = first;
                Second = second;
            }

            public int Second { get; set; }

            public string First { get; set; }
            public void AfterLoad(PropertyValues values)
            {
                throw new NotImplementedException();
            }

            public void BeforeSave(PropertyValues values)
            {
                throw new NotImplementedException();
            }

            public void BeforeDelete(object itemName, List<string> propertyNames)
            {
                throw new NotImplementedException();
            }
        }
        [Test,
         ExpectedException(typeof (ArgumentException),
             ExpectedMessage =
                 "Unable to instantiate domain constraint 'Simol.ConstraintAttributeTest+TestConstraint' with '1' constructor argument(s). The argument values were '100'."
             )]
        public void InvalidDomainConstraint()
        {
            new ConstraintAttribute(typeof (TestConstraint), 100);
        }

        [Test]
        public void ConstraintWithConstructorArgs()
        {
            string first = RandomData.AsciiString(10);
            int second = RandomData.Int();
            ConstraintAttribute constraintAttribute = new ConstraintAttribute(typeof (TestConstraint), first, second);

            TestConstraint constraint = constraintAttribute.Constraint as TestConstraint;
            Assert.IsNotNull(constraint);
            Assert.AreEqual(first, constraint.First);
            Assert.AreEqual(second, constraint.Second);
        }
    }
}