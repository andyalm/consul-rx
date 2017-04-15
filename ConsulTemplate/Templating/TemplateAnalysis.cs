using System.Collections.Generic;

namespace ConsulTemplate.Templating
{
    public class TemplateAnalysis
    {
        public HashSet<string> RequiredKeys { get; } = new HashSet<string>();
        public HashSet<string> RequiredKeyPrefixes { get; } = new HashSet<string>();
        public HashSet<string> OptionalKeys { get; } = new HashSet<string>();
        public HashSet<string> RequiredServices { get; } = new HashSet<string>();
    }
}