using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudArchive
{
    public class AssetProvider : IAssetReadProvider, IAssetWriteProvider
    {
        private readonly IAssetReadProvider _assetReadProvider;
        private readonly IAssetWriteProvider _assetWriteProvider;

        public AssetProvider(IAssetReadProvider readProvider, IAssetWriteProvider writeProvider)
        {
            _assetReadProvider = readProvider;
            _assetWriteProvider = writeProvider;
        }

        public async Task<AssetStatusCode> Check(string area, string selector, string hash)
        {
            return await _assetReadProvider.Check(area, selector, hash);
        }

        public async Task<Tuple<string, Stream>> Stream(string area, string selector)
        {
            return await _assetReadProvider.Stream(area, selector);
        }

        public async Task<byte[]> Read(string area, string selector)
        {
            return await _assetReadProvider.Read(area, selector);
        }

        public async Task<string> GetUrl(string area, string selector)
        {
            return await _assetReadProvider.GetUrl(area, selector);
        }

        public async Task EnsureAccess(string area)
        {
            await _assetWriteProvider.EnsureAccess(area);
        }

        public async Task<string> Update(string area, string selector, string contentType, byte[] binary)
        {
            return await _assetWriteProvider.Update(area, selector, contentType, binary);
        }
    }
}