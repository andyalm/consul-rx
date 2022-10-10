using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace ConsulRx.UnitTests
{
    public class ConsulStateSpec
    {
        private ConsulState _consulState = new ConsulState();

        [Fact]
        public void TryUpdateServiceWithSameValuesDoesNothing()
        {
            var consulState = _consulState.UpdateService(new Service
            {
                Id = "myid",
                Name = "myservice",
                Nodes = new[]
                {
                    new ServiceNode
                    {
                        Name = "mynode1",
                        Address = "10.0.0.1",
                        Port = 80,
                        Tags = new[]
                        {
                            "tag1",
                            "tag2"
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            ["mymeta1"] = "myval1",
                            ["mymeta2"] = "myval2",
                        }
                    },
                }
            });

            consulState.TryUpdateService(new Service
            {
                Id = "myid",
                Name = "myservice",
                Nodes = new[]
                {
                    new ServiceNode
                    {
                        Name = "mynode1",
                        Address = "10.0.0.1",
                        Port = 80,
                        Tags = new[]
                        {
                            "tag1",
                            "tag2"
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            ["mymeta1"] = "myval1",
                            ["mymeta2"] = "myval2",
                        }
                    },
                }
            }, out _).Should().BeFalse();
        }

        [Fact]
        public void TryUpdateServiceWithDifferentTagsOnANodeReturnsTrue()
        {
            var consulState = _consulState.UpdateService(new Service
            {
                Id = "myid",
                Name = "myservice",
                Nodes = new[]
                {
                    new ServiceNode
                    {
                        Name = "mynode1",
                        Address = "10.0.0.1",
                        Port = 80,
                        Tags = new[]
                        {
                            "tag1",
                            "tag2"
                        }
                    },
                }
            });

            consulState.TryUpdateService(new Service
            {
                Id = "myid",
                Name = "myservice",
                Nodes = new[]
                {
                    new ServiceNode
                    {
                        Name = "mynode1",
                        Address = "10.0.0.1",
                        Port = 80,
                        Tags = new[]
                        {
                            "tag2",
                            "tag3"
                        }
                    },
                }
            }, out var updatedState).Should().BeTrue();
            updatedState.Services.First().Nodes.First().Tags.Should().HaveCount(2);
            updatedState.Services.First().Nodes.First().Tags.Should().ContainInOrder("tag2", "tag3");
        }
        
        [Fact]
        public void TryUpdateServiceWithDifferentMetadataOnANodeReturnsTrue()
        {
            var consulState = _consulState.UpdateService(new Service
            {
                Id = "myid",
                Name = "myservice",
                Nodes = new[]
                {
                    new ServiceNode
                    {
                        Name = "mynode1",
                        Address = "10.0.0.1",
                        Port = 80,
                        Metadata = new Dictionary<string, string>
                        {
                            ["mymeta1"] = "myval1"
                        }
                    },
                }
            });

            consulState.TryUpdateService(new Service
            {
                Id = "myid",
                Name = "myservice",
                Nodes = new[]
                {
                    new ServiceNode
                    {
                        Name = "mynode1",
                        Address = "10.0.0.1",
                        Port = 80,
                        Metadata = new Dictionary<string, string>
                        {
                            ["mymeta1"] = "myval2"
                        }
                    },
                }
            }, out var updatedState).Should().BeTrue();
            updatedState.Services.First().Nodes.First().Metadata.Should().HaveCount(1);
            updatedState.Services.First().Nodes.First().Metadata.Should().ContainKey("mymeta1");
            updatedState.Services.First().Nodes.First().Metadata.Should().ContainValue("myval2");
        }

        [Fact]
        public void TryUpdateKeyWithSameValueDoesNotUpdate()
        {
            var consulState = _consulState.UpdateKVNode(new KeyValueNode("apps/setting1", "val1"));
            consulState.TryUpdateKVNode(new KeyValueNode("apps/setting1", "val1"), out var dontCare).Should().BeFalse();
        }

        [Fact]
        public void TryUpdateKeyWithDifferentValueDoesUpdate()
        {
            var consulState = _consulState.UpdateKVNode(new KeyValueNode("apps/setting1", "val1"));
            consulState.TryUpdateKVNode(new KeyValueNode("apps/setting1", "val2"), out var updatedState).Should().BeTrue();
            updatedState.KVStore.GetValue("apps/setting1").Should().Be("val2");
        }

        [Fact]
        public void TryUpdateKeyPrefixWithSameChildKeysDoesNotUpdate()
        {
            var consulState = _consulState.UpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/setting1", "val1"),
                new KeyValueNode("apps/myapp/setting2", "val2"),
            });
            consulState.TryUpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/setting1", "val1"),
                new KeyValueNode("apps/myapp/setting2", "val2"),
            }, out var dontCare).Should().BeFalse();
        }

        [Fact]
        public void TryUpdateKeyPrefixWithAddedChildKeysDoesUpdate()
        {
            var consulState = _consulState.UpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/setting1", "val1"),
                new KeyValueNode("apps/myapp/setting2", "val2"),
            });
            consulState.TryUpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/setting1", "val1"),
                new KeyValueNode("apps/myapp/setting2", "val2"),
                new KeyValueNode("apps/myapp/setting3", "val3"),
            }, out var updatedState).Should().BeTrue();
            updatedState.KVStore.GetChildren("apps/myapp").Should().HaveCount(3);
        }

        [Fact]
        public void TryUpdateKeyPrefixWithUpdatedValueDoesUpdate()
        {
            var consulState = _consulState.UpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/setting1", "val1"),
                new KeyValueNode("apps/myapp/setting2", "val2"),
            });
            consulState.TryUpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/setting1", "val3"),
                new KeyValueNode("apps/myapp/setting2", "val2")
            }, out var updatedState).Should().BeTrue();
            updatedState.KVStore.GetValue("apps/myapp/setting1").Should().Be("val3");
            updatedState.KVStore.GetValue("apps/myapp/setting2").Should().Be("val2");
        }

        [Fact]
        public void MissingKeyPrefixIsUnmarkedAsMissingWhenOneIsAdded()
        {
            var consulState = _consulState.MarkKeyPrefixAsMissingOrEmpty("apps/myapp");
            consulState.TryUpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/setting1", "val1")
            }, out var updatedState).Should().BeTrue();

            updatedState.MissingKeyPrefixes.Should().NotContain("apps/myapp");
            updatedState.KVStore.GetValue("apps/myapp/setting1").Should().Be("val1");
        }

        [Fact]
        public void ExistingChildKeysAreRemovedWhenKeyPrefixIsMarkedAsMissing()
        {
            var consulState = _consulState.UpdateKVNodes(new[]
            {
                new KeyValueNode("apps/myapp/setting1", "val1"),
                new KeyValueNode("apps/myapp/setting2", "val2"),
            });
            consulState.TryMarkKeyPrefixAsMissingOrEmpty("apps/myapp", out var updatedState).Should().BeTrue();
            updatedState.MissingKeyPrefixes.Should().Contain("apps/myapp");
            updatedState.KVStore.GetChildren("apps/myapp").Should().BeEmpty();
        }
    }
}