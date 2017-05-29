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
                if (!string.IsNullOrEmpty(_endpoint))
                    return _endpoint;

                return null;
            }
            set => _endpoint = value;
        }
        public string Datacenter { get; set; }
        public string AclToken { get; set; }
        public TimeSpan? LongPollMaxWait { get; set; }
        public TimeSpan? RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    }
}