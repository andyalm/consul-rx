using System.Text;

namespace ConsulTemplate.Reactive
{
    public class KeyValueNode
    {
        public string FullKey { get; }
        public string Value { get; }

        public KeyValueNode(string fullKey, byte[] value) : this(fullKey, Encoding.UTF8.GetString(value)) { }

        public KeyValueNode(string fullKey, string value)
        {
            FullKey = fullKey;
            Value = value;
        }

        public string LeafKey
        {
            get
            {
                var lastSlashIndex = FullKey.LastIndexOf('/');
                if (lastSlashIndex < 0)
                    return FullKey;
                else
                    return FullKey.Substring(lastSlashIndex + 1);
            }
        }

        public string ParentKey
        {
            get
            {
                var lastSlashIndex = FullKey.LastIndexOf('/');
                if (lastSlashIndex < 0)
                    return FullKey;
                else
                    return FullKey.Remove(lastSlashIndex);
            }
        }

        public bool IsChildOf(string otherKey)
        {
            return otherKey.Equals(ParentKey);
        }

        public bool IsDescendentOf(string prefix)
        {
            return FullKey.StartsWith(prefix);
        }

        protected bool Equals(KeyValueNode other)
        {
            return string.Equals(FullKey, other.FullKey) && string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KeyValueNode) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((FullKey != null ? FullKey.GetHashCode() : 0) * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            }
        }
    }
}