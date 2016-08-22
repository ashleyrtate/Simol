using System;

namespace Simol.TestSupport
{
    /// <summary>
    /// Test object for testing full-text indexing.
    /// </summary>
    public class H : TestItemBase
    {
        [Version(VersioningBehavior.AutoIncrement)]
        public DateTime ModifiedAt { get; set; }

        [Index]
        public string TextValue { get; set; }

        public int IntValue { get; set; }

        public bool BooleanValue { get; set; }
    }
}