using System;
using System.Collections.Generic;
using System.Threading;
using Consul;
using ConsulTemplate.Templating;
using ConsulTemplateDotNet.Models;
using ConsulTemplateDotNet.Reactive;
using Microsoft.Extensions.Configuration;

namespace ConsulTemplateDotNet
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

            var servicesObservable = client.ObserveServices(args);
            servicesObservable.Subscribe(services =>
            {
                model.UpdateService(services);
                if (model.Services.Count > 0)
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
            });
            var kvObservable = client.ObserveKeys(args).Subscribe(kv => {
                model.UpdateKey(kv);
                if (model.KVPairs.Count > 0)
                {
                    try
                    {
                        Console.WriteLine($"Rendering template for kv change");
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
            });

            //client.ObserveKeys(args).Subscribe(pair => Console.WriteLine($"{pair.Key} = {Encoding.UTF8.GetString(pair.Value)}"));
            Thread.Sleep(-1);
        }
    }
}
