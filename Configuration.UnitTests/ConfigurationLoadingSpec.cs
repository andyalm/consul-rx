using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using ConsulRx.TestSupport;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ConsulRx.Configuration.UnitTests
{
    public class ConfigurationLoadingSpec
    {
        private readonly InMemoryEmergencyCache _cache = new InMemoryEmergencyCache();
        private readonly ConsulConfigurationSource _configSource;

        public ConfigurationLoadingSpec()
        {
            _configSource = new ConsulConfigurationSource()
                .UseCache(_cache)
                .MapKeyPrefix("apps/myapp", "consul");
        }
        
        [Fact]
        public async Task SettingsSuccessfullyRetrievedFromConsulAreCachedInLocalCache()
        {
            var consul = new FakeObservableConsul();
            
            var configProvider = (ConsulConfigurationProvider) _configSource.Build(consul);

            consul.CurrentState = consul.CurrentState.UpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/folder1/item1", "value1"),
                new KeyValueNode("apps/myapp/folder1/item2", "value2"),
                new KeyValueNode("apps/myapp/folder2/item1", "value3")
            });

            await configProvider.LoadAsync();

            _cache.CachedSettings.Should().NotBeNull();
            _cache.CachedSettings.Should().NotBeEmpty();
            _cache.CachedSettings.Should().Contain("consul:folder1:item1", "value1");
            _cache.CachedSettings.Should().Contain("consul:folder1:item2", "value2");
            _cache.CachedSettings.Should().Contain("consul:folder2:item1", "value3");
        }

        [Fact]
        public async Task ExceptionLoadingFromConsulFallsBackToEmergencyCache()
        {
            _cache.CachedSettings = new Dictionary<string, string>
            {
                {"consul:folder1:item1", "cachedvalue"}
            };

            var consul = Substitute.For<IObservableConsul>();
            consul.GetDependenciesAsync(null)
                .ThrowsForAnyArgs(
                    new ConsulErrorException(new QueryResult {StatusCode = HttpStatusCode.InternalServerError}));
            
            var configProvider = (ConsulConfigurationProvider) _configSource.Build(consul);
            await configProvider.LoadAsync();
            
            configProvider.TryGet("consul:folder1:item1", out var value).Should().BeTrue();
            value.Should().Be("cachedvalue");
        }

        [Fact]
        public async Task ObserveDependenciesIsRetriedAfterLoadingFromEmergencyCacheIfAutoUpdateIsOn()
        {
            _cache.CachedSettings = new Dictionary<string, string>
            {
                {"consul:folder1:item1", "cachedvalue"}
            };

            var consul = Substitute.For<IObservableConsul>();
            consul.Configuration.Returns(new ObservableConsulConfiguration());
            var dependencySubject = new Subject<ConsulState>();
            consul.GetDependenciesAsync(null)
                .ThrowsForAnyArgs(
                    new ConsulErrorException(new QueryResult {StatusCode = HttpStatusCode.InternalServerError}));
            consul.ObserveDependencies(null).ReturnsForAnyArgs(dependencySubject);
            
            var configProvider = (ConsulConfigurationProvider) _configSource.AutoUpdate().Build(consul);
            await configProvider.LoadAsync();
            
            dependencySubject.OnNext(new ConsulState().UpdateKVNode(new KeyValueNode("apps/myapp/folder1/item1", "value1")));
            
            //give time for values to update
            await Task.Delay(50);
            
            configProvider.TryGet("consul:folder1:item1", out var value).Should().BeTrue();
            value.Should().Be("value1");
        }

        [Fact]
        public async Task DependenciesAreNotObservedIfAutoUpdateIsDisabled()
        {
            var consul = Substitute.For<IObservableConsul>();
            consul.GetDependenciesAsync(null)
                .ReturnsForAnyArgs(
                    new ConsulState().UpdateKVNode(new KeyValueNode("apps/myapp/folder1/item1", "value1")));
            var configProvider = (ConsulConfigurationProvider) _configSource.Build(consul);
            await configProvider.LoadAsync();

            consul.DidNotReceiveWithAnyArgs().ObserveDependencies(null);
        }
    }
}