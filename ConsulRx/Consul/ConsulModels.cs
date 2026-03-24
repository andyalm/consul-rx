using System;
using System.Net;

namespace ConsulRx
{
    public enum ConsistencyMode
    {
        Default,
        Consistent,
        Stale
    }

    public class QueryOptions
    {
        public string Token { get; set; }
        public ulong WaitIndex { get; set; }
        public TimeSpan? WaitTime { get; set; }
        public ConsistencyMode Consistency { get; set; }
    }

    public class QueryResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public ulong LastIndex { get; set; }
        public bool KnownLeader { get; set; }
    }

    public class QueryResult<T> : QueryResult
    {
        public T Response { get; set; }
    }

    public class KVPair
    {
        public KVPair(string key)
        {
            Key = key;
        }

        public string Key { get; set; }
        public byte[] Value { get; set; }
    }

    public class CatalogService
    {
        public string ServiceName { get; set; }
        public string ServiceID { get; set; }
        public string Node { get; set; }
        public string Address { get; set; }
        public string ServiceAddress { get; set; }
        public int ServicePort { get; set; }
        public string[] ServiceTags { get; set; }
    }
}
