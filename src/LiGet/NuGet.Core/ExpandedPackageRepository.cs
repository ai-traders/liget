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
            var downloader = new LocalPackageArchiveDownloader(package.Path, package.Identity, _logAdapter);
            downloader.CopyNupkgFileToAsync(destPackagePath, ct).Wait();
            var hashFilePath = Path.ChangeExtension(destPackagePath, PackagingCoreConstants.HashFileExtension);
            var hash = downloader.GetPackageHashAsync("SHA512", ct).Result;
            var hashBytes = Encoding.UTF8.GetBytes(hash);
            _fileSystem.AddFile(hashFilePath, hashFileStream => { hashFileStream.Write(hashBytes, 0, hashBytes.Length); });

            var nuspecPath = _pathResolver.GetManifestFilePath(package.Identity.Id, package.Identity.Version);
            using(var nuspecStream = File.OpenWrite(nuspecPath)) {
                downloader.CoreReader.GetNuspecAsync(ct).Result.CopyTo(nuspecStream);                
            }
            _log.DebugFormat("Saved manifest {0}",nuspecPath);
            Lazy<NuspecReader> nuspecReader = new Lazy<NuspecReader>(() => new NuspecReader(nuspecPath));
            var packageReader = new Func<PackageReaderBase>(() => new PackageArchiveReader(File.OpenRead(destPackagePath)));
            return new LocalPackageInfo(package.Identity,destPackagePath,package.LastWriteTimeUtc,nuspecReader,packageReader);

            // 
            // var nupkgPath = _pathResolver.GetPackageFilePath(package.Identity.Id, package.Identity.Version);
            // // was
            // //var nupkgPath = Path.Combine(packagePath, package.Id + "." + package.Version.ToNormalizedString() + Constants.PackageExtension);

            // _fileSystem.MakeDirectoryForFile(nupkgPath);
            // using (var stream = package.GetReader())
            // {                
            //     stream.CopyNupkgAsync(nupkgPath, CancellationToken.None).Wait();
            //     //was _fileSystem.AddFile(nupkgPath, stream);
            // }

            // var hashBytes = Encoding.UTF8.GetBytes(package.GetHash(_hashProvider));
            // var hashFilePath = Path.ChangeExtension(nupkgPath, Constants.HashFileExtension);
            // _fileSystem.AddFile(hashFilePath, hashFileStream => { hashFileStream.Write(hashBytes, 0, hashBytes.Length); });

            // using (var stream = package.GetStream())
            // {
            //     using (var manifestStream = PackageHelper.GetManifestStream(stream))
            //     {
            //         var manifestPath = Path.Combine(packagePath, package.Id + Constants.ManifestExtension);
            //         _fileSystem.AddFile(manifestPath, manifestStream);
            //     }
            // }
        }

        public void RemovePackage(PackageIdentity package)
        {
            if (Exists(package.Id, package.Version))
            {
                var packagePath = GetPackageRoot(package.Id, package.Version);
                _fileSystem.DeleteDirectory(packagePath, recursive: true);
            }
        }

        public bool Exists(string packageId, SemanticVersion version)
        {
            throw new NotImplementedException();

            // var hashFilePath = Path.ChangeExtension(GetPackagePath(packageId, version), Constants.HashFileExtension);
            // return _fileSystem.FileExists(hashFilePath);
        }

        public LocalPackageInfo FindPackage(string packageId, SemanticVersion version)
        {
            if (!Exists(packageId, version))
            {
                return null;
            }

            return GetPackageInternal(packageId, version);
        }

        public IEnumerable<LocalPackageInfo> FindPackagesById(string packageId)
        {
            //TODO resign from IFileSystem
            var physicalFs = (PhysicalFileSystem)_fileSystem;
            string root = physicalFs.Root;
            return LocalFolderUtility.GetPackagesV3(root, packageId, _logAdapter);
            // foreach (var versionDirectory in _fileSystem.GetDirectoriesSafe(packageId))
            // {
            //     var versionDirectoryName = Path.GetFileName(versionDirectory);
            //     SemanticVersion version;
            //     if (SemanticVersion.TryParse(versionDirectoryName, out version) &&
            //         Exists(packageId, version))
            //     {
            //         IPackage package = null;

            //         try
            //         {
            //             package = GetPackageInternal(packageId, version);
            //         }
            //         catch (XmlException ex)
            //         {
            //             Logger.Log(MessageLevel.Warning, ex.Message);
            //             Logger.Log(
            //                 MessageLevel.Warning, 
            //                 NuGetResources.Manifest_NotFound, 
            //                 string.Format("{0}/{1}", packageId, version));
            //             continue;
            //         }
            //         catch (IOException ex)
            //         {
            //             Logger.Log(MessageLevel.Warning, ex.Message);
            //             Logger.Log(
            //                 MessageLevel.Warning, 
            //                 NuGetResources.Manifest_NotFound, 
            //                 string.Format("{0}/{1}", packageId, version));
            //             continue;
            //         }

            //         yield return package;
            //     }
            // }
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

        private LocalPackageInfo GetPackageInternal(string packageId, SemanticVersion version)
        {
            // var packagePath = GetPackagePath(packageId, version);
            // var manifestPath = Path.Combine(GetPackageRoot(packageId, version), packageId + Constants.ManifestExtension);
            // return new ZipPackage(() => _fileSystem.OpenFile(packagePath), () => _fileSystem.OpenFile(manifestPath));
            throw new NotImplementedException();
        }

        private string GetPackagePath(string packageId, SemanticVersion version)
        {
            throw new NotImplementedException();
            //return _pathResolver.GetPackageFilePath(packageId, new NuGetVersion(version));
            // return Path.Combine(
            //     GetPackageRoot(packageId, version),
            //     packageId + "." + version.ToNormalizedString() + Constants.PackageExtension);
        }
    }
}