using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace ConsulRx.Templating.CommandLine
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
                Name = "consul-rx-template"
            };
            var help = app.HelpOption("-h|--help");
            var consulEndpoint = app.Option("-e|--consul-endpoint", "The HTTP endpoint for consul", CommandOptionType.SingleValue);
            var templatePath = app.Option("-t|--template", "The path to the template and the path to the output (delimited by a colon)", CommandOptionType.SingleValue);
            var aclToken = app.Option("-a|--acl-token", "The ACL token used when reading from the KV store", CommandOptionType.SingleValue);
            var properties = app.Option("-p|--properties",
                "The template properties to pass to the templates in the format name=value",
                CommandOptionType.MultipleValue);
            app.OnExecute(async () =>
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

                var template = ParseTemplatePath(templatePath, out var outputPath);

                var templateProcessor = new TemplateProcessorBuilder(template, outputPath)
                    .ConsulConfiguration(consulConfig)
                    .TemplateProperties(ParseProperties(properties.Values))
                    .Build();

                try
                {
                    await templateProcessor.RunAsync();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    return 1;
                }
                
                return 0;
            });
            

            return app.Execute(args);
        }

        private static string ParseTemplatePath(CommandOption templateArg, out string outputPath)
        {
            outputPath = null;
            if (!templateArg.HasValue())
                return "example.yml.razor";

            var args = templateArg.Value().Split(':');
            if (args.Length > 1)
                outputPath = args[1];
            return args[0];
        }

        private static IDictionary<string,object> ParseProperties(IEnumerable<string> args)
        {
            return args.Select(a =>
                {
                    var equalsIndex = a.IndexOf('=');
                    if (equalsIndex <= 0)
                    {
                        throw new FormatException("Expected the property argument to be of the format name=value");
                    }

                    return new KeyValuePair<string, object>(a.Substring(0, equalsIndex), a.Substring(equalsIndex + 1));
                })
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        }
    }


}
