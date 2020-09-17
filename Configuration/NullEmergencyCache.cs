using System.Collections.Generic;

namespace ConsulRx.Configuration
{
    public class NullEmergencyCache : IEmergencyCache
    {
        public static IEmergencyCache Instance { get; } = new NullEmergencyCache();
        
        private NullEmergencyCache() {}
        
        public void Save(IDictionary<string, string> settings)
        {
            
        }

        public bool TryLoad(out IDictionary<string, string> settings)
        {
            settings = null;
            return false;
        }
    }
}