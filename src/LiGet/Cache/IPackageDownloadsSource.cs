using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiGet.Cache
{
    public interface IPackageDownloadsSource
    {
        Task<Dictionary<string, Dictionary<string, long>>> GetPackageDownloadsAsync();
    }
}
