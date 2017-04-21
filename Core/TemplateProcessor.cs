using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ConsulRazor.Reactive;
using ConsulRazor.Templating;

namespace ConsulRazor
{
    public class TemplateProcessor : IDisposable
    {
        private readonly ITemplateRenderer _renderer;
        private readonly TemplateDependencies _templateDependencies;
        public ConsulState ConsulState { get; }
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly PropertyBag _properties;
        public string TemplatePath { get; }


        public TemplateProcessor(ITemplateRenderer renderer, IObservableConsul client, string templatePath, PropertyBag properties = null)
        {
            _renderer = renderer;
            _templateDependencies = renderer.AnalyzeDependencies(templatePath);
            TemplatePath = templatePath;
            _properties = properties;

            ConsulState = new ConsulState();

            Observe(() => client.ObserveServices(_templateDependencies.Services),
                services => ConsulState.UpdateService(services.ToService()));
            Observe(() => client.ObserveKeys(_templateDependencies.Keys),
                kv => ConsulState.UpdateKVNode(kv.ToKeyValueNode()));
            Observe(() => client.ObserveKeysRecursive(_templateDependencies.KeyPrefixes),
                kv =>
                {
                    if (kv.Result.Response == null || !kv.Result.Response.Any())
                        ConsulState.MarkKeyPrefixAsMissingOrEmpty(kv.KeyPrefix);
                    else
                        ConsulState.UpdateKVNodes(kv.ToKeyValueNodes());
                });

            _subscriptions.Add(ConsulState.Changes.Subscribe(_ => RenderTemplate()));
        }

        private void Observe<T>(Func<IObservable<T>> getObservable, Action<T> subscribe) where T : IConsulObservation
        {
            _subscriptions.Add(getObservable()
                .Subscribe(item => HandleConsulObservable(item, subscribe)));
        }

        private void RenderTemplate()
        {
            if (ConsulState.SatisfiesAll(_templateDependencies))
            {
                try
                {
                    Console.WriteLine($"Rendering template");
                    _renderer.Render(TemplatePath, Console.Out, ConsulState, _properties);
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