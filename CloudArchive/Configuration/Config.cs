using Nalarium;
using Nalarium.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CloudArchive.Configuration
{
    public static class Config
    {
        private static readonly object Lock = new object();
        private static readonly Dictionary<string, ConfigContent> Content = new Dictionary<string, ConfigContent>();

        public static Area LoadArea(string area)
        {
            if (string.IsNullOrEmpty(area))
            {
                return null;
            }
            var config = Load(GetConfigString());
            var areaConfig = config.Areas.FirstOrDefault(p => p.Name.Equals(area, StringComparison.InvariantCultureIgnoreCase));
            if (!String.IsNullOrEmpty(areaConfig?.StorageName))
            {
                areaConfig.Storage = LoadStorage(areaConfig.StorageName);
            }
            return areaConfig;
        }

        public static Storage LoadStorage(string storage)
        {
            if (string.IsNullOrEmpty(storage))
            {
                return null;
            }
            var config = Load(GetConfigString());
            return config.StorageList.FirstOrDefault(p => p.Name.Equals(storage, StringComparison.InvariantCultureIgnoreCase));
        }

        public static ConfigContent Load(bool reload = false)
        {
            return Load(GetConfigString(), reload);
        }

        public static ConfigContent Load(ILocationResolver resolver, bool reload = false)
        {
            return Load(GetConfigString(resolver), reload);
        }

        public static ConfigContent Load(string filename, bool reload = false)
        {
            lock (Lock)
            {
                if (Content.ContainsKey(filename))
                {
                    if (!reload)
                    {
                        return Content[filename];
                    }
                    Content.Remove(filename);
                }
                if (!File.Exists(filename))
                {
                    throw new FileNotFoundException(filename);
                }
                var reader = new StringReader(File.ReadAllText(filename));
                var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());
                var content = deserializer.Deserialize<ConfigContent>(reader);
                Content[filename] = content;

                foreach (var storage in content.StorageList)
                {
                    storage.Provider = Url.Clean(storage.Provider);
                    storage.Name = Url.Clean(storage.Name);
                    storage.Key1 = Url.Clean(storage.Key1);
                    storage.Key2 = Url.Clean(storage.Key2);
                }

                foreach (var area in content.Areas)
                {
                    area.Name = area.Name.ToLower();
                    area.Container = area.Container.ToLower();
                    if (string.IsNullOrEmpty(area.Container))
                    {
                        throw new NullReferenceException($"Area container required ({area.Name}).");
                    }
                    if (string.IsNullOrEmpty(area.Folder))
                    {
                        throw new NullReferenceException($"Area folder required ({area.Name}).");
                    }
                    if (string.IsNullOrEmpty(area.StorageName))
                    {
                        throw new NullReferenceException($"Area storage required ({area.Name}).");
                    }
                    var storage = content.StorageList.FirstOrDefault(p => p.Name.Equals(area.StorageName, StringComparison.InvariantCultureIgnoreCase));
                    if (storage == null)
                    {
                        throw new NullReferenceException($"Invalid storage group ({area.StorageName}).");
                    }
                    area.Storage = storage;
                    var assetFileTypeData = area.FileTypes;
                    foreach (var item in assetFileTypeData)
                    {
                        if (item.Extension.StartsWith("."))
                        {
                            item.Extension = item.Extension.Substring(1, item.Extension.Length - 1);
                        }
                    }
                }
                return content;
            }
        }

        public static void ReleaseConfig()
        {
            ReleaseConfig(GetConfigString());
        }

        public static void ReleaseConfig(ILocationResolver resolver)
        {
            ReleaseConfig(GetConfigString(resolver));
        }

        public static void ReleaseConfig(string filename)
        {
            lock (Lock)
            {
                if (Content.ContainsKey(filename))
                {
                    Content.Remove(filename);
                }
            }
        }

        private static string GetConfigString()
        {
            ILocationResolver resolver = null;
            var configLocation = ConfigAccessor.ApplicationSettings("CloudArchive:ConfigLocation").ToLower();
            if (configLocation.Contains("[entryassembly]"))
            {
                resolver = new EntryAssemblyLocationResolver();
            }
            else if (configLocation.Contains("\\"))
            {
                resolver = new LiteralLocationResolver();
            }
            return GetConfigString(resolver);
        }

        private static string GetConfigString(ILocationResolver resolver)
        {
            if (resolver == null)
            {
                throw new NullReferenceException(nameof(resolver));
            }
            return resolver.Resolve(ConfigAccessor.ApplicationSettings("CloudArchive:ConfigLocation").ToLower());
        }

        public static string WriteConfig()
        {
            var sb = new StringBuilder();
            using (var sr = new StringWriter(sb))
            {
                var serializer = new Serializer(namingConvention: new CamelCaseNamingConvention());
                serializer.Serialize(sr, Load());
            }
            return sb.ToString();
        }
    }
}