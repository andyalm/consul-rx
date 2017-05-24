using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;

namespace ConsulRx
{
    public class ChangeTrackingCollection<T> : IEnumerable<T>
    {
        private readonly Func<T, string> _keyAccessor;
        private readonly ConcurrentDictionary<string, T> _dictionary = new ConcurrentDictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        private readonly object _mutex = new object();
        private readonly Subject<IEnumerable<T>> _changes = new Subject<IEnumerable<T>>();
        private readonly string _itemName = typeof(T).Name;

        public ChangeTrackingCollection(Func<T,string> keyAccessor)
        {
            _keyAccessor = keyAccessor;
        }

        public IObservable<IEnumerable<T>> Changes => _changes;

        public T Get(string key)
        {
            if (_dictionary.TryGetValue(key, out T value))
                return value;
            else
                return default(T);
        }

        public void TryUpdateAll(IEnumerable<T> values)
        {
            if(!values.Any())
                return;

            bool changed = false;
            lock (_mutex)
            {
                foreach (var value in values)
                {
                    var key = _keyAccessor(value);
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
            }

            if (changed)
            {
                _changes.OnNext(values);
            }
        }

        public void TryUpdate(T value)
        {
            TryUpdateAll(new[] { value });
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