using System;
using System.Collections.Generic;
using ConsulRazor.Templating;
using ReactiveConsul;

namespace ConsulRazor
{
    public class TemplateProcessorBuilder
    {
        private readonly string _templatePath;
        private readonly string _outputPath;
        private ObservableConsulConfiguration _consulConfig = new ObservableConsulConfiguration();
        private readonly RazorTemplateRenderer _renderer;
        private IDictionary<string, object> _properties;


        public TemplateProcessorBuilder(string templatePath, string outputPath = null)
        {
            _templatePath = templatePath;
            _outputPath = outputPath;
            _renderer = new RazorTemplateRenderer(new[] {_templatePath}, new RazorTemplateCompiler());
        }

        public TemplateProcessorBuilder ConsulConfiguration(ObservableConsulConfiguration config)
        {
            _consulConfig = config;
            return this;
        }

        public TemplateProcessorBuilder TemplateBaseClass<T>(Action<T> configure) where  T : ConsulTemplateBase
        {
            _renderer.UseBaseClass(configure);
            return this;
        }

        public TemplateProcessorBuilder TemplateBaseClass<T>() where  T : ConsulTemplateBase
        {
            _renderer.UseBaseClass<T>(_ => {});
            return this;
        }

        public TemplateProcessorBuilder TemplateProperties(IDictionary<string, object> properties)
        {
            _properties = properties;
            return this;
        }

        public IDisposable Build()
        {
            var consulClient = new ObservableConsul(_consulConfig);

            return new TemplateProcessor(_renderer, consulClient, _templatePath, _outputPath, new PropertyBag(_properties));
        }
    }
}