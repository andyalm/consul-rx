using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ConsulRx.Configuration
{
    public class FileSystemEmergencyCache : IEmergencyCache
    {
        private readonly Lazy<string> _filePath;
        private readonly JsonSerializer _jsonSerializer;

        public FileSystemEmergencyCache()
        {
            _filePath = new Lazy<string>(ResolveFilePath);
            _jsonSerializer = new JsonSerializer();
        }
        
        public void Save(IDictionary<string, string> settings)
        {
            using (var stream = File.Open(_filePath.Value, FileMode.Create))
            {
                var writer = new JsonTextWriter(new StreamWriter(stream));
                _jsonSerializer.Serialize(writer, settings);
                writer.Flush();
            }
        }

        public bool TryLoad(out IDictionary<string, string> settings)
        {
            if (!File.Exists(_filePath.Value))
            {
                settings = null;
                return false;
            }
            try
            {
                using (var stream = File.OpenRead(_filePath.Value))
                {
                    var deserializedSettings = _jsonSerializer
                        .Deserialize<IDictionary<string, string>>(new JsonTextReader(new StreamReader(stream)));
                    settings = new Dictionary<string, string>(deserializedSettings, StringComparer.OrdinalIgnoreCase);
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new ConsulRxConfigurationException(
                    $"An error occurred while trying to read the ConsulRx emergency cache file. It might be corrupted (filename: {_filePath.Value})"
                    , ex);
            }
        }


        private string ResolveFilePath()
        {
            var path = Environment.GetEnvironmentVariable("CONSULRX_EMERGENCY_CACHE_PATH");
            if (!string.IsNullOrWhiteSpace(path))
                return path;

            return Path.Combine(AppContext.BaseDirectory, "consulrx-emergency-cache.json");
        }
    }
}