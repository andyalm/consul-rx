using System.Collections;
using System.Collections.Generic;

namespace ConsulRx
{
    public interface IReadOnlyKeyValueStore : IEnumerable<KeyValueNode>
    {
        bool ContainsKey(string fullKey);
        string GetValue(string fullKey);
        IEnumerable<KeyValueNode> GetChildren(string keyPrefix);
        IEnumerable<KeyValueNode> GetTree(string keyPrefix);
        bool ContainsKeyStartingWith(string keyPrefix);
    }
}