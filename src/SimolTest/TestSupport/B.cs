using System.Collections.Generic;
using Simol.Formatters;

namespace Simol.TestSupport
{
    /// <summary>
    /// Test object for holding objects configured and formatted with custom attributes.
    /// </summary>
    [DomainName("CustomB")]
    public class B
    {
        [ItemName, NumberFormat(10, 0, false)]
        public int FormattedIntItemName { get; set; }

        [CustomFormat(typeof(ByteArrayFormatter))]
        public byte[] ByteArrayValue { get; set; }

        [CustomFormat(typeof(TestDictionaryFormatter))]
        public Dictionary<string, string> DictionaryValue { get; set; }

        [CustomFormat(typeof (TestIntFormatter))]
        public int ConvertedIntValue { get; set; }

        [CustomFormat("#")]
        public int FormattedIntValue { get; set; }

        [NumberFormat(0, 15, false)]
        public double SizedDoubleValue { get; set; }

        [NumberFormat(1, 0, true)]
        public int SizedIntValue { get; set; }

        [SimolInclude]
        public int IncludedIntValue { get; set; }

        [AttributeName("CustomAttributeNameInt"), NumberFormat(1, 0, false)]
        public int RenamedIntValue { get; set; }

        public int ExcludedIntValue { get; set; }

        [Span(false)]
        public string LongStringValue { get; set; }
    }
}