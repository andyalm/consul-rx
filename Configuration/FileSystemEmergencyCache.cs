using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ConsulRx.Configuration
{
    public class FileSystemEmergencyCache : IEmergencyCache
    {
        private readonly Lazy<string> _filePath;

        public FileSystemEmergencyCache()
        {
            _filePath = new Lazy<string>(ResolveFilePath);
        }

        public void Save(IDictionary<string, string> settings)
        {
            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(_filePath.Value, json);
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
                var json = File.ReadAllText(_filePath.Value);
                var deserializedSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                settings = new Dictionary<string, string>(deserializedSettings, StringComparer.OrdinalIgnoreCase);
                return true;
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
