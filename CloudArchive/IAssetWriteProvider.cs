using System.Threading.Tasks;

namespace CloudArchive
{
    public interface IAssetWriteProvider
    {
        Task EnsureAccess(string area);
        Task<string> Update(string area, string selector, string contentType, byte[] binary);
    }
}