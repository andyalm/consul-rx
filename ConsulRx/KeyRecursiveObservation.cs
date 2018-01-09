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

            return Result.Response
                .Where(p => p.Value != null) //no point in returning keys with null values. I believe these are just folder keys anyways.
                .Select(p => new KeyValueNode(p.Key, p.Value));
        }
    }
}