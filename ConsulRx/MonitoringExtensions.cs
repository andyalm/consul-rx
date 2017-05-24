using System.Collections.Generic;
using Consul;
using Spiffy.Monitoring;

namespace ConsulRx
{
    internal static class MonitoringExtensions
    {
        public static void IncludeConsulResult<T>(this EventContext eventContext, QueryResult<T> result)
        {
            eventContext["HttpStatusCode"] = result.StatusCode;
            eventContext["ResponseIndex"] = result.LastIndex;
            eventContext["KnownLeader"] = result.KnownLeader;
        }
    }
}