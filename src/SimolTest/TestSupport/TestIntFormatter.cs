using System;

namespace Simol.TestSupport
{
    public class TestIntFormatter : ITypeFormatter
    {
        public string ToString(object value)
        {
            return Convert.ToString(value);
        }

        public object ToType(string value, Type expected)
        {
            return Convert.ChangeType(value, expected);
        }
    }
}