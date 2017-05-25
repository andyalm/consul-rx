using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsulRx.UnitTests;

namespace ConsulRx.TestSupport
{
    public class ObservationSink<T> : IEnumerable<T>
    {
        private readonly ConcurrentBag<T> _observations = new ConcurrentBag<T>();
        private readonly AsyncAutoResetEvent _resetEvent = new AsyncAutoResetEvent(false);

        public void Add(T item)
        {
            _observations.Add(item);
            _resetEvent.Set();
        }

        public Task WaitForAddAsync()
        {
            return _resetEvent.WaitAsync(1000);
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return _observations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}