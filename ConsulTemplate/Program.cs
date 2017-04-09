using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
                c.Token = config["gossip-token"];
            }).Observable(new ObservableConsulConfiguration { WaitTime = TimeSpan.FromSeconds(10)});

            var model = new TemplateModel();

            client.ObserveServices(args.Where(a => a.Contains("-")))
                .Subscribe(services => WithErrorLogging(() => model.UpdateService(services.ToService())));
            client.ObserveKeys(args.Where(a => !a.Contains("-")))
                .Subscribe(kv => WithErrorLogging(() => model.UpdateKey(kv.ToKeyValuePair())));

            model.Changes.Subscribe(RenderTemplate);
            
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

        private static void RenderTemplate(TemplateModel model)
        {
            if (model.Services.Any() || model.KVPairs.Any())
            {
                try
                {
                    Console.WriteLine($"Rendering template");
                    var template = new Template("example.cshtml");
                    template.Render(Console.Out, model);
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
