using System;
using System.Collections.Generic;
using System.Threading;
using ConsulTemplate.Reactive;
using Microsoft.Extensions.Configuration;

namespace ConsulTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddYamlFile("development.yml", optional: true)
                .AddCommandLine(args, SwitchMappings)
                .Build();

            var client = new ObservableConsul(config.GetSection("consul").Get<ObservableConsulConfiguration>());

            using (TemplateProcessor.ForRazorTemplate("example.yml.razor", client))
            {
                Thread.Sleep(-1);
            }
        }

        private static readonly IDictionary<string,string> SwitchMappings = new Dictionary<string, string>
        {
            {"-e","consul:endpoint"},
            {"--endpoint", "consul:endpoint"},
            {"-dc", "consul:datacenter"},
            {"--datacenter", "consul:datacenter"},
            {"-w", "consul:longPollMaxWait"},
            {"--max-wait", "consul:longPollMaxWait"}
        };
    }
}
