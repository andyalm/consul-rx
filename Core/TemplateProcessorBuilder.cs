using System;
using System.Collections.Generic;
using ConsulRazor.Reactive;
using ConsulRazor.Templating;

namespace ConsulRazor
{
    public class TemplateProcessorBuilder
    {
        private readonly string _templatePath;
        private ObservableConsulConfiguration _consulConfig = new ObservableConsulConfiguration();
        private RazorTemplateRenderer _renderer;
        private IDictionary<string, object> _properties;


        public TemplateProcessorBuilder(string templatePath)
        {
            _templatePath = templatePath;
            _renderer = new RazorTemplateRenderer(new[] {_templatePath});
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

        public TemplateProcessorBuilder TemplateProperties(IDictionary<string, object> properties)
        {
            _properties = properties;
            return this;
        }

        public IDisposable Build()
        {
            var consulClient = new ObservableConsul(_consulConfig);

            return new TemplateProcessor(_renderer, consulClient, _templatePath, new PropertyBag(_properties));
        }
    }
}