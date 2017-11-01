using DBreeze;

namespace LiGet.Cache.DBreeze
{
    public interface IDBreezeConfig
    {
        string RootCacheDirectory { get; }

        DBreezeConfiguration.eStorage StorageBackend { get; }
    }
}