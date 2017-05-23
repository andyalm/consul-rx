using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ReactiveConsul.TestSupport;

namespace ReactiveConsul.Configuration.UnitTests
{
    public static class ConfigProviderLoadingExtensions
    {
        public static IConfigurationProvider LoadConfigProvider(this FakeObservableConsul consul, ConsulConfigurationSource configSource, ConsulState consulState)
        {
            var configProvider = configSource.Build(consul);
            var loadTask = Task.Run(() => configProvider.Load());
            Task.Run(() =>
            {
                foreach (var i in Enumerable.Range(0, 3))
                {
                    Thread.Sleep(20);
                    consul.Dependencies.OnNext(consulState);
                }
            });
            loadTask.Wait();

            return configProvider;
        }
    }
}