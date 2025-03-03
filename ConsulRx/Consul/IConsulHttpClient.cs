using System.Threading.Tasks;

namespace ConsulRx
{
    public interface IConsulHttpClient
    {
        Task<QueryResult<CatalogService[]>> GetServiceAsync(string serviceName, QueryOptions options);
        Task<QueryResult<KVPair>> GetKeyAsync(string key, QueryOptions options);
        Task<QueryResult<KVPair[]>> GetKeyListAsync(string prefix, QueryOptions options);
    }
}
