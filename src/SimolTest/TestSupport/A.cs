using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Simol.TestSupport
{
    public enum TestEnum
    {
        First,
        Second,
        Third
    }

    /// <summary>
    /// Test object for holding basic value types with no attributes.
    /// </summary>
    [Constraint(typeof(TestDomainConstraint))]
    public class A : TestItemBase
    {
        public bool BooleanValue { get; set; }
        public byte ByteValue { get; set; }
        public sbyte SByteValue { get; set; }
        public char CharValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public float FloatValue { get; set; }
        public int IntValue { get; set; }
        public uint UIntValue { get; set; }
        public long LongValue { get; set; }
        public ulong ULongValue { get; set; }
        public object ObjectValue { get; set; } // skipped by default
        public short ShortValue { get; set; }
        public ushort UShortValue { get; set; }
        public string StringValue { get; set; }
        public TestEnum EnumValue { get; set; }
        public List<int> IntGenericList { get; set; }
        public Collection<int> IntGenericCollection { get; set; }
        public int[] IntArray { get; set; } // skipped by default

        public int? NullableIntValue { get; set; }
        public TestEnum? NullableEnumValue { get; set; }

        public List<int?> NullableIntGenericList { get; set; }

        // lower case version of string value property
        public string stringvalue { get; set; }
    }
}