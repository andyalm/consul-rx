using System;
using Consul;

namespace ConsulTemplate.Reactive
{
    public static class ObservableClientExtensions
    {
        public static ObservableConsul Observable(this ConsulClient client, TimeSpan? longPollMaxWait)
        {
            return new ObservableConsul(client, longPollMaxWait);
        }
    }
}