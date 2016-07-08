using CloudArchive.Azure;
using CloudArchive.Configuration;
using CloudArchive.S3;
using System;

namespace CloudArchive.Client
{
    public static class AssetProviderBuilder
    {
        public static AssetProvider Create(string area)
        {
            return Create(Config.LoadArea(area));
        }

        public static AssetProvider Create(Area areaConfig)
        {
            IAssetReadProvider readProvider;
            IAssetWriteProvider writeProvider;

            var assetStorageProvider = areaConfig?.Storage?.Provider?.ToLower();

            if (assetStorageProvider == "azure")
            {
                readProvider = new AzureAssetProvider();
                writeProvider = readProvider as IAssetWriteProvider;
            }
            else if (assetStorageProvider == "s3")
            {
                readProvider = new S3AssetProvider();
                writeProvider = readProvider as IAssetWriteProvider;
            }
            else
            {
                throw new InvalidOperationException("no");
            }
            return new AssetProvider(readProvider, writeProvider);
        }
    }
}
