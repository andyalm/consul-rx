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
            var aclToken = app.Option("-a|--acl-token", "The ACL token used when reading from the KV store", CommandOptionType.SingleValue);
            app.OnExecute(() =>
            {
                if (help.HasValue())
                {
                    app.Out.WriteLine(app.GetHelpText());
                    return 0;
                }

                if (consulEndpoint.HasValue())
                    consulConfig.Endpoint = consulEndpoint.Value();
                if (aclToken.HasValue())
                    consulConfig.AclToken = aclToken.Value();

                var template = templatePath.HasValue() ? templatePath.Value() : "example.yml.razor";

                var templateProcessor = new TemplateProcessorBuilder(template)
                    .ConsulConfiguration(consulConfig)
                    .Build();

                using (templateProcessor)
                {
                    Thread.Sleep(-1);
                }

                return 0;
            });

            return app.Execute(args);
        }
    }
}
