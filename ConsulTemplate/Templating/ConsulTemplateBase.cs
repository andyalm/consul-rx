using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsulTemplate.Reactive;

namespace ConsulTemplate.Templating
{
    public abstract class ConsulTemplateBase
    {
        private TemplateModel Model { get; set; }
        private TextWriter Writer { get; set; }

        private TemplateAnalysis Analysis { get; } = new TemplateAnalysis();

        public abstract Task ExecuteAsync();

        private bool AnalysisMode { get; set; } = false;

        public TemplateAnalysis Analyse()
        {
            AnalysisMode = true;
            try
            {
                Model = new TemplateModel();
                ExecuteAsync().GetAwaiter().GetResult();

                return Analysis;
            }
            finally
            {
                AnalysisMode = false;
            }
        }

        public void Render(TextWriter writer, TemplateModel model)
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

        public string Key(string key)
        {
            if (AnalysisMode)
            {
                Analysis.RequiredKeys.Add(key);
                return string.Empty;
            }
            
            return Model.KVPairs.Get(key).Value;
        }
    }

    public class TemplateAnalysis
    {
        public HashSet<string> RequiredKeys { get; } = new HashSet<string>();
        public HashSet<string> OptionalKeys { get; } = new HashSet<string>();
        public HashSet<string> RequiredServices { get; } = new HashSet<string>();
    }
}