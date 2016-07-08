using CloudArchive.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Nalarium;
using Nalarium.Cryptography;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace CloudArchive.Azure
{
    public class AzureAssetProvider :
        IAssetReadProvider,
        IAssetWriteProvider
    {
        [Reference("http://blogs.msdn.com/b/windowsazurestorage/archive/2011/02/18/windows-azure-blob-md5-overview.aspx")]
        public async Task<AssetStatusCode> Check(string area, string selector, string hash)
        {
            if (string.IsNullOrEmpty(area))
            {
                return AssetStatusCode.Error;
            }

            var areaConfig = Config.LoadArea(area);
            var storageConfig = StorageConfig.Get(area);
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageConfig.Name};AccountKey={storageConfig.Key1}";

            area = area.ToLower(CultureInfo.CurrentCulture);

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(areaConfig.Container);
            await container.CreateIfNotExistsAsync();

            var blockBlob = container.GetBlockBlobReference(selector);
            if (!blockBlob.Exists())
            {
                return AssetStatusCode.DoesNotExist;
            }

            var storedHash = blockBlob.Properties.ContentMD5;

            return storedHash.Equals(hash) ? AssetStatusCode.Same : AssetStatusCode.Different;
        }

        public async Task<Tuple<string, Stream>> Stream(string area, string selector)
        {
            if (string.IsNullOrEmpty(area))
            {
                return null;
            }

            var areaConfig = Config.LoadArea(area);
            var storageConfig = StorageConfig.Get(area);
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageConfig.Name};AccountKey={storageConfig.Key1}";

            area = area.ToLower(CultureInfo.CurrentCulture);

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(areaConfig.Container);
            await container.CreateIfNotExistsAsync();

            var blockBlob = container.GetBlockBlobReference(selector);
            using (var stream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(stream);
                return await Task.FromResult(new Tuple<string, Stream>(blockBlob.Properties.ContentType, stream));
            }
        }

        public async Task<byte[]> Read(string area, string selector)
        {
            if (string.IsNullOrEmpty(area))
            {
                return null;
            }

            var areaConfig = Config.LoadArea(area);
            var storageConfig = StorageConfig.Get(area);
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageConfig.Name};AccountKey={storageConfig.Key1}";

            area = area.ToLower(CultureInfo.CurrentCulture);

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(areaConfig.Container);
            await container.CreateIfNotExistsAsync();

            var blockBlob = container.GetBlockBlobReference(selector);
            using (var stream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(stream);
                return stream.ToArray();
            }
        }

        public async Task<string> GetUrl(string area, string selector)
        {
            if (string.IsNullOrEmpty(area))
            {
                return null;
            }
            if (string.IsNullOrEmpty(selector))
            {
                return null;
            }

            var areaConfig = Config.LoadArea(area);
            var storageConfig = StorageConfig.Get(area);
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageConfig.Name};AccountKey={storageConfig.Key1}";

            area = area.ToLower(CultureInfo.CurrentCulture);
            selector = selector.ToLower(CultureInfo.CurrentCulture);

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(areaConfig.Container);
            await container.CreateIfNotExistsAsync();

            var blockBlob = container.GetBlockBlobReference(selector);

            return blockBlob.Uri.AbsoluteUri;
        }

        public async Task EnsureAccess(string area)
        {
            if (string.IsNullOrEmpty(area))
            {
                return;
            }

            var areaConfig = Config.LoadArea(area);
            var storageConfig = StorageConfig.Get(area);
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageConfig.Name};AccountKey={storageConfig.Key1}";

            area = area.ToLower(CultureInfo.CurrentCulture);

            var blobStorageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = blobStorageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(areaConfig.Container);
            await container.CreateIfNotExistsAsync();

            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            InitializeCors(blobClient);
        }

        public async Task<string> Update(string area, string selector, string contentType, byte[] buffer)
        {
            if (string.IsNullOrEmpty(area))
            {
                return null;
            }

            var areaConfig = Config.LoadArea(area);
            var storageConfig = StorageConfig.Get(area);
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageConfig.Name};AccountKey={storageConfig.Key1}";

            area = area.ToLower(CultureInfo.CurrentCulture);

            var blobStorageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = blobStorageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference(areaConfig.Container);
            await container.CreateIfNotExistsAsync();

            var hash = QuickHash.Hash(buffer, HashMethod.SHA256);

            var blockBlob = container.GetBlockBlobReference(selector);
            using (var stream = new MemoryStream(buffer))
            {
                await blockBlob.UploadFromStreamAsync(stream);
                if (!string.IsNullOrEmpty(contentType))
                {
                    blockBlob.Properties.ContentType = contentType;
                }
                blockBlob.Properties.ContentMD5 = hash;
                await blockBlob.SetPropertiesAsync();
            }

            return hash;
        }

        private static void InitializeCors(CloudBlobClient blobClient)
        {
            var blobServiceProperties = blobClient.GetServiceProperties();

            ConfigureCors(blobServiceProperties);

            blobClient.SetServiceProperties(blobServiceProperties);
        }

        private static void ConfigureCors(ServiceProperties serviceProperties)
        {
            serviceProperties.Cors = new CorsProperties();
            serviceProperties.Cors.CorsRules.Add(new CorsRule
            {
                AllowedHeaders = new List<string> { "*" },
                AllowedMethods = CorsHttpMethods.Get,
                AllowedOrigins = new List<string> { "*" },
                ExposedHeaders = new List<string> { "*" },
                MaxAgeInSeconds = 1800
            });
        }

        public string CleanSelector(string selector)
        {
            return selector.Replace("/", "_").Replace(".", "=");
        }
    }
}
