using System;
using Simol.TestSupport;
using Coditate.Common.Util;

namespace Simol.System
{
    [DomainName("SystemTest")]
    public class SystemTestItem : TestItemBase
    {
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
        public DateTime DateValue { get; set; }
        public string StringValue { get; set; }
        public long? NullableLongValue { get; set; }
        [Version(VersioningBehavior.None)]
        public DateTime VersionValue { get; set; }

        public static SystemTestItem Create()
        {
            var item = new SystemTestItem
            {
                DateValue = SystemTestUtils.RandomDate(),
                DoubleValue = SystemTestUtils.RandomDouble(),
                IntValue = SystemTestUtils.RandomInt(),
                StringValue = RandomData.AlphaNumericString(100, true),
                VersionValue = DateUtils.Round(DateTime.Now, DateRounding.Second)
            };
            return item;
        }
    }
}