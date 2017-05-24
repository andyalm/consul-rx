using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ConsulRx
{
    public class KeyValueStore : IEnumerable<KeyValueNode>
    {
        private readonly ImmutableDictionary<string,KeyValueNode> _leaves;

        public KeyValueStore()
        {
            _leaves = ImmutableDictionary<string, KeyValueNode>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
        }

        public KeyValueStore(IEnumerable<KeyValueNode> leaves)
        {
            _leaves = ImmutableDictionary.Create<string,KeyValueNode>(StringComparer.OrdinalIgnoreCase).AddRange(leaves.Select(n => n.ToIndexedPair()));
        }

        public KeyValueStore(ImmutableDictionary<string, KeyValueNode> leaves)
        {
            _leaves = leaves;
        }

        public KeyValueStore Update(KeyValueNode kvNode)
        {
            return new KeyValueStore(_leaves.SetItem(kvNode.FullKey, kvNode));
        }

        public KeyValueStore Update(IEnumerable<KeyValueNode> kvNodes)
        {
            return new KeyValueStore(_leaves.SetItems(kvNodes.Select(n => n.ToIndexedPair())));
        }

        public string GetValue(string fullKey)
        {
            if (_leaves.TryGetValue(fullKey, out var node))
                return node.Value;
            
            return null;
        }

        public IEnumerator<KeyValueNode> GetEnumerator()
        {
            return _leaves.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<KeyValueNode> GetChildren(string keyPrefix)
        {
            return _leaves.Values.Where(node => node.IsChildOf(keyPrefix));
        }

        public IEnumerable<KeyValueNode> GetTree(string keyPrefix)
        {
            return _leaves.Values.Where(node => node.IsDescendentOf(keyPrefix));
        }
    }
}