using System;

namespace ConsulRx
{
    public static class Defaults
    {
        public static TimeSpan ErrorRetryInterval => TimeSpan.FromSeconds(15);

        public static TimeSpan UpdateMaxInterval => TimeSpan.FromSeconds(15);
    }
}