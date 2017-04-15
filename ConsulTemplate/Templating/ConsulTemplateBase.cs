using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsulTemplate.Reactive;

namespace ConsulTemplate.Templating
{
    public abstract class ConsulTemplateBase
    {
        private ConsulState Model { get; set; }
        private TextWriter Writer { get; set; }

        private TemplateAnalysis Analysis { get; set; }

        public abstract Task ExecuteAsync();

        private bool AnalysisMode { get; set; }

        public TemplateAnalysis Analyse()
        {
            AnalysisMode = true;
            try
            {
                Analysis = new TemplateAnalysis();
                Model = new ConsulState();
                ExecuteAsync().GetAwaiter().GetResult();

                return Analysis;
            }
            finally
            {
                AnalysisMode = false;
            }
        }

        public void Render(TextWriter writer, ConsulState model)
        {
            Writer = writer;
            Model = model;
            ExecuteAsync().GetAwaiter().GetResult();
        }

        public void Write(object value)
        {
            if(!AnalysisMode)
            {
                WriteLiteral(value);
            }
        }

        public void WriteLiteral(object value)
        {
            if (!AnalysisMode)
            {
                Writer.Write(value);
            }
        }

        public IEnumerable<ServiceNode> ServiceNodes(string serviceName)
        {
            if (AnalysisMode)
            {
                Analysis.RequiredServices.Add(serviceName);
                return Enumerable.Empty<ServiceNode>();
            }
            
            return Model.Services.Get(serviceName)?.Nodes ?? Enumerable.Empty<ServiceNode>();
        }

        public string Value(string key)
        {
            if (AnalysisMode)
            {
                Analysis.RequiredKeys.Add(key);
                return string.Empty;
            }
            
            return Model.KVStore.GetValue(key);
        }

        public IEnumerable<KeyValueNode> Children(string keyPrefix)
        {
            if (AnalysisMode)
            {
                Analysis.RequiredKeyPrefixes.Add(keyPrefix);
                return Enumerable.Empty<KeyValueNode>();
            }

            return Model.KVStore.GetChildren(keyPrefix);
        }

        public IEnumerable<KeyValueNode> Tree(string keyPrefix)
        {
            if (AnalysisMode)
            {
                Analysis.RequiredKeyPrefixes.Add(keyPrefix);
                return Enumerable.Empty<KeyValueNode>();
            }

            return Model.KVStore.GetTree(keyPrefix);
        }
    }
}