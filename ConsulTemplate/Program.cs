using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using Consul;
using ConsulTemplate.Reactive;
using ConsulTemplate.Templating;
using Microsoft.Extensions.Configuration;

namespace ConsulTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("foo", "bar")}) //avoid complaining about no configuration source
                .AddYamlFile("development.yml", optional: true)
                .Build();

            var client = new ConsulClient(c =>
            {
                c.Address = new Uri(config["endpoint"] ?? "http://localhost:8500");
                c.Datacenter = config["datacenter"];
                c.Token = config["gossipToken"];
            }).Observable(config.GetValue<TimeSpan?>("longPollMaxWait"));

            var templatePath = "example.yml.razor";
            var renderer = new RazorTemplateRenderer(new [] {templatePath});
            var templateAnalysis = renderer.Analyse(templatePath);

            var model = new ConsulState();

            client.ObserveServices(templateAnalysis.RequiredServices)
                .Catch<CatalogService[],ConsulApiException>(ex => client.ObserveServices(templateAnalysis.RequiredServices))
                .Subscribe(services => WithErrorLogging(() => model.UpdateService(services.ToService())));
            client.ObserveKeys(templateAnalysis.RequiredKeys)
                .Catch<KVPair,ConsulApiException>(ex => client.ObserveKeys(templateAnalysis.RequiredKeys))
                .Subscribe(kv => WithErrorLogging(() => model.UpdateKVNode(kv.ToKeyValueNode())));
            client.ObserveKeysRecursive(templateAnalysis.RequiredKeyPrefixes)
                .Catch<KVPair[],ConsulApiException>(ex => client.ObserveKeysRecursive(templateAnalysis.RequiredKeyPrefixes))
                .Subscribe(kv => WithErrorLogging(() => model.UpdateKVNodes(kv.ToKeyValueNodes())));

            model.Changes.Subscribe(m => RenderTemplate(renderer, templatePath, m));
            
            Thread.Sleep(-1);
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

        private static void RenderTemplate(ITemplateRenderer razorTemplateRenderer, string templatePath, ConsulState model)
        {
            if (model.Services.Any() || model.KVStore.Any())
            {
                try
                {
                    Console.WriteLine($"Rendering template");
                    razorTemplateRenderer.Render(templatePath, Console.Out, model);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }
        }
    }
}
