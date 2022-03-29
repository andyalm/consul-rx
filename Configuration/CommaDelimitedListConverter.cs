using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsulRx.Configuration
{
    public class CommaDelimitedListConverter : IConfigTypeConverter
    {
        public IEnumerable<KeyValuePair<string, string>> GetConfigValues(string rawValue)
        {
            return rawValue.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries)
                .Select((value, index) => new KeyValuePair<string, string>(index.ToString(), value.Trim()));
        }
    }
}