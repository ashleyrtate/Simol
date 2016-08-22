using System;
using Simol.TestSupport;
using Coditate.Common.Util;

namespace Simol.System
{
    [DomainName("FullTextTest")]
    public class FullTextTestItem : TestItemBase
    {
        [Index, Span]
        public string LongStringValue1 { get; set; }
        [Span(true, true)]
        public string LongStringValue2 { get; set; }
        [Version(VersioningBehavior.AutoIncrement)]
        public DateTime VersionValue { get; set; }

        public static FullTextTestItem Create()
        {
            var item = new FullTextTestItem
            {
                LongStringValue1 = RandomData.AlphaNumericString(2000, true),
                LongStringValue2 = RandomData.AlphaNumericString(2000, true)
            };
            return item;
        }
    }
}