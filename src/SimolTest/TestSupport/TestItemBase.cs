using System;

namespace Simol.TestSupport
{
    public abstract class TestItemBase
    {
        private Guid itemName;

        [ItemName]
        public Guid ItemName
        {
            get
            {
                if (itemName == Guid.Empty)
                {
                    itemName = Guid.NewGuid();
                }
                return itemName;
            }
            set { itemName = value; }
        }
    }
}