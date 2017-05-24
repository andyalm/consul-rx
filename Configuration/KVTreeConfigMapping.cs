using System.Collections.ObjectModel;

namespace ConsulRx.Configuration
{
    public class KVTreeConfigMapping
    {
        public KVTreeConfigMapping(string configKey, string consulKeyPrefix)
        {
            ConfigKey = configKey;
            ConsulKeyPrefix = consulKeyPrefix;
        }

        public string ConfigKey { get; }
        public string ConsulKeyPrefix { get; }

        public string FullConfigKey(KeyValueNode node)
        {
            var relativeConsulKey = node.FullKey.Substring(ConsulKeyPrefix.Length + 1);
            return $"{ConfigKey}:{relativeConsulKey.Replace("/", ":")}";
        }
    }

    public class KVTreeConfigMappingCollection : KeyedCollection<string,KVTreeConfigMapping>
    {
        protected override string GetKeyForItem(KVTreeConfigMapping item)
        {
            return item.ConfigKey;
        }
    }
}