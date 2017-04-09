using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace ConsulTemplate.Reactive
{
    public class ChangeTrackingCollection<T> : IEnumerable<T>
    {
        private readonly Func<T, string> _keyAccessor;
        private readonly ConcurrentDictionary<string, T> _dictionary = new ConcurrentDictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        private readonly object _mutex = new object();
        private readonly Subject<T> _changes = new Subject<T>();
        private readonly string _itemName = typeof(T).Name;

        public ChangeTrackingCollection(Func<T,string> keyAccessor)
        {
            _keyAccessor = keyAccessor;
        }

        public IObservable<T> Changes => _changes;

        public void TryUpdate(T value)
        {
            bool changed = false;
            var key = _keyAccessor(value);
            lock (_mutex)
            {
                if (_dictionary.TryAdd(key, value))
                {
                    Console.WriteLine($"Adding {_itemName} {key}");
                    changed = true;
                }
                else
                {
                    var existingValue = _dictionary[key];
                    if (!existingValue.Equals(value))
                    {
                        Console.WriteLine($"Updating {_itemName} {key}");
                        _dictionary[key] = value;
                        changed = true;
                    }
                    else
                    {
                        Console.WriteLine($"Existing value for {_itemName} {key} is the same as the new value");
                    }
                }
            }

            if (changed)
            {
                _changes.OnNext(value);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _dictionary.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}