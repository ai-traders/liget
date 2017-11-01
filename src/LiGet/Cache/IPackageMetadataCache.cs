using System;

namespace LiGet.Cache
{
    public interface IPackageMetadataCache
    {
        /// <summary>
        /// Date at which cache store was created.
        /// Implies that no entry is older than this.
        /// </summary>
        /// <returns></returns>
        DateTimeOffset CacheCreationDate { get; }

        byte[] TryGet(string package);

        /// <summary>
        /// Inserts package metadata to cache.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="timestamp">When data was accuired. Considered valid then.</param>
        void Insert(string package, DateTimeOffset timestamp, byte[] content);

        void InvalidateIfOlder(string package, DateTimeOffset timestamp);
    }
}