using System.Collections.Generic;

namespace ConsulRx.Configuration
{
    public interface IEmergencyCache
    {
        void Save(IDictionary<string, string> settings);
        bool TryLoad(out IDictionary<string,string> settings);
    }
}