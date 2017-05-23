using Consul;

namespace ReactiveConsul
{
    public class KeyObservation : IConsulObservation
    {
        public string Key { get; }
        public QueryResult<KVPair> Result { get; }
        QueryResult IConsulObservation.Result => Result;

        public KeyObservation(string key, QueryResult<KVPair> result)
        {
            Key = key;
            Result = result;
        }

        public KeyValueNode ToKeyValueNode()
        {
            return new KeyValueNode(Key, Result.Response?.Value ?? new byte[0]);
        }
    }
}