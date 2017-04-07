using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using Consul;
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

            client.ObserveServices(args)
                .Subscribe(services =>
                {
                    foreach (var s in services)
                    {
                        Console.WriteLine($"{s.ServiceName} - {s.Address}:{s.ServicePort}");
                    }
                });
            //client.ObserveKeys(args).Subscribe(pair => Console.WriteLine($"{pair.Key} = {Encoding.UTF8.GetString(pair.Value)}"));
            Thread.Sleep(-1);
        }
    }
}
