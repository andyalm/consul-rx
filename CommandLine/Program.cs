using System;
using System.Collections.Generic;
using System.Threading;
using ConsulTemplate.Reactive;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace ConsulTemplate
{
    class Program
    {
        static int Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddYamlFile("development.yml", optional: true)
                .Build();

            var consulConfig = config.GetSection("consul")?.Get<ObservableConsulConfiguration>() ?? new ObservableConsulConfiguration();

            var app = new CommandLineApplication
            {
                Name = "dotnet-consul-template"
            };
            var help = app.HelpOption("-h|--help");
            var consulEndpoint = app.Option("-e|--consul-endpoint", "The HTTP endpoint for consul", CommandOptionType.SingleValue);
            var templatePath = app.Option("-t|--template", "The path to the template", CommandOptionType.SingleValue);
            app.OnExecute(() =>
            {
                if (help.HasValue())
                {
                    app.Out.WriteLine(app.GetHelpText());
                    return 0;
                }

                if (consulEndpoint.HasValue())
                {
                    consulConfig.Endpoint = consulEndpoint.Value();
                }

                var template = templatePath.HasValue() ? templatePath.Value() : "example.yml.razor";

                var client = new ObservableConsul(consulConfig);

                using (TemplateProcessor.ForRazorTemplate(template, client))
                {
                    Thread.Sleep(-1);
                }

                return 0;
            });

            return app.Execute(args);
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
