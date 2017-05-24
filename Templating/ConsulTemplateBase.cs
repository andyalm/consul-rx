using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsulRx.Templating
{
    public abstract class ConsulTemplateBase
    {
        private ConsulState Model { get; set; }
        private TextWriter Writer { get; set; }

        private ConsulDependencies Dependencies { get; set; }

        private PropertyBag Properties { get; set; }

        public abstract Task ExecuteAsync();

        public string TemplatePath { get; internal set; }

        private bool AnalysisMode { get; set; }

        private ITemplateRenderer Renderer { get; set; }

        public ConsulDependencies AnalyzeDependencies(PropertyBag properties, ITemplateRenderer renderer)
        {
            AnalysisMode = true;
            try
            {
                Dependencies = new ConsulDependencies();
                Model = new ConsulState();
                Properties = properties ?? new PropertyBag();
                Renderer = renderer;
                ExecuteAsync().GetAwaiter().GetResult();

                return Dependencies;
            }
            finally
            {
                AnalysisMode = false;
            }
        }

        public void Render(TextWriter writer, ConsulState model, ITemplateRenderer renderer, PropertyBag properties)
        {
            Writer = writer;
            Model = model;
            Renderer = renderer;
            Properties = properties ?? new PropertyBag();
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
                Dependencies.Services.Add(serviceName);
                return Enumerable.Empty<ServiceNode>();
            }
            
            return Model.Services.Get(serviceName)?.Nodes ?? Enumerable.Empty<ServiceNode>();
        }

        public string Value(string key)
        {
            if (AnalysisMode)
            {
                Dependencies.Keys.Add(key);
                return string.Empty;
            }
            
            return Model.KVStore.GetValue(key);
        }

        public IEnumerable<KeyValueNode> Children(string keyPrefix)
        {
            if (AnalysisMode)
            {
                Dependencies.KeyPrefixes.Add(keyPrefix);
                return Enumerable.Empty<KeyValueNode>();
            }

            return Model.KVStore.GetChildren(keyPrefix);
        }

        public IEnumerable<KeyValueNode> Tree(string keyPrefix)
        {
            if (AnalysisMode)
            {
                Dependencies.KeyPrefixes.Add(keyPrefix);
                return Enumerable.Empty<KeyValueNode>();
            }

            return Model.KVStore.GetTree(keyPrefix);
        }

        public string Partial(string name, object args = null)
        {
            if (AnalysisMode)
            {
                Renderer.AnalyzePartialDependencies(name, TemplatePath, new PropertyBag(args)).CopyTo(Dependencies);
                return string.Empty;
            }

            Renderer.RenderPartial(name, TemplatePath, Writer, Model, new PropertyBag(args));
            return string.Empty;
        }

        public T Property<T>(string name)
        {
            return Properties.Value<T>(name);
        }

        public string Property(string name)
        {
            return Property<object>(name)?.ToString();
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(TemplatePath))
            {
                return TemplatePath;
            }

            return GetType().FullName;
        }
    }
}