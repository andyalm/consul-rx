using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Consul;

namespace ReactiveConsul.TestSupport
{
    public class FakeConsulClient : ICatalogEndpoint, IConsulClient
    {
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
                throw new InvalidOperationException($"There are not outstanding requests for service '{serviceName}");
            }
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

        private readonly Dictionary<string, TaskCompletionSource<QueryResult<CatalogService[]>>> _serviceCalls =
            new Dictionary<string, TaskCompletionSource<QueryResult<CatalogService[]>>>(
                StringComparer.OrdinalIgnoreCase);

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
        public IKVEndpoint KV { get; }
        public IRawEndpoint Raw { get; }
        public ISessionEndpoint Session { get; }
        public IStatusEndpoint Status { get; }
        public IOperatorEndpoint Operator { get; }
        public IPreparedQueryEndpoint PreparedQuery { get; }
        public ICoordinateEndpoint Coordinate { get; }
        public ISnapshotEndpoint Snapshot { get; }
    }
}