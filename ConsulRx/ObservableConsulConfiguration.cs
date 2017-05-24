using System;

namespace ConsulRx
{
    public class ObservableConsulConfiguration
    {
        public string Endpoint { get; set; }
        public string Datacenter { get; set; }
        public string AclToken { get; set; }
        public TimeSpan? LongPollMaxWait { get; set; }
        public TimeSpan? RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    }
}