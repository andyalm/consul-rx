using System.Collections.Generic;

namespace ConsulRx.Configuration
{
    public class PassthruConfigTypeConverter : IConfigTypeConverter
    {
        public IEnumerable<KeyValuePair<string, string>> GetConfigValues(string rawValue)
        {
            yield return new KeyValuePair<string, string>(string.Empty, rawValue);
        }
    }
}