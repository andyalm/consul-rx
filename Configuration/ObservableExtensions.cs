using System;
using System.Reactive.Linq;

namespace ConsulRx.Configuration
{
    internal static class ObservableExtensions
    {
        public static IObservable<T> DelayedRetry<T>(this IObservable<T> src, TimeSpan delay)
        {
            if (delay == TimeSpan.Zero) return src.Retry();
            return src.Catch(src.DelaySubscription(delay).Retry());
        }
    }
}