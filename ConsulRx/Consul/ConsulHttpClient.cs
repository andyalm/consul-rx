using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsulRx
{
    public class ConsulHttpClient : IConsulHttpClient
    {
        private readonly HttpClient _httpClient;

        public ConsulHttpClient(Uri address, string datacenter = null)
        {
            _httpClient = new HttpClient { BaseAddress = address };
            Datacenter = datacenter;
        }

        public ConsulHttpClient(HttpClient httpClient, string datacenter = null)
        {
            _httpClient = httpClient;
            Datacenter = datacenter;
        }

        private string Datacenter { get; }

        public async Task<QueryResult<CatalogService[]>> GetServiceAsync(string serviceName, QueryOptions options)
        {
            var path = $"/v1/catalog/service/{Uri.EscapeDataString(serviceName)}";
            var response = await SendAsync(path, options).ConfigureAwait(false);
            var result = CreateResult<CatalogService[]>(response);

            if (result.StatusCode == HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                result.Response = JsonSerializer.Deserialize<CatalogService[]>(json, JsonOptions);
            }

            return result;
        }

        public async Task<QueryResult<KVPair>> GetKeyAsync(string key, QueryOptions options)
        {
            var path = $"/v1/kv/{key}";
            var response = await SendAsync(path, options).ConfigureAwait(false);
            var result = CreateResult<KVPair>(response);

            if (result.StatusCode == HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var dtos = JsonSerializer.Deserialize<KVPairDto[]>(json, JsonOptions);
                if (dtos != null && dtos.Length > 0)
                {
                    result.Response = dtos[0].ToKVPair();
                }
            }

            return result;
        }

        public async Task<QueryResult<KVPair[]>> GetKeyListAsync(string prefix, QueryOptions options)
        {
            var path = $"/v1/kv/{prefix}?recurse";
            var response = await SendAsync(path, options).ConfigureAwait(false);
            var result = CreateResult<KVPair[]>(response);

            if (result.StatusCode == HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var dtos = JsonSerializer.Deserialize<KVPairDto[]>(json, JsonOptions);
                if (dtos != null)
                {
                    var pairs = new KVPair[dtos.Length];
                    for (int i = 0; i < dtos.Length; i++)
                    {
                        pairs[i] = dtos[i].ToKVPair();
                    }
                    result.Response = pairs;
                }
            }

            return result;
        }

        private async Task<HttpResponseMessage> SendAsync(string path, QueryOptions options)
        {
            var separator = path.Contains("?") ? "&" : "?";
            var query = BuildQueryString(options);
            if (!string.IsNullOrEmpty(query))
            {
                path = $"{path}{separator}{query}";
            }

            var response = await _httpClient.GetAsync(path).ConfigureAwait(false);
            return response;
        }

        private string BuildQueryString(QueryOptions options)
        {
            if (options == null) return string.Empty;

            var parts = new System.Collections.Generic.List<string>();

            if (options.WaitIndex > 0)
                parts.Add($"index={options.WaitIndex}");

            if (options.WaitTime.HasValue)
            {
                var totalSeconds = (long)options.WaitTime.Value.TotalSeconds;
                parts.Add($"wait={totalSeconds}s");
            }

            if (!string.IsNullOrEmpty(options.Token))
                parts.Add($"token={Uri.EscapeDataString(options.Token)}");

            if (options.Consistency == ConsistencyMode.Consistent)
                parts.Add("consistent");
            else if (options.Consistency == ConsistencyMode.Stale)
                parts.Add("stale");

            if (!string.IsNullOrEmpty(Datacenter))
                parts.Add($"dc={Uri.EscapeDataString(Datacenter)}");

            return string.Join("&", parts);
        }

        private static QueryResult<T> CreateResult<T>(HttpResponseMessage response)
        {
            var result = new QueryResult<T>
            {
                StatusCode = response.StatusCode
            };

            if (response.Headers.TryGetValues("X-Consul-Index", out var indexValues))
            {
                foreach (var val in indexValues)
                {
                    if (ulong.TryParse(val, out var index))
                    {
                        result.LastIndex = index;
                        break;
                    }
                }
            }

            if (response.Headers.TryGetValues("X-Consul-Knownleader", out var leaderValues))
            {
                foreach (var val in leaderValues)
                {
                    if (bool.TryParse(val, out var knownLeader))
                    {
                        result.KnownLeader = knownLeader;
                        break;
                    }
                }
            }

            return result;
        }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private class KVPairDto
        {
            public string Key { get; set; }
            public string Value { get; set; }

            public KVPair ToKVPair()
            {
                return new KVPair(Key)
                {
                    Value = Value != null ? Convert.FromBase64String(Value) : null
                };
            }
        }
    }
}
