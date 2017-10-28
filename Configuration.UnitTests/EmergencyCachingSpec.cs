using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using ConsulRx.TestSupport;
using FluentAssertions;
using Xunit;

namespace ConsulRx.Configuration.UnitTests
{
    public class EmergencyCachingSpec
    {
        private readonly FakeConsulClient _consulClient = new FakeConsulClient();
        private readonly IObservableConsul _consul;
        private readonly InMemoryEmergencyCache _cache = new InMemoryEmergencyCache();
        private readonly ConsulConfigurationSource _configSource;
        private readonly KVPair[] _successfulValues = new[]
        {
            new KVPair("apps/myapp/folder1/item1") { Value = Encoding.UTF8.GetBytes("value1")}, 
            new KVPair("apps/myapp/folder1/item2") { Value = Encoding.UTF8.GetBytes("value2")}, 
            new KVPair("apps/myapp/folder2/item1") { Value = Encoding.UTF8.GetBytes("value3")}, 
        };
        private TimeSpan _retryDelay = TimeSpan.FromMilliseconds(100);

        public EmergencyCachingSpec()
        {
            _consul = new ObservableConsul(_consulClient, retryDelay:_retryDelay);
            _configSource = new ConsulConfigurationSource()
                .UseCache(_cache)
                .AutoUpdate(_retryDelay)
                .MapKeyPrefix("apps/myapp", "consul");
        }
        
        [Fact]
        public async Task SettingsSuccessfullyRetrievedFromConsulAreCachedInLocalCache()
        {
            var configProvider = (ConsulConfigurationProvider) _configSource.Build(_consul);
            await Task.WhenAll(configProvider.LoadAsync(), _consulClient.CompleteListAsync("apps/myapp",
                new QueryResult<KVPair[]>
                {
                    StatusCode = HttpStatusCode.OK,
                    Response = _successfulValues
                }));

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
            
            var configProvider = (ConsulConfigurationProvider) _configSource.Build(_consul);
            await Task.WhenAll(configProvider.LoadAsync(), _consulClient.CompleteListAsync("apps/myapp",
                new QueryResult<KVPair[]>
                {
                    StatusCode = HttpStatusCode.InternalServerError
                }));
            
            configProvider.TryGet("consul:folder1:item1", out var value).Should().BeTrue();
            value.Should().Be("cachedvalue");
        }

        [Fact]
        public async Task ObserveDependenciesIsRetriedAfterLoadingFromEmergencyCache()
        {
            _cache.CachedSettings = new Dictionary<string, string>
            {
                {"consul:folder1:item1", "cachedvalue"}
            };
            
            var configProvider = (ConsulConfigurationProvider) _configSource.Build(_consul);
            await Task.WhenAll(configProvider.LoadAsync(), _consulClient.CompleteListAsync("apps/myapp",
                new QueryResult<KVPair[]>
                {
                    StatusCode = HttpStatusCode.InternalServerError
                }));
            await Task.Delay(250);
            await _consulClient.CompleteListAsync("apps/myapp", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = _successfulValues
            });
            //give time for values to update
            await Task.Delay(50);
            
            configProvider.TryGet("consul:folder1:item1", out var value).Should().BeTrue();
            value.Should().Be("value1");
        }

        [Fact]
        public async Task DependenciesAreNotObservedIfAutoUpdateIsDisabled()
        {
            _configSource.DoNotAutoUpdate();
            
            var configProvider = (ConsulConfigurationProvider) _configSource.Build(_consul);
            await Task.WhenAll(configProvider.LoadAsync(), _consulClient.CompleteListAsync("apps/myapp",
                new QueryResult<KVPair[]>
                {
                    StatusCode = HttpStatusCode.OK,
                    Response = _successfulValues.Skip(1).ToArray()
                }));

            configProvider.TryGet("consul:folder1:item1", out var value).Should().BeFalse();
            
            await Task.Delay(_retryDelay + _retryDelay);

            await _consulClient.CompleteListAsync("apps/myapp", new QueryResult<KVPair[]>
            {
                StatusCode = HttpStatusCode.OK,
                Response = _successfulValues
            });
            
            //give time for values to update
            await Task.Delay(50);
            
            configProvider.TryGet("consul:folder1:item1", out value).Should().BeFalse();
        }
    }
}