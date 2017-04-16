using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ConsulTemplate.Reactive;
using ConsulTemplate.Templating;

namespace ConsulTemplate
{
    public class TemplateProcessor : IDisposable
    {
        private readonly ITemplateRenderer _renderer;
        private readonly TemplateDependencies _templateDependencies;
        public ConsulState ConsulState { get; }
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        public string TemplatePath { get; }


        public TemplateProcessor(ITemplateRenderer renderer, IObservableConsul client, string templatePath)
        {
            _renderer = renderer;
            _templateDependencies = renderer.AnalyzeDependencies(templatePath);
            TemplatePath = templatePath;

            ConsulState = new ConsulState();

            Observe(() => client.ObserveServices(_templateDependencies.Services),
                services => ConsulState.UpdateService(services.ToService()));
            Observe(() => client.ObserveKeys(_templateDependencies.Keys),
                kv => ConsulState.UpdateKVNode(kv.ToKeyValueNode()));
            Observe(() => client.ObserveKeysRecursive(_templateDependencies.KeyPrefixes),
                kv => ConsulState.UpdateKVNodes(kv.ToKeyValueNodes()));

            _subscriptions.Add(ConsulState.Changes.Subscribe(_ => RenderTemplate()));
        }

        private void Observe<T>(Func<IObservable<T>> getObservable, Action<T> subscribe) where T : IConsulObservation
        {
            _subscriptions.Add(getObservable()
                .Subscribe(item => HandleConsulObservable(item, subscribe)));
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
            if (ConsulState.SatisfiesAll(_templateDependencies))
            {
                try
                {
                    Console.WriteLine($"Rendering template");
                    _renderer.Render(TemplatePath, Console.Out, ConsulState);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }
        }

        private void HandleConsulObservable<T>(T observation, Action<T> action) where T : IConsulObservation
        {
            if (observation.Result.StatusCode == HttpStatusCode.OK ||
                observation.Result.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    action(observation);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }
            else
            {
                Console.WriteLine($"Error retrieving something: {observation.Result.StatusCode}");
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