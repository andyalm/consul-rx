using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ConsulRazor.Reactive
{
    public static class ConsulStateObservableExtensions
    {
        public static Task<ConsulState> ObserveDependencies(this IObservableConsul client, ConsulDependencies dependencies)
        {
            var dependenciesCompletionSource = new TaskCompletionSource<ConsulState>();
            var consulState = new ConsulState();
            consulState.Changes
                .TakeWhile(_ => !consulState.SatisfiesAll(dependencies))
                .Subscribe(_ => { }, onCompleted: () => dependenciesCompletionSource.SetResult(consulState));
            consulState.ObserveDependencies(client, dependencies);

            return dependenciesCompletionSource.Task;
        }  
    }
}