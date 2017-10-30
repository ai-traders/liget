using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using LiGet;
using LiGet.NuGet.Server.Infrastructure;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

namespace NuGet
{
    /// <summary>
    /// Represents a NuGet v3 style expanded repository. Packages in this repository are 
    /// stored in the format {id}/{version}/{unzipped-contents}
    /// </summary>
    public class ExpandedPackageRepository
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ExpandedPackageRepository));
        private static readonly ILogger _logAdapter = new Log4NetLoggerAdapter(_log);

        private readonly IFileSystem _fileSystem;
        private readonly CryptoHashProvider _hashProvider;

        private readonly VersionFolderPathResolver _pathResolver;

        public ExpandedPackageRepository(IServerPackageRepositoryConfig serverConfig) 
            : this(new PhysicalFileSystem(serverConfig.RootPath)) {
                
            }

        public ExpandedPackageRepository(IFileSystem fileSystem)
            : this(fileSystem, new CryptoHashProvider())
        {
            _pathResolver = new VersionFolderPathResolver(fileSystem.Root);
        }


        public ExpandedPackageRepository(
            IFileSystem fileSystem,
            CryptoHashProvider hashProvider)
        {
            _fileSystem = fileSystem;
            _hashProvider = hashProvider;
        }

        public string Source
        {
            get { return _fileSystem.Root; }
        }

        public bool SupportsPrereleasePackages
        {
            get { return true; }
        }

        public LocalPackageInfo AddPackage(LocalPackageInfo package)
        {
            var ct = CancellationToken.None;
            var destPackageDir = _pathResolver.GetPackageDirectory(package.Identity.Id, package.Identity.Version);
            var destPackagePath = _pathResolver.GetPackageFilePath(package.Identity.Id, package.Identity.Version);
            _fileSystem.MakeDirectoryForFile(destPackagePath);
            using(var downloader = new LocalPackageArchiveDownloader(package.Path, package.Identity, _logAdapter)) {
                downloader.CopyNupkgFileToAsync(destPackagePath, ct).Wait();
                var hashFilePath = Path.ChangeExtension(destPackagePath, PackagingCoreConstants.HashFileExtension);
                var hash = downloader.GetPackageHashAsync("SHA512", ct).Result;
                var hashBytes = Encoding.UTF8.GetBytes(hash);
                _fileSystem.AddFile(hashFilePath, hashFileStream => { hashFileStream.Write(hashBytes, 0, hashBytes.Length); });

                var nuspecPath = _pathResolver.GetManifestFilePath(package.Identity.Id, package.Identity.Version);
                using(var nuspecStream = File.OpenWrite(nuspecPath)) {
                    using(var fs = downloader.CoreReader.GetNuspecAsync(ct).Result) {
                        fs.CopyTo(nuspecStream);                
                    }
                }
                _log.DebugFormat("Saved manifest {0}",nuspecPath);
                Lazy<NuspecReader> nuspecReader = new Lazy<NuspecReader>(() => new NuspecReader(nuspecPath));
                var packageReader = new Func<PackageReaderBase>(() => new PackageArchiveReader(File.OpenRead(destPackagePath)));
                return new LocalPackageInfo(package.Identity,destPackagePath,package.LastWriteTimeUtc,nuspecReader,packageReader);
            }
        }

        public LocalPackageInfo AddPackage(Stream packageStream, bool allowOverwrite)
        {
            string tempFilePath = Path.Combine(this.Source, Path.GetRandomFileName() + PackagingCoreConstants.PackageDownloadMarkerFileExtension);
            using(var dest = File.OpenWrite(tempFilePath)) {
                packageStream.CopyTo(dest);
            }
            PackageIdentity identity;
            string destPackagePath;
            using(var archive = new PackageArchiveReader(File.OpenRead(tempFilePath))){
                var id = archive.NuspecReader.GetId();
                var version = archive.NuspecReader.GetVersion();
                identity = new PackageIdentity(id, version);
                destPackagePath = _pathResolver.GetPackageFilePath(id, version);
                _fileSystem.MakeDirectoryForFile(destPackagePath);
            }
            var hashFilePath = Path.ChangeExtension(destPackagePath, PackagingCoreConstants.HashFileExtension);
            if(!allowOverwrite && File.Exists(hashFilePath))
                throw new PackageDuplicateException($"Package {identity} already exists");
            if(File.Exists(destPackagePath))
                File.Delete(destPackagePath);
            File.Move(tempFilePath, destPackagePath);
            var ct = CancellationToken.None;
            using(var downloader = new LocalPackageArchiveDownloader(destPackagePath, identity, _logAdapter)) {
                var hash = downloader.GetPackageHashAsync("SHA512", ct).Result;
                var hashBytes = Encoding.UTF8.GetBytes(hash);
                File.WriteAllBytes(hashFilePath, hashBytes);

                var nuspecPath = _pathResolver.GetManifestFilePath(identity.Id, identity.Version);
                using(var nuspecStream = File.OpenWrite(nuspecPath)) {
                    using(var stream = downloader.CoreReader.GetNuspecAsync(ct).Result){
                        stream.CopyTo(nuspecStream);                
                    }
                }
                _log.DebugFormat("Saved manifest {0}",nuspecPath);
                Lazy<NuspecReader> nuspecReader = new Lazy<NuspecReader>(() => new NuspecReader(nuspecPath));
                var packageReader = new Func<PackageReaderBase>(() => new PackageArchiveReader(File.OpenRead(destPackagePath)));
                return new LocalPackageInfo(identity,destPackagePath,DateTime.UtcNow,nuspecReader,packageReader);
            }
        }

        public void RemovePackage(PackageIdentity package)
        {
            if (Exists(package.Id, package.Version))
            {
                var packagePath = GetPackageRoot(package.Id, package.Version);
                _fileSystem.DeleteDirectory(packagePath, recursive: true);
            }
        }

        public bool Exists(string packageId, NuGetVersion version)
        {
            var hashFilePath = _pathResolver.GetHashPath(packageId, version);
            return _fileSystem.FileExists(hashFilePath);
        }

        public LocalPackageInfo FindPackage(string packageId, NuGetVersion version)
        {
            if (!Exists(packageId, version))
            {
                return null;
            }

            //TODO resign from IFileSystem
            var physicalFs = (PhysicalFileSystem)_fileSystem;
            string root = physicalFs.Root;

            return LocalFolderUtility.GetPackageV3(root, packageId, version, _logAdapter);
        }

        public IEnumerable<LocalPackageInfo> FindPackagesById(string packageId)
        {
            //TODO resign from IFileSystem
            var physicalFs = (PhysicalFileSystem)_fileSystem;
            string root = physicalFs.Root;
            return LocalFolderUtility.GetPackagesV3(root, packageId.ToLowerInvariant(), _logAdapter);
        }

        public IEnumerable<LocalPackageInfo> GetPackages()
        {
            var physicalFs = (PhysicalFileSystem)_fileSystem;
            string root = physicalFs.Root;
            return LocalFolderUtility.GetPackagesV3(root, _logAdapter); 
        }

        private static string GetPackageRoot(string packageId, SemanticVersion version)
        {
            return Path.Combine(packageId, version.ToNormalizedString());
        }

        internal Func<Stream> GetStream(PackageIdentity packageIdentity)
        {
            var path = _pathResolver.GetPackageFilePath(packageIdentity.Id, packageIdentity.Version);
            if(!File.Exists(path))
                return null;

            return () => File.OpenRead(path);
        }
    }
}