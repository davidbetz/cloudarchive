using System;
using System.IO;

namespace CloudArchive.Configuration
{
    public static class StorageConfig
    {
        public static Storage Get(string area)
        {
            var areaConfig = Config.LoadArea(area);

            var storage = areaConfig.Storage;
            if (String.IsNullOrEmpty(storage.Name))
            {
                throw new NullReferenceException($"Storage name required ({areaConfig.Name}).");
            }
            if (String.IsNullOrEmpty(storage.Provider))
            {
                throw new NullReferenceException($"Storage provider required ({areaConfig.Name}).");
            }
            if (String.IsNullOrEmpty(storage.Key1))
            {
                throw new NullReferenceException($"Storage key1 required ({areaConfig.Name}).");
            }
            if (String.IsNullOrEmpty(storage.Key2))
            {
                throw new NullReferenceException($"Storage key2 required ({areaConfig.Name}).");
            }
            if (storage.Key1.StartsWith("(") && storage.Key1.EndsWith(")"))
            {
                var key1FileName = storage.Key1.Substring(1, storage.Key1.Length - 2);
                storage.Key1 = File.ReadAllText(key1FileName);
            }
            if (storage.Key2.StartsWith("(") && storage.Key2.EndsWith(")"))
            {
                var key2FileName = storage.Key2.Substring(1, storage.Key2.Length - 2);
                storage.Key2 = File.ReadAllText(key2FileName);
            }
            return storage;
        }
    }
}