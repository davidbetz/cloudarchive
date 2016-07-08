using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudArchive
{
    public interface IAssetReadProvider
    {
        Task<AssetStatusCode> Check(string area, string selector, string hash);
        Task<Tuple<string, Stream>> Stream(string area, string selector);
        Task<byte[]> Read(string area, string selector);
        Task<string> GetUrl(string area, string selector);
    }
}