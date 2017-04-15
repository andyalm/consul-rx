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

            Observe(client.ObserveServices(_templateAnalysis.RequiredServices),
                services => _consulState.UpdateService(services.ToService()));
            Observe(client.ObserveKeys(_templateAnalysis.RequiredKeys.Concat(_templateAnalysis.OptionalKeys)),
                kv => _consulState.UpdateKVNode(kv.ToKeyValueNode()));
            Observe(client.ObserveKeysRecursive(_templateAnalysis.KeyPrefixes),
                kv => _consulState.UpdateKVNodes(kv.ToKeyValueNodes()));

            _subscriptions.Add(_consulState.Changes.Subscribe(_ => RenderTemplate()));
        }

        private void Observe<T>(IObservable<T> observable, Action<T> subscribe)
        {
            _subscriptions.Add(
                observable
                .Catch<T,ConsulApiException>(ex => observable)
                .Subscribe(item => WithErrorLogging(() => subscribe(item))));
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