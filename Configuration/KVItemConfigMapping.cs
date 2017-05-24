using System.Collections.ObjectModel;

namespace ConsulRx.Configuration
{
    public class KVItemConfigMapping
    {
        public KVItemConfigMapping(string configKey, string consulKey)
        {
            ConfigKey = configKey;
            ConsulKey = consulKey;
        }

        public string ConfigKey { get; }
        public string ConsulKey { get; }
    }
    
    public class KVItemConfigMappingCollection : KeyedCollection<string,KVItemConfigMapping>
    {
        protected override string GetKeyForItem(KVItemConfigMapping item)
        {
            return item.ConfigKey;
        }
    }
}