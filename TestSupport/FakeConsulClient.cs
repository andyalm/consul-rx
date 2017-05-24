using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Consul;

namespace ConsulRx.TestSupport
{
    public class FakeConsulClient : ICatalogEndpoint, IKVEndpoint, IConsulClient
    {
        private readonly Dictionary<string, TaskCompletionSource<QueryResult<CatalogService[]>>> _serviceCalls =
            new Dictionary<string, TaskCompletionSource<QueryResult<CatalogService[]>>>(
                StringComparer.OrdinalIgnoreCase);
        
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
            var completionSource = new TaskCompletionSource<QueryResult<CatalogService[]>>();
            _serviceCalls[service] = completionSource;

            return completionSource.Task;
        }

        public void CompleteService(string serviceName, QueryResult<CatalogService[]> result)
        {
            if (_serviceCalls.TryGetValue(serviceName, out var completionSource))
            {
                completionSource.SetResult(result);
                _serviceCalls.Remove(serviceName);
            }
            else
            {
                throw new InvalidOperationException($"There are not outstanding requests for service '{serviceName}'");
            }
        }
        
        private readonly Dictionary<string, TaskCompletionSource<QueryResult<KVPair>>> _getCalls = new Dictionary<string, TaskCompletionSource<QueryResult<KVPair>>>(StringComparer.OrdinalIgnoreCase);
        
        public Task<QueryResult<KVPair>> Get(string key, CancellationToken ct = new CancellationToken())
        {
            return Get(key, null, ct);
        }

        public Task<QueryResult<KVPair>> Get(string key, QueryOptions q, CancellationToken ct = new CancellationToken())
        {
            var completionSource = new TaskCompletionSource<QueryResult<KVPair>>();
            _getCalls[key] = completionSource;

            return completionSource.Task;
        }

        public void CompleteGet(string key, QueryResult<KVPair> result)
        {
            if (_getCalls.TryGetValue(key, out var completionSource))
            {
                completionSource.SetResult(result);
                _getCalls.Remove(key);
            }
            else
            {
                throw new InvalidOperationException($"There are not outstanding requests for key '{key}'");
            }
        }
        
        private readonly Dictionary<string,TaskCompletionSource<QueryResult<KVPair[]>>> _listCalls = new Dictionary<string, TaskCompletionSource<QueryResult<KVPair[]>>>(StringComparer.OrdinalIgnoreCase);
        
        public Task<QueryResult<KVPair[]>> List(string prefix, CancellationToken ct = new CancellationToken())
        {
            return List(prefix, null, ct);
        }

        public Task<QueryResult<KVPair[]>> List(string prefix, QueryOptions q, CancellationToken ct = new CancellationToken())
        {
            var completionSource = new TaskCompletionSource<QueryResult<KVPair[]>>();
            _listCalls[prefix] = completionSource;

            return completionSource.Task;
        }

        public void CompleteList(string prefix, QueryResult<KVPair[]> result)
        {
            if (_listCalls.TryGetValue(prefix, out var completionSource))
            {
                _listCalls.Remove(prefix);
                completionSource.SetResult(result);
            }
            else
            {
                throw new InvalidOperationException($"There are no outstanding requests for key prefix '{prefix}'");
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