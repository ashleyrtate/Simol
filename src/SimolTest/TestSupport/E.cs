namespace Simol.TestSupport
{
    /// <summary>
    /// Test object for testing exclude attributes.
    /// </summary>
    public class E : TestItemBase
    {
        public bool BooleanValue { get; set; }

        [SimolExclude]
        public int IntValue { get; set; }

        public double DoubleValue { get; set; }
    }
}