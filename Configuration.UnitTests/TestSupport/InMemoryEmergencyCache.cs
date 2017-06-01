using System;
using System.Collections.Generic;

namespace ConsulRx.Configuration.UnitTests
{
    public class InMemoryEmergencyCache : IEmergencyCache
    {
        public IDictionary<string,string> CachedSettings { get; set; }
        
        public void Save(IDictionary<string, string> settings)
        {
            CachedSettings = settings;
        }

        public bool TryLoad(out IDictionary<string, string> settings)
        {
            if (CachedSettings != null)
            {
                settings = CachedSettings;
                return true;
            }

            settings = null;
            return false;
        }
    }
}