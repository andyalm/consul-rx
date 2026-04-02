using System;

namespace ConsulRx
{
    public class ObservableConsulConfiguration
    {
        private string _endpoint;

        public string Endpoint
        {
            get
            {
                var endpoint = _endpoint
                    ?? Environment.GetEnvironmentVariable("CONSUL_HTTP_ADDR")
                    ?? "localhost:8500";

                return NormalizeEndpoint(endpoint);
            }
            set => _endpoint = value;
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                && (uri.Scheme == "http" || uri.Scheme == "https"))
                return endpoint;

            return $"http://{endpoint}";
        }
        public string Datacenter { get; set; }
        public string AclToken { get; set; }
        public TimeSpan? LongPollMaxWait { get; set; }
        public TimeSpan? RetryDelay { get; set; } = Defaults.ErrorRetryInterval;
        public ConsistencyMode ConsistencyMode { get; set; } = ConsistencyMode.Default;
    }
}