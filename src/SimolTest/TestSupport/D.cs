namespace Simol.TestSupport
{
    /// <summary>
    /// Test object for testing include attributes.
    /// </summary>
    public class D : TestItemBase
    {
        public bool BooleanValue { get; set; }

        [SimolInclude]
        public int IntValue { get; set; }

        public double DoubleValue { get; set; }
    }
}