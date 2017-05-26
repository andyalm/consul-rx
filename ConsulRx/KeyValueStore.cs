using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ConsulRx
{
    public class KeyValueStore : IReadOnlyKeyValueStore
    {
        private readonly ImmutableDictionary<string,KeyValueNode> _leaves;
        public static readonly KeyValueStore Empty = new KeyValueStore();

        private KeyValueStore()
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

        public bool TryUpdate(KeyValueNode kvNode, out KeyValueStore updatedStore)
        {
            if (_leaves.TryGetValue(kvNode.FullKey, out var existingNode) && existingNode.Equals(kvNode))
            {
                updatedStore = null;
                return false;
            }

            updatedStore = new KeyValueStore(_leaves.SetItem(kvNode.FullKey, kvNode));
            return true;
        }

        public bool TryUpdate(IEnumerable<KeyValueNode> kvNodes, out KeyValueStore updatedStore)
        {
            bool atLeastOneUpdate = false;
            var leaves = _leaves;
            foreach (var kvNode in kvNodes)
            {
                if (leaves.TryGetValue(kvNode.FullKey, out var existingNode) && existingNode.Equals(kvNode))
                {
                    continue;
                }
                atLeastOneUpdate = true;
                leaves = leaves.SetItem(kvNode.FullKey, kvNode);
            }

            if (atLeastOneUpdate)
            {
                updatedStore = new KeyValueStore(leaves);
                return true;
            }

            updatedStore = null;
            return false;
        }

        public bool TryRemoveKeysStartingWith(string keyPrefix, out KeyValueStore updatedStore)
        {
            var keysToRemove = _leaves.Keys.Where(key => key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (keysToRemove.Any())
            {
                updatedStore = new KeyValueStore(_leaves.RemoveRange(keysToRemove));
                return true;
            }

            updatedStore = null;
            return false;
        }

        public bool ContainsKey(string fullKey)
        {
            return _leaves.ContainsKey(fullKey);
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

        public bool ContainsKeyStartingWith(string keyPrefix)
        {
            return _leaves.Keys.Any(k => k.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}