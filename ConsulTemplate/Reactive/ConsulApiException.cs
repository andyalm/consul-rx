using System;
using System.Net;

namespace ConsulTemplate.Reactive
{
    public class ConsulApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public ConsulApiException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}