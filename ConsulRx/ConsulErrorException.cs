using System;
using System.Net;
using Consul;

namespace ConsulRx
{
    public class ConsulErrorException : Exception
    {
        public ConsulErrorException(QueryResult result)
        {
            Result = result;
        }

        public QueryResult Result { get; }
    }
}