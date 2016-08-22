using System;

namespace Simol.TestSupport
{
    /// <summary>
    /// Simpler version of "A" with fewer properties for basic tests.
    /// </summary>
    [Constraint(typeof(TestDomainConstraint))]
    public class A2
    {
        public A2()
        {
            ItemName = Guid.NewGuid();
        }
        
        [ItemName]
        public Guid? ItemName { get; set; }

        public bool BooleanValue { get; set; }
        public int IntValue { get; set; }
        [Index]
        public string StringValue { get; set; }
    }
}