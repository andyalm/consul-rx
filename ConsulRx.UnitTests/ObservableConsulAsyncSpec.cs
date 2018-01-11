using System.Net;
using System.Text;
using System.Threading.Tasks;
using Consul;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ConsulRx.UnitTests
{
    public class ObservableConsulAsyncSpec
    {
        private readonly ObservableConsul _observableConsul;
        private readonly IConsulClient _consulClient;

        public ObservableConsulAsyncSpec()
        {
            _consulClient = Substitute.For<IConsulClient>();
            _observableConsul = new ObservableConsul(_consulClient);
            
            _consulClient.Catalog.Service("MyService", Arg.Any<string>(), Arg.Any<QueryOptions>())
                .Returns(new QueryResult<CatalogService[]>
                {
                    StatusCode = HttpStatusCode.OK,
                    Response = new[]
                    {
                        new CatalogService
                        {
                            ServiceName = "MyService",
                            Node = "Node1",
                            ServiceAddress = "10.0.0.2"
                        }, 
                        new CatalogService
                        {
                            ServiceName = "MyService",
                            Node = "Node2",
                            ServiceAddress = "10.0.0.3"
                        } 
                    }
                });
            
            _consulClient.Catalog.Service("MyService2", Arg.Any<string>(), Arg.Any<QueryOptions>())
                .Returns(new QueryResult<CatalogService[]>
                {
                    StatusCode = HttpStatusCode.OK,
                    Response = new[]
                    {
                        new CatalogService
                        {
                            ServiceName = "MyService2",
                            Node = "Node1",
                            ServiceAddress = "10.0.0.10"
                        }
                    }
                });
            
            _consulClient.Catalog.Service("MissingService", Arg.Any<string>(), Arg.Any<QueryOptions>())
                .Returns(new QueryResult<CatalogService[]>
                {
                    StatusCode = HttpStatusCode.NotFound
                });
            
            _consulClient.KV.Get("shared/key1", Arg.Any<QueryOptions>()).Returns(new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.OK,
                Response = new KVPair("shared/key1")
                {
                    Value = Encoding.UTF8.GetBytes("value1")
                }
            });
            
            _consulClient.KV.Get("shared/missingkey", Arg.Any<QueryOptions>())
                .Returns(new QueryResult<KVPair>
                {
                    StatusCode = HttpStatusCode.NotFound
                });
            
            _consulClient.KV.List("apps/myapp", Arg.Any<QueryOptions>())
                .Returns(new QueryResult<KVPair[]>
                {
                    StatusCode = HttpStatusCode.OK,
                    Response = new[]
                    {
                        new KVPair("apps/myapp/folder1/item1")
                        {
                            Value = Encoding.UTF8.GetBytes("value1")
                        },
                        new KVPair("apps/myapp/folder1/item2")
                        {
                            Value = Encoding.UTF8.GetBytes("value2")
                        },
                        new KVPair("apps/myapp/folder2/item1")
                        {
                            Value = Encoding.UTF8.GetBytes("value3")
                        }
                    }
                });
            
            _consulClient.KV.List("apps/missingapp", Arg.Any<QueryOptions>())
                .Returns(new QueryResult<KVPair[]>
                {
                    StatusCode = HttpStatusCode.NotFound
                });
        }

        [Fact]
        public async Task GetServiceReturnsServiceWhenItExists()
        {
            var service = await _observableConsul.GetServiceAsync("MyService");
            service.Should().NotBeNull();

            service.Name.Should().Be("MyService");
            service.Nodes.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetServiceReturnsNullWhenItDoesNotExist()
        {
            var service = await _observableConsul.GetServiceAsync("MissingService");
            service.Should().BeNull();
        }

        [Fact]
        public async Task GetServiceThrowsWhenConsulReturnsServerError()
        {
            _consulClient.Catalog.Service("MyService", Arg.Any<string>(), Arg.Any<QueryOptions>())
                .Returns(new QueryResult<CatalogService[]>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

            await Assert.ThrowsAsync<ConsulErrorException>(async () =>
            {
                await _observableConsul.GetServiceAsync("MyService");
            });
        }

        [Fact]
        public async Task GetKeyReturnsValueWhenItExists()
        {
            var node = await _observableConsul.GetKeyAsync("shared/key1");
            node.Should().NotBeNull();

            node.FullKey.Should().Be("shared/key1");
            node.Value.Should().Be("value1");
        }
        
        [Fact]
        public async Task GetKeyReturnsNullWhenItDoesNotExist()
        {
            var node = await _observableConsul.GetKeyAsync("shared/missingkey");
            node.Should().BeNull();
        }
        
        [Fact]
        public async Task GetKeyThrowsWhenConsulReturnsServerError()
        {
            _consulClient.KV.Get("shared/key1", Arg.Any<QueryOptions>())
                .Returns(new QueryResult<KVPair>
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            await Assert.ThrowsAsync<ConsulErrorException>(async () =>
            {
                await _observableConsul.GetKeyAsync("shared/key1");
            });
        }

        [Fact]
        public async Task GetKeyRecursiveReturnsAllChildKeys()
        {
            var nodes = await _observableConsul.GetKeyRecursiveAsync("apps/myapp");

            nodes.Should().HaveCount(3);
            nodes.Should().Contain(n => n.FullKey == "apps/myapp/folder1/item1" && n.Value == "value1");
            nodes.Should().Contain(n => n.FullKey == "apps/myapp/folder1/item2" && n.Value == "value2");
            nodes.Should().Contain(n => n.FullKey == "apps/myapp/folder2/item1" && n.Value == "value3");
        }

        [Fact]
        public async Task GetKeyRecursiveReturnsEmptyListWhenKeyDoesNotExist()
        {
            var nodes = await _observableConsul.GetKeyRecursiveAsync("apps/missingapp");
            nodes.Should().NotBeNull();
            nodes.Should().BeEmpty();
        }
        
        [Fact]
        public async Task GetKeyRecursiveThrowsWhenConsulReturnsServerError()
        {
            _consulClient.KV.List("apps/myapp", Arg.Any<QueryOptions>())
                .Returns(new QueryResult<KVPair[]>
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            await Assert.ThrowsAsync<ConsulErrorException>(async () =>
            {
                await _observableConsul.GetKeyRecursiveAsync("apps/myapp");
            });
        }

        [Fact]
        public async Task GetDependenciesReturnsEntireStateOfAllDependencies()
        {
            var dependencies = new ConsulDependencies
            {
                KeyPrefixes = { "apps/myapp" },
                Keys = { "shared/key1", "shared/missingkey" },
                Services = { "MyService", "MyService2", "MissingService" }
            };

            var consulState = await _observableConsul.GetDependenciesAsync(dependencies);

            consulState.Services.Should().HaveCount(2);
            consulState.Services.Should().Contain(s => s.Name == "MyService");
            consulState.Services.Should().Contain(s => s.Name == "MyService2");

            consulState.KVStore.ContainsKey("shared/key1").Should().BeTrue();
            consulState.KVStore.ContainsKey("shared/missingkey").Should().BeFalse();
            var childKeys = consulState.KVStore.GetTree("apps/myapp");
            childKeys.Should().HaveCount(3);
        }
    }
}