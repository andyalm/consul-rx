using Consul;

namespace ConsulTemplate.Reactive
{
    public static class ObservableClientExtensions
    {
        public static ObservableConsul Observable(this ConsulClient client, ObservableConsulConfiguration config = null)
        {
            return new ObservableConsul(client, config);
        }
    }
}