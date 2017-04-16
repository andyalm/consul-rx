using System;
using System.Linq;
using System.Net;
using Consul;

namespace ConsulTemplate.Reactive
{
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
}