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
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("foo", "bar")}) //avoid complaining about no configuration source
                .AddYamlFile("development.yml", optional: true)
                .Build();

            var client = new ObservableConsul(config.GetSection("consul").Get<ObservableConsulConfiguration>());

            using (TemplateProcessor.ForRazorTemplate("example.yml.razor", client))
            {
                Thread.Sleep(-1);
            }
        }     
    }
}
