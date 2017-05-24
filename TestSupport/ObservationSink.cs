using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ConsulRx.TestSupport
{
    public class ObservationSink<T> : IEnumerable<T>
    {
        private readonly ConcurrentBag<T> _observations = new ConcurrentBag<T>();
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public void Add(T item)
        {
            _resetEvent.Set();
            _observations.Add(item);
        }

        public void WaitForAdd()
        {
            _resetEvent.WaitOne(100);
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}