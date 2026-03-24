using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsulRx.UnitTests;

namespace ConsulRx.TestSupport
{
    public class FakeConsulClient : IConsulHttpClient
    {
        private readonly Dictionary<string, TaskCompletionSource<QueryResult<CatalogService[]>>> _serviceCalls =
            new Dictionary<string, TaskCompletionSource<QueryResult<CatalogService[]>>>(
                StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncAutoResetEvent> _waitingServiceCalls = new Dictionary<string, AsyncAutoResetEvent>(StringComparer.OrdinalIgnoreCase);

        public Task<QueryResult<CatalogService[]>> GetServiceAsync(string serviceName, QueryOptions options)
        {
            var completionSource = new TaskCompletionSource<QueryResult<CatalogService[]>>();
            _serviceCalls[serviceName] = completionSource;

            if (_waitingServiceCalls.TryGetValue(serviceName, out var waitingEvent))
            {
                _waitingServiceCalls.Remove(serviceName);
                waitingEvent.Set();
            }

            return completionSource.Task;
        }

        public async Task CompleteServiceAsync(string serviceName, QueryResult<CatalogService[]> result)
        {
            if (!_serviceCalls.TryGetValue(serviceName, out var completionSource))
            {
                var serviceCallResetEvent = new AsyncAutoResetEvent(false);
                _waitingServiceCalls[serviceName] = serviceCallResetEvent;
                if (!(await serviceCallResetEvent.WaitAsync(500)))
                {
                    throw new InvalidOperationException($"Timed out waiting for a request for service requst '{serviceName}' to be initiated");
                }
                completionSource = _serviceCalls[serviceName];
            }
            _serviceCalls.Remove(serviceName);
            completionSource.SetResult(result);
            await completionSource.Task;
        }

        private readonly Dictionary<string, TaskCompletionSource<QueryResult<KVPair>>> _getCalls = new Dictionary<string, TaskCompletionSource<QueryResult<KVPair>>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncAutoResetEvent> _waitingGetCalls = new Dictionary<string, AsyncAutoResetEvent>(StringComparer.OrdinalIgnoreCase);

        public Task<QueryResult<KVPair>> GetKeyAsync(string key, QueryOptions options)
        {
            var completionSource = new TaskCompletionSource<QueryResult<KVPair>>();
            _getCalls[key] = completionSource;

            if (_waitingGetCalls.TryGetValue(key, out var waitingEvent))
            {
                _waitingGetCalls.Remove(key);
                waitingEvent.Set();
            }

            return completionSource.Task;
        }

        public async Task CompleteGetAsync(string key, QueryResult<KVPair> result)
        {
            if (!_getCalls.TryGetValue(key, out var completionSource))
            {
                var resetEvent = new AsyncAutoResetEvent(false);
                _waitingGetCalls[key] = resetEvent;
                if (!(await resetEvent.WaitAsync(500)))
                {
                    throw new InvalidOperationException($"Timed out waiting for a request for key '{key}' to be initiated");
                }
                completionSource = _getCalls[key];
            }
            _getCalls.Remove(key);
            completionSource.SetResult(result);
            await completionSource.Task;
        }

        private readonly Dictionary<string,TaskCompletionSource<QueryResult<KVPair[]>>> _listCalls = new Dictionary<string, TaskCompletionSource<QueryResult<KVPair[]>>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncAutoResetEvent> _waitingListCalls = new Dictionary<string, AsyncAutoResetEvent>(StringComparer.OrdinalIgnoreCase);

        public Task<QueryResult<KVPair[]>> GetKeyListAsync(string prefix, QueryOptions options)
        {
            var completionSource = new TaskCompletionSource<QueryResult<KVPair[]>>();
            _listCalls[prefix] = completionSource;

            if (_waitingListCalls.TryGetValue(prefix, out var waitingEvent))
            {
                _waitingListCalls.Remove(prefix);
                waitingEvent.Set();
            }

            return completionSource.Task;
        }

        public async Task CompleteListAsync(string prefix, QueryResult<KVPair[]> result)
        {
            if (!_listCalls.TryGetValue(prefix, out var completionSource))
            {
                var resetEvent = new AsyncAutoResetEvent(false);
                _waitingListCalls[prefix] = resetEvent;
                if (!(await resetEvent.WaitAsync(500)))
                {
                    throw new InvalidOperationException($"Timed out waiting for a request for key prefix '{prefix}' to be initiated");
                }
                completionSource = _listCalls[prefix];
            }
            _listCalls.Remove(prefix);
            completionSource.SetResult(result);
            await completionSource.Task;
        }
    }
}
