using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ConsulRazor.Reactive
{
    public class KeyValueStore : IEnumerable<KeyValueNode>
    {
        private readonly ChangeTrackingCollection<KeyValueNode> _leaves = new ChangeTrackingCollection<KeyValueNode>(n => n.FullKey);

        public IObservable<IEnumerable<KeyValueNode>> Changes => _leaves.Changes;

        public void Update(KeyValueNode kvNode)
        {
            _leaves.TryUpdate(kvNode);
        }

        public void Update(IEnumerable<KeyValueNode> kvNodes)
        {
            _leaves.TryUpdateAll(kvNodes);
        }

        public string GetValue(string fullKey)
        {
            return _leaves.Get(fullKey)?.Value;
        }

        public IEnumerator<KeyValueNode> GetEnumerator()
        {
            return _leaves.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<KeyValueNode> GetChildren(string keyPrefix)
        {
            return _leaves.Where(node => node.IsChildOf(keyPrefix));
        }

        public IEnumerable<KeyValueNode> GetTree(string keyPrefix)
        {
            return _leaves.Where(node => node.IsDescendentOf(keyPrefix));
        }
    }
}