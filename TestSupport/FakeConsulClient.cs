using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using ConsulRx.UnitTests;

namespace ConsulRx.TestSupport
{
    public class FakeConsulClient : ICatalogEndpoint, IKVEndpoint, IConsulClient
    {
        private readonly Dictionary<string, TaskCompletionSource<QueryResult<CatalogService[]>>> _serviceCalls =
            new Dictionary<string, TaskCompletionSource<QueryResult<CatalogService[]>>>(
                StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncAutoResetEvent> _waitingServiceCalls = new Dictionary<string, AsyncAutoResetEvent>(StringComparer.OrdinalIgnoreCase);
        
        public Task<QueryResult<CatalogService[]>> Service(string service, CancellationToken ct = new CancellationToken())
        {
            return Service(service, null, ct);
        }

        public Task<QueryResult<CatalogService[]>> Service(string service, string tag, CancellationToken ct = new CancellationToken())
        {
            return Service(service, null, null, ct);
        }

        public Task<QueryResult<CatalogService[]>> Service(string service, string tag, QueryOptions q, CancellationToken ct = new CancellationToken())
        {
            if (_waitingServiceCalls.TryGetValue(service, out var waitingEvent))
            {
                waitingEvent.Set();
                _waitingServiceCalls.Remove(service);
            }
            var completionSource = new TaskCompletionSource<QueryResult<CatalogService[]>>();
            _serviceCalls[service] = completionSource;

            return completionSource.Task;
        }

        public async Task CompleteServiceAsync(string serviceName, QueryResult<CatalogService[]> result)
        {
            if (_serviceCalls.TryGetValue(serviceName, out var completionSource))
            {
                _serviceCalls.Remove(serviceName);
                completionSource.SetResult(result);
                await completionSource.Task;
            }
            else
            {
                var serviceCallResetEvent = new AsyncAutoResetEvent(false);
                _waitingServiceCalls[serviceName] = serviceCallResetEvent;
                if (!(await serviceCallResetEvent.WaitAsync(500)))
                {
                    throw new InvalidOperationException($"Timed out waiting for a request for service requst '{serviceName}' to be initiated");
                }
            }
        }
        
        private readonly Dictionary<string, TaskCompletionSource<QueryResult<KVPair>>> _getCalls = new Dictionary<string, TaskCompletionSource<QueryResult<KVPair>>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncAutoResetEvent> _waitingGetCalls = new Dictionary<string, AsyncAutoResetEvent>(StringComparer.OrdinalIgnoreCase);

        public Task<QueryResult<KVPair>> Get(string key, CancellationToken ct = new CancellationToken())
        {
            return Get(key, null, ct);
        }

        public Task<QueryResult<KVPair>> Get(string key, QueryOptions q, CancellationToken ct = new CancellationToken())
        {
            if (_waitingGetCalls.TryGetValue(key, out var waitingEvent))
            {
                waitingEvent.Set();
                _waitingGetCalls.Remove(key);
            }
            var completionSource = new TaskCompletionSource<QueryResult<KVPair>>();
            _getCalls[key] = completionSource;

            return completionSource.Task;
        }

        public async Task CompleteGetAsync(string key, QueryResult<KVPair> result)
        {
            if (_getCalls.TryGetValue(key, out var completionSource))
            {
                _getCalls.Remove(key);
                completionSource.SetResult(result);
                await completionSource.Task;
            }
            else
            {
                var serviceCallResetEvent = new AsyncAutoResetEvent(false);
                _waitingGetCalls[key] = serviceCallResetEvent;
                if (!(await serviceCallResetEvent.WaitAsync(500)))
                {
                    throw new InvalidOperationException($"Timed out waiting for a request for key '{key}' to be initiated");
                }
            }
        }
        
        private readonly Dictionary<string,TaskCompletionSource<QueryResult<KVPair[]>>> _listCalls = new Dictionary<string, TaskCompletionSource<QueryResult<KVPair[]>>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncAutoResetEvent> _waitingListCalls = new Dictionary<string, AsyncAutoResetEvent>(StringComparer.OrdinalIgnoreCase);

        public Task<QueryResult<KVPair[]>> List(string prefix, CancellationToken ct = new CancellationToken())
        {
            return List(prefix, null, ct);
        }

        public Task<QueryResult<KVPair[]>> List(string prefix, QueryOptions q, CancellationToken ct = new CancellationToken())
        {
            if (_waitingListCalls.TryGetValue(prefix, out var waitingEvent))
            {
                waitingEvent.Set();
                _waitingListCalls.Remove(prefix);
            }
            var completionSource = new TaskCompletionSource<QueryResult<KVPair[]>>();
            _listCalls[prefix] = completionSource;

            return completionSource.Task;
        }

        public async Task CompleteListAsync(string prefix, QueryResult<KVPair[]> result)
        {
            if (_listCalls.TryGetValue(prefix, out var completionSource))
            {
                _listCalls.Remove(prefix);
                completionSource.SetResult(result);
                await completionSource.Task;
            }
            else
            {
                var serviceCallResetEvent = new AsyncAutoResetEvent(false);
                _waitingListCalls[prefix] = serviceCallResetEvent;
                if (!(await serviceCallResetEvent.WaitAsync(500)))
                {
                    throw new InvalidOperationException($"Timed out waiting for a request for key prefix '{prefix}' to be initiated");
                }
            }
        }
        
        public Task<QueryResult<string[]>> Keys(string prefix, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<QueryResult<string[]>> Keys(string prefix, string separator, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<QueryResult<string[]>> Keys(string prefix, string separator, QueryOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> Acquire(KVPair p, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> Acquire(KVPair p, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> CAS(KVPair p, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> CAS(KVPair p, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> Delete(string key, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> Delete(string key, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> DeleteCAS(KVPair p, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> DeleteCAS(KVPair p, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> DeleteTree(string prefix, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> DeleteTree(string prefix, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> Put(KVPair p, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> Put(KVPair p, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> Release(KVPair p, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<bool>> Release(KVPair p, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<KVTxnResponse>> Txn(List<KVTxnOp> txn, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<WriteResult<KVTxnResponse>> Txn(List<KVTxnOp> txn, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<QueryResult<string[]>> Datacenters(CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<WriteResult> Deregister(CatalogDeregistration reg, CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<WriteResult> Deregister(CatalogDeregistration reg, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<QueryResult<CatalogNode>> Node(string node, CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<QueryResult<CatalogNode>> Node(string node, QueryOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<QueryResult<Node[]>> Nodes(CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<QueryResult<Node[]>> Nodes(QueryOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<WriteResult> Register(CatalogRegistration reg, CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<WriteResult> Register(CatalogRegistration reg, WriteOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<QueryResult<Dictionary<string, string[]>>> Services(CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task<QueryResult<Dictionary<string, string[]>>> Services(QueryOptions q, CancellationToken ct = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<IDistributedLock> AcquireLock(LockOptions opts, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IDistributedLock> AcquireLock(string key, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IDistributedSemaphore> AcquireSemaphore(SemaphoreOptions opts, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IDistributedSemaphore> AcquireSemaphore(string prefix, int limit, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public IDistributedLock CreateLock(LockOptions opts)
        {
            throw new NotImplementedException();
        }

        public IDistributedLock CreateLock(string key)
        {
            throw new NotImplementedException();
        }

        public Task ExecuteInSemaphore(SemaphoreOptions opts, Action a, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task ExecuteInSemaphore(string prefix, int limit, Action a, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task ExecuteLocked(LockOptions opts, Action action, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task ExecuteLocked(LockOptions opts, CancellationToken ct, Action action)
        {
            throw new NotImplementedException();
        }

        public Task ExecuteLocked(string key, Action action, CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task ExecuteLocked(string key, CancellationToken ct, Action action)
        {
            throw new NotImplementedException();
        }

        public IDistributedSemaphore Semaphore(SemaphoreOptions opts)
        {
            throw new NotImplementedException();
        }

        public IDistributedSemaphore Semaphore(string prefix, int limit)
        {
            throw new NotImplementedException();
        }

        public IACLEndpoint ACL { get; }
        public IAgentEndpoint Agent { get; }
        public ICatalogEndpoint Catalog => this;
        public IEventEndpoint Event { get; }
        public IHealthEndpoint Health { get; }
        public IKVEndpoint KV => this;
        public IRawEndpoint Raw { get; }
        public ISessionEndpoint Session { get; }
        public IStatusEndpoint Status { get; }
        public IOperatorEndpoint Operator { get; }
        public IPreparedQueryEndpoint PreparedQuery { get; }
        public ICoordinateEndpoint Coordinate { get; }
        public ISnapshotEndpoint Snapshot { get; }
    }
}