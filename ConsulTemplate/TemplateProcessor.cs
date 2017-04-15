using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Consul;
using ConsulTemplate.Reactive;
using ConsulTemplate.Templating;

namespace ConsulTemplate
{
    public class TemplateProcessor : IDisposable
    {
        private readonly ITemplateRenderer _renderer;
        private readonly TemplateAnalysis _templateAnalysis;
        private readonly ConsulState _consulState;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        public string TemplatePath { get; }

        public TemplateProcessor(ITemplateRenderer renderer, IObservableConsul client, string templatePath)
        {
            _renderer = renderer;
            _templateAnalysis = renderer.Analyse(templatePath);
            TemplatePath = templatePath;

            _consulState = new ConsulState();

            _subscriptions.Add(client.ObserveServices(_templateAnalysis.RequiredServices)
                .Catch<CatalogService[], ConsulApiException>(ex => client.ObserveServices(_templateAnalysis.RequiredServices))
                .Subscribe(services => WithErrorLogging(() => _consulState.UpdateService(services.ToService()))));

            _subscriptions.Add(client.ObserveKeys(_templateAnalysis.RequiredKeys)
                .Catch<KVPair, ConsulApiException>(ex => client.ObserveKeys(_templateAnalysis.RequiredKeys))
                .Subscribe(kv => WithErrorLogging(() => _consulState.UpdateKVNode(kv.ToKeyValueNode()))));

            _subscriptions.Add(client.ObserveKeysRecursive(_templateAnalysis.RequiredKeyPrefixes)
                .Catch<KVPair[], ConsulApiException>(ex => client.ObserveKeysRecursive(_templateAnalysis.RequiredKeyPrefixes))
                .Subscribe(kv => WithErrorLogging(() => _consulState.UpdateKVNodes(kv.ToKeyValueNodes()))));     

            _subscriptions.Add(_consulState.Changes.Subscribe(_ => RenderTemplate()));
        }

        public static TemplateProcessor ForRazorTemplate(string templatePath, IObservableConsul client)
        {
            var renderer = new RazorTemplateRenderer(new [] {templatePath});

            return new TemplateProcessor(renderer, client, templatePath);
        }

        public static IEnumerable<TemplateProcessor> ForRazorTemplates(IEnumerable<string> templatePaths, IObservableConsul client)
        {
            var renderer = new RazorTemplateRenderer(templatePaths);

            return templatePaths.Select(p => new TemplateProcessor(renderer, client, p)).ToArray();
        }

        private void RenderTemplate()
        {
            if (_consulState.Services.Any() || _consulState.KVStore.Any())
            {
                try
                {
                    Console.WriteLine($"Rendering template");
                    _renderer.Render(TemplatePath, Console.Out, _consulState);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }
        }

        private static void WithErrorLogging(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}