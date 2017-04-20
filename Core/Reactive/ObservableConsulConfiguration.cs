using System;

namespace ConsulRazor.Reactive
{
    public class ObservableConsulConfiguration
    {
        public string Endpoint { get; set; }
        public string Datacenter { get; set; }
        public string AclToken { get; set; }
        public TimeSpan? LongPollMaxWait { get; set; }
    }
}