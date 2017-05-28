using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Spiffy.Monitoring;

namespace ConsulRx.Templating
{
    public class TemplateProcessor
    {
        private readonly ITemplateRenderer _renderer;
        private readonly IObservableConsul _client;
        public string TemplatePath { get; }
        public string OutputPath { get; }
        public PropertyBag Properties { get; }
        public ConsulState ConsulState { get; private set; }


        public TemplateProcessor(ITemplateRenderer renderer, IObservableConsul client, string templatePath, string outputPath, PropertyBag properties = null)
        {
            _renderer = renderer;
            _client = client;
            TemplatePath = templatePath;
            OutputPath = outputPath;
            Properties = properties;
        }

        public Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            var completionSource = new TaskCompletionSource<object>();
            var consulDependencies = _renderer.AnalyzeDependencies(TemplatePath, Properties);
            var subscription = _client.ObserveDependencies(consulDependencies).Subscribe(consulState =>
            {
                ConsulState = consulState;
                RenderTemplate();
            }, onError: exception =>
            {
                completionSource.SetException(exception);
            }, onCompleted: () =>
            {
                completionSource.SetResult(null);
            });
            cancellationToken.Register(() => subscription.Dispose());

            return completionSource.Task;
        }

        private void RenderTemplate()
        {
            var eventContext = new EventContext("ConsulRx", "RenderTemplate");
            try
            {
                eventContext["TemplatePath"] = TemplatePath;
                using (var writer = OpenOutput())
                {
                    _renderer.Render(TemplatePath, writer, ConsulState, Properties);
                }
            }
            catch (Exception ex)
            {
                eventContext.IncludeException(ex);
            }
            finally
            {
                eventContext.Dispose();
            }
        }

        private TextWriter OpenOutput()
        {
            if (OutputPath == null || OutputPath.Equals("STDOUT"))
            {
                return Console.Out;
            }

            return new StreamWriter(File.Open(OutputPath, FileMode.Create));
        }
    }
}