using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core.Entities;
using BaGet.Core.Extensions;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace BaGet.Core.Services
{
    public class FilePackageStorageService : IPackageStorageService
    {
        // See: https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/Stream.cs#L35
        private const int DefaultCopyBufferSize = 81920;

        private readonly string _storePath;

        public FilePackageStorageService(string storePath)
        {
            _storePath = storePath ?? throw new ArgumentNullException(nameof(storePath));
        }

        public string RootPath { get { return _storePath; } }

         public async Task SavePackageContentAsync(
            Package package,
            Stream packageStream,
            Stream nuspecStream,
            Stream readmeStream,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            package = package ?? throw new ArgumentNullException(nameof(package));
            packageStream = packageStream ?? throw new ArgumentNullException(nameof(packageStream));
            nuspecStream = nuspecStream ?? throw new ArgumentNullException(nameof(nuspecStream));

            var lowercasedId = package.Id.ToLowerInvariant();
            var lowercasedNormalizedVersion = package.VersionString.ToLowerInvariant();

            EnsurePathExists(lowercasedId, lowercasedNormalizedVersion);

            await SaveFileStreamAsync(
                lowercasedId,
                lowercasedNormalizedVersion,
                PackagePath,
                packageStream,
                cancellationToken);

            await SaveFileStreamAsync(
                lowercasedId,
                lowercasedNormalizedVersion,
                NuspecPath,
                nuspecStream,
                cancellationToken);

            if (readmeStream != null)
            {
                await SaveFileStreamAsync(
                    lowercasedId,
                    lowercasedNormalizedVersion,
                    ReadmePath,
                    readmeStream,
                    cancellationToken);
            }
        }

        public Task<Stream> GetPackageStreamAsync(PackageIdentity id)
        {
            var packageStream = GetFileStream(id, PackagePath);

            return Task.FromResult(packageStream);
        }

        public Task<Stream> GetNuspecStreamAsync(PackageIdentity id)
        {
            var nuspecStream = GetFileStream(id, NuspecPath);

            return Task.FromResult(nuspecStream);
        }

        public Task<Stream> GetReadmeStreamAsync(PackageIdentity id)
        {
            var readmeStream = GetFileStream(id, ReadmePath);

            return Task.FromResult(readmeStream);
        }

        private async Task SaveFileStreamAsync(
            string lowercasedId,
            string lowercasedNormalizedVersion,
            Func<string, string, string> pathFunc,
            Stream content,
            CancellationToken cancellationToken)
        {
            var path = pathFunc(lowercasedId, lowercasedNormalizedVersion);

            // TODO: Uploads should be idempotent. This should fail if and only if the blob
            // already exists but has different content.
            using (var fileStream = File.Open(path, FileMode.CreateNew))
            {
                await content.CopyToAsync(fileStream, DefaultCopyBufferSize, cancellationToken);
            }
        }

        public Task DeleteAsync(PackageIdentity id)
        {
            var lowercasedId = id.Id.ToLowerInvariant();
            var lowercasedNormalizedVersion = id.Version.ToNormalizedString().ToLowerInvariant();

            var packagePath = PackagePath(lowercasedId, lowercasedNormalizedVersion);
            var nuspecPath = NuspecPath(lowercasedId, lowercasedNormalizedVersion);
            var readmePath = ReadmePath(lowercasedId, lowercasedNormalizedVersion);

            try
            {
                File.Delete(packagePath);
                File.Delete(nuspecPath);
                File.Delete(readmePath);
            }
            catch (DirectoryNotFoundException)
            {
            }

            return Task.CompletedTask;
        }

        private Stream GetFileStream(PackageIdentity id, Func<string, string, string> pathFunc)
        {
            var versionString = id.Version.ToNormalizedString().ToLowerInvariant();
            var path = pathFunc(id.Id.ToLowerInvariant(), versionString);

            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        private string PackagePath(string lowercasedId, string lowercasedNormalizedVersion)
        {
            return Path.Combine(
                _storePath,
                lowercasedId,
                lowercasedNormalizedVersion,
                $"{lowercasedId}.{lowercasedNormalizedVersion}.nupkg");
        }

        private string NuspecPath(string lowercasedId, string lowercasedNormalizedVersion)
        {
            return Path.Combine(
                _storePath,
                lowercasedId,
                lowercasedNormalizedVersion,
                $"{lowercasedId}.nuspec");
        }

        private string ReadmePath(string lowercasedId, string lowercasedNormalizedVersion)
        {
            return Path.Combine(
                _storePath,
                lowercasedId,
                lowercasedNormalizedVersion,
                "readme");
        }

        private void EnsurePathExists(string lowercasedId, string lowercasedNormalizedVersion)
        {
            var path = Path.Combine(_storePath, lowercasedId, lowercasedNormalizedVersion);

            Directory.CreateDirectory(path);
        }
    }
}
