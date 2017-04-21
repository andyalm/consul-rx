using System.Collections.Generic;

namespace ConsulRazor.Templating
{
    public class TemplateDependencies
    {
        public HashSet<string> Keys { get; } = new HashSet<string>();
        public HashSet<string> KeyPrefixes { get; } = new HashSet<string>();
        public HashSet<string> Services { get; } = new HashSet<string>();

        public void CopyTo(TemplateDependencies other)
        {
            other.Keys.UnionWith(Keys);
            other.KeyPrefixes.UnionWith(KeyPrefixes);
            other.Services.UnionWith(Services);
        }
    }
}