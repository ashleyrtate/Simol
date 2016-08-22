using System.Collections.Generic;

namespace Simol.TestSupport
{
    /// <summary>
    /// Test object for holding nullable value types with no attributes.
    /// </summary>
    public class C : TestItemBase
    {
        public C()
        {
            EmptyListValue = new List<int>();
            ListOfNullsValue = new List<int?> ();
        }

        public bool? BooleanValue { get; set; }
        public int? IntValue { get; set; }
        public double? DoubleValue { get; set; }
        public string StringValue { get; set; }
        public List<int> EmptyListValue { get; set; }
        public List<int?> ListOfNullsValue { get; set; }
        [Span(false)]
        public string LongStringValue { get; set; }
        [Span(false)]
        public string LongStringValue2 { get; set; }

    }
}