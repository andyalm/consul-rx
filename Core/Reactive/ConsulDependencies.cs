using System.Collections.Generic;

namespace ConsulRazor.Reactive
{
    public class ConsulDependencies
    {
        public HashSet<string> Keys { get; } = new HashSet<string>();
        public HashSet<string> KeyPrefixes { get; } = new HashSet<string>();
        public HashSet<string> Services { get; } = new HashSet<string>();

        public void CopyTo(ConsulDependencies other)
        {
            other.Keys.UnionWith(Keys);
            other.KeyPrefixes.UnionWith(KeyPrefixes);
            other.Services.UnionWith(Services);
        }
    }
}