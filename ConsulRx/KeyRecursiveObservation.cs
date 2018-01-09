using System.Collections.Generic;
using System.Linq;
using Consul;

namespace ConsulRx
{
    public class KeyRecursiveObservation : IConsulObservation
    {
        public string KeyPrefix { get; }
        public QueryResult<KVPair[]> Result { get; }
        QueryResult IConsulObservation.Result => Result;

        public KeyRecursiveObservation(string keyPrefix, QueryResult<KVPair[]> result)
        {
            KeyPrefix = keyPrefix;
            Result = result;
        }

        public IEnumerable<KeyValueNode> ToKeyValueNodes()
        {
            if (Result.Response == null)
                return Enumerable.Empty<KeyValueNode>();

            return Result.Response.Select(p => new KeyValueNode(p.Key, p.Value));
        }
    }
}