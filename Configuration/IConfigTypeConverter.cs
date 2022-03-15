using System.Collections.Generic;

namespace ConsulRx.Configuration
{
    public interface IConfigTypeConverter
    {
        /// <summary>
        /// Returns the config key(s) and value(s) that should be created for the given raw value from consul.
        /// </summary>
        /// <remarks>
        /// The config keys returned by this method should be "relative" to the parent config context. If you wish to return a single key,
        /// then you should use an empty string for the key.
        /// </remarks>
        /// <param name="rawValue"></param>
        /// <returns></returns>
        IEnumerable<KeyValuePair<string, string>> GetConfigValues(string rawValue);
    }
}