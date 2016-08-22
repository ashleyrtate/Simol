using Simol.Consistency;
using System.Collections.Generic;

namespace Simol.TestSupport
{
    public class TestDomainConstraint : DomainConstraintBase
    {
        public int AfterLoadCount { get; set; }

        public int BeforeSaveCount { get; set; }

        public int BeforeDeleteCount { get; set; }

        public override void AfterLoad(PropertyValues values)
        {
            AfterLoadCount++;
        }

        public override void BeforeSave(PropertyValues values)
        {
            BeforeSaveCount++;
        }

        public override void BeforeDelete(object itemName, List<string> propertyNames)
        {
            BeforeDeleteCount++;
        }

        public void Reset()
        {
            AfterLoadCount = 0;
            BeforeSaveCount = 0;
            BeforeDeleteCount = 0;
        }
    }
}