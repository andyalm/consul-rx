using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Consul;

namespace ConsulTemplate.Reactive
{
    public interface IConsulObservation
    {
        QueryResult Result { get; }
    }

    public class ServiceObservation : IConsulObservation
    {
        public string ServiceName { get; }
        public QueryResult<CatalogService[]> Result { get; }

        QueryResult IConsulObservation.Result => Result;

        public ServiceObservation(string serviceName, QueryResult<CatalogService[]> result)
        {
            ServiceName = serviceName;
            Result = result;
        }

        public Service ToService()
        {
            if (Result.Response == null || Result.Response.Length == 0)
            {
                return new Service { Name = ServiceName, Id = null, Nodes = new ServiceNode[0] };
            }

            return new Service
            {
                Id = Result.Response.First().ServiceID,
                Name = ServiceName,
                Nodes = Result.Response.Select(n => new ServiceNode
                    {
                        Address = string.IsNullOrWhiteSpace(n.ServiceAddress) ? n.Address : n.ServiceAddress,
                        Name = n.Node,
                        Port = n.ServicePort,
                        Tags = n.ServiceTags
                    })
                    .ToArray()
            };
        }
    }

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