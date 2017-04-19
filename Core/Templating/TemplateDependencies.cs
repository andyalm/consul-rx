using System.Collections.Generic;

namespace ConsulTemplate.Templating
{
    public class TemplateDependencies
    {
        public HashSet<string> Keys { get; } = new HashSet<string>();
        public HashSet<string> KeyPrefixes { get; } = new HashSet<string>();
        public HashSet<string> Services { get; } = new HashSet<string>();
    }
}