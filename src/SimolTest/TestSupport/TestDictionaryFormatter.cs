using System;
using System.Collections.Generic;

namespace Simol.TestSupport
{
    /// <summary>
    /// Test formatter for a Dictionary<string, string>.
    /// </summary>
    public class TestDictionaryFormatter : ITypeFormatter
    {
        public string ToString(object value)
        {
            var pair = (KeyValuePair<string, string>) value;
            return pair.Key + "|" + pair.Value;
        }

        public object ToType(string valueString, Type expected)
        {
            string[] parts = valueString.Split('|');
            var pair = new KeyValuePair<string, string>(parts[0], parts[1]);
            return pair;
        }
    }
}