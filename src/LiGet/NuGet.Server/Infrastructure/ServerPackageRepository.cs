// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using LiGet.Models;
using NuGet;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

namespace LiGet.NuGet.Server.Infrastructure
{
    /// <summary>
    /// ServerPackageRepository represents a folder of nupkgs on disk. All packages are cached during the first request in order
    /// to correctly determine attributes such as IsAbsoluteLatestVersion. Adding, removing, or making changes to packages on disk 
    /// will clear the cache.
    /// </summary>
    public class ServerPackageRepository : IPackageService, IDisposable
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(ServerPackageRepository));
        private static readonly ILogger _logAdapter = new Log4NetLoggerAdapter(_logger);

        private readonly object _syncLock = new object();
        private readonly IServerPackageRepositoryConfig _config;
        private readonly IFileSystem _fileSystem;

        // from nuget core docs:
        // Represents a NuGet v3 style expanded repository. Packages in this repository are 
        // stored in the format {id}/{version}/{unzipped-contents}
        private readonly ExpandedPackageRepository _expandedPackageRepository;
        private readonly IServerPackageStore _serverPackageStore;

        private readonly bool _runBackgroundTasks;
        private FileSystemWatcher _fileSystemWatcher;
        private bool _isFileSystemWatcherSuppressed;
        private bool _needsRebuild = true;

        private Timer _persistenceTimer;
        private Timer _rebuildTimer;

        private PackageSaveModes _packageSave = PackageSaveModes.Nupkg;
        
        public ServerPackageRepository(
            ExpandedPackageRepository innerRepository,
            IServerPackageRepositoryConfig serverConfig)
        {
            if (innerRepository == null)
            {
                throw new ArgumentNullException("innerRepository");
            }
            _config = serverConfig;

             new DirectoryInfo(serverConfig.RootPath).Create();
            _fileSystem = new PhysicalFileSystem(serverConfig.RootPath);
            _runBackgroundTasks = serverConfig.RunBackgroundTasks;
            _expandedPackageRepository = innerRepository;

            _serverPackageStore = new ServerPackageStore(_fileSystem, Environment.MachineName.ToLowerInvariant() + ".cache.bin");
            _logger.InfoFormat("Initialized server package repository at {0}",serverConfig.RootPath);
        }

        private void SetupBackgroundJobs()
        {
            if (!_runBackgroundTasks)
            {
                return;
            }

            _logger.Info( "Registering background jobs...");

            // Persist to package store at given interval (when dirty)
            _persistenceTimer = new Timer(state => 
                _serverPackageStore.PersistIfDirty(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            // Rebuild the package store in the background (every hour)
            _rebuildTimer = new Timer(state =>
                RebuildPackageStore(), null, TimeSpan.FromSeconds(15), TimeSpan.FromHours(1));
            
            _logger.Info("Finished registering background jobs.");
        }

        public IQueryable<IPackage> GetPackages()
        {
            return GetPackages(ClientCompatibility.Default);
        }

        public IQueryable<ServerPackage> GetPackages(ClientCompatibility compatibility)
        {
            var cache = CachedPackages.AsQueryable();

            if (!compatibility.AllowSemVer2)
            {
                cache = cache.Where(p => !p.IsSemVer2);
            }

            return cache;
        }

        public bool Exists(string packageId, NuGetVersion version)
        {
            return FindPackage(packageId, version) != null;
        }

        public ServerPackage FindPackage(string packageId, NuGetVersion version)
        {
            return FindPackagesById(packageId, ClientCompatibility.Max)
                .FirstOrDefault(p => p.Version.Equals(version));
        }

        public IEnumerable<ServerPackage> FindPackagesById(string packageId, ClientCompatibility compatibility)
        {
            return GetPackages(compatibility)
                .Where(p => StringComparer.OrdinalIgnoreCase.Compare(p.Id, packageId) == 0);
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            return FindPackagesById(packageId, ClientCompatibility.Default);
        }
        
        public IQueryable<ServerPackage> Search(
            string searchTerm,
            IEnumerable<string> targetFrameworks,
            bool allowPrereleaseVersions)
        {
            return Search(searchTerm, targetFrameworks, allowPrereleaseVersions, ClientCompatibility.Default);
        }

        public IQueryable<ServerPackage> Search(
            string searchTerm,
            IEnumerable<string> targetFrameworks,
            bool allowPrereleaseVersions,
            ClientCompatibility compatibility)
        {
            var packages = GetPackages(compatibility)
                .AsQueryable()
                .Find(searchTerm)
                .FilterByPrerelease(allowPrereleaseVersions);

            if (EnableDelisting)
            {
                packages = packages.Where(p => p.Listed);
            }

            if (EnableFrameworkFiltering && targetFrameworks.Any())
            {
                throw new NotImplementedException("filter target frameworks");
                // // Get the list of framework names
                // var frameworkNames = targetFrameworks
                //     .Select(frameworkName => NuGetFramework.Parse(frameworkName));

                // packages = packages
                //     .Where(package => frameworkNames
                //         .Any(frameworkName => VersionUtility
                //             .IsCompatible(frameworkName, package.GetSupportedFrameworks())));
            }

            return packages.AsQueryable();
        }

        // public IEnumerable<IPackage> GetUpdates(
        //     IEnumerable<IPackageName> packages,
        //     bool includePrerelease,
        //     bool includeAllVersions,
        //     IEnumerable<FrameworkName> targetFrameworks,
        //     IEnumerable<VersionRange> versionConstraints)
        // {
        //     return this.GetUpdatesCore(
        //         packages,
        //         includePrerelease,
        //         includeAllVersions,
        //         targetFrameworks,
        //         versionConstraints,
        //         ClientCompatibility.Default);
        // }

        public PackageSaveModes PackageSaveMode 
        {
            get { return _packageSave; }
            set
            {
                if (value == PackageSaveModes.None)
                {
                    throw new ArgumentException("PackageSave cannot be set to None");
                }

                _packageSave = value;
            }
        }

        public string Source
        {
            get
            {
                return _expandedPackageRepository.Source;
            }
        }

        public bool SupportsPrereleasePackages
        {
            get
            {
                return _expandedPackageRepository.SupportsPrereleasePackages;
            }
        }

        private void AddPackagesFromDropFolder()
        {
            _logger.Info("Start adding packages from drop folder.");

            using (LockAndSuppressFileSystemWatcher())
            {
                try
                {
                    var localPackages = LocalFolderUtility.GetPackagesV2(_fileSystem.Root, _logAdapter);

                    
                    var serverPackages = new HashSet<ServerPackage>(PackageEqualityComparer.IdAndVersion);

                    foreach (var package in localPackages)
                    {
                        try
                        {
                            //TODO ignoring symbols packages
                            // // Is it a symbols package?
                            // if (IgnoreSymbolsPackages && package.IsSymbolsPackage())
                            // {
                            //     var message = string.Format("Package {0} is a symbols package (it contains .pdb files and a /src folder). The server is configured to ignore symbols packages.", package);

                            //     _logger.Error(message);

                            //     continue;
                            // }

                            // Allow overwriting package? If not, skip this one.
                            if (!AllowOverrideExistingPackageOnPush && _expandedPackageRepository.Exists(package.Identity.Id, package.Identity.Version))
                            {
                                var message = string.Format("Package {0} already exists. The server is configured to not allow overwriting packages that already exist.", package);

                                _logger.Error(message);

                                continue;
                            }

                            // Copy to correct filesystem location
                            var added = _expandedPackageRepository.AddPackage(package);
                            _fileSystem.DeleteFile(package.Path);

                            // Mark for addition to metadata store
                            serverPackages.Add(CreateServerPackage(added, EnableDelisting));
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            // The file may be in use (still being copied) - ignore the error
                            _logger.ErrorFormat("Error adding package file {0} from drop folder: {1}", package.Path, ex.Message);
                        }
                        catch (IOException ex)
                        {
                            // The file may be in use (still being copied) - ignore the error
                            _logger.ErrorFormat("Error adding package file {0} from drop folder: {1}", package.Path, ex.Message);
                        }
                    }

                    // Add packages to metadata store in bulk
                    _serverPackageStore.StoreRange(serverPackages);
                    _serverPackageStore.PersistIfDirty();

                    _logger.Info("Finished adding packages from drop folder.");
                }
                finally
                {
                    //OptimizedZipPackage.PurgeCache();
                }
            }
        }

        /// <summary>
        /// Add a file to the repository.
        /// </summary>
        public void AddPackage(LocalPackageInfo package)
        {
            _logger.InfoFormat("Start adding package {0} {1}.", package.Identity.Id, package.Identity.Version);

            // if (IgnoreSymbolsPackages && package.IsSymbolsPackage())
            // {
            //     var message = string.Format("Package {0} is a symbols package (it contains .pdb files and a /src folder). The server is configured to ignore symbols packages.", package);

            //     _logger.Error(message);
            //     throw new InvalidOperationException(message);
            // }

            if (!AllowOverrideExistingPackageOnPush && Exists(package.Identity.Id, package.Identity.Version))
            {
                var message = string.Format("Package {0} already exists. The server is configured to not allow overwriting packages that already exist.", package.Identity);

                _logger.Error(message);
                throw new InvalidOperationException(message);
            }

            using (LockAndSuppressFileSystemWatcher())
            {
                // Copy to correct filesystem location
                package = _expandedPackageRepository.AddPackage(package);

                // Add to metadata store
                _serverPackageStore.Store(CreateServerPackage(package, EnableDelisting));

                _logger.InfoFormat("Finished adding package {0} {1}.", package.Identity.Id, package.Identity.Version);
            }
        }

        /// <summary>
        /// Unlist or delete a package.
        /// </summary>
        public void RemovePackage(PackageIdentity package)
        {
            if (package == null)
            {
                return;
            }

            using (LockAndSuppressFileSystemWatcher())
            {
                _logger.InfoFormat("Start removing package {0} {1}.", package.Id, package.Version);

                if (EnableDelisting)
                {
                    var physicalFileSystem = _fileSystem as PhysicalFileSystem;
                    if (physicalFileSystem != null)
                    {
                        var fileName = "";
                        throw new NotImplementedException("package remove");
                        // physicalFileSystem.GetFullPath(
                        //    GetPackageFileName(package.Id, package.Version));

                        if (File.Exists(fileName))
                        {
                            // Set "unlisted"
                            File.SetAttributes(fileName, File.GetAttributes(fileName) | FileAttributes.Hidden);

                            // Update metadata store
                            var serverPackage = FindPackage(package.Id, package.Version) as ServerPackage;
                            if (serverPackage != null)
                            {
                                serverPackage.Listed = false;
                                _serverPackageStore.Store(serverPackage);
                            }

                            // Note that delisted files can still be queried, therefore not deleting persisted hashes if present.
                            // Also, no need to flip hidden attribute on these since only the one from the nupkg is queried.

                            _logger.InfoFormat("Unlisted package {0} {1}.", package.Id, package.Version);
                        }
                        else
                        {
                            _logger.ErrorFormat(
                                "Error removing package {0} {1} - could not find package file {2}",
                                    package.Id, package.Version, fileName);
                        }
                    }
                }
                else
                {
                    // Remove from filesystem
                    _expandedPackageRepository.RemovePackage(package);

                    // Update metadata store
                    _serverPackageStore.Remove(package.Id, package.Version);

                    _logger.InfoFormat("Finished removing package {0} {1}.", package.Id, package.Version);
                }
            }
        }

        /// <summary>
        /// Remove a package from the respository.
        /// </summary>
        public void RemovePackage(string packageId, SemanticVersion version)
        {
            throw new NotImplementedException("remove package");
            // var package = FindPackage(packageId, version);

            // RemovePackage(package);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_persistenceTimer != null)
            {
                _persistenceTimer.Dispose();
            }

            if (_rebuildTimer != null)
            {
                _rebuildTimer.Dispose();
            }

            UnregisterFileSystemWatcher();
            _serverPackageStore.PersistIfDirty();
        }
        
        /// <summary>
        /// Package cache containing packages metadata. 
        /// This data is generated if it does not exist already.
        /// </summary>
        private IEnumerable<ServerPackage> CachedPackages
        {
            get
            {
                /*
                 * We rebuild the package storage under either of two conditions:
                 *
                 * 1. If the "needs rebuild" flag is set to true. This is initially the case when the repository is
                 *    instantiated, if a non-package drop file system event occurred (e.g. a file deletion), or if the
                 *    cache was manually cleared.
                 *
                 * 2. If the store has no packages at all. This is so we pick up initial packages as quickly as
                 *    possible.
                 */
                if (_needsRebuild || !_serverPackageStore.HasPackages())
                {
                    lock (_syncLock)
                    {
                        if (_needsRebuild || !_serverPackageStore.HasPackages())
                        {
                            RebuildPackageStore();
                        }
                    }
                }
                
                // First time we come here, attach the file system watcher
                if (_fileSystemWatcher == null)
                {
                    MonitorFileSystem(true);
                }

                // First time we come here, setup background jobs
                if (_persistenceTimer == null)
                {
                    SetupBackgroundJobs();
                }

                // Return packages
                return _serverPackageStore.GetAll();
            }
        }

        private void RebuildPackageStore()
        {
            lock (_syncLock)
            {
                _logger.Info("Start rebuilding package store...");

                // Build cache
                var packages = ReadPackagesFromDisk();
                _serverPackageStore.Clear();
                _serverPackageStore.StoreRange(packages);

                // Add packages from drop folder
                AddPackagesFromDropFolder();

                // Persist
                _serverPackageStore.PersistIfDirty();

                _needsRebuild = false;

                _logger.Info("Finished rebuilding package store.");
            }
        }

        /// <summary>
        /// ReadPackagesFromDisk loads all packages from disk and determines additional metadata such as the hash, IsAbsoluteLatestVersion, and IsLatestVersion.
        /// </summary>
        private HashSet<ServerPackage> ReadPackagesFromDisk()
        {
            _logger.Info("Start reading packages from disk...");

            using (LockAndSuppressFileSystemWatcher())
            {
                try
                {
                    var cachedPackages = new ConcurrentBag<ServerPackage>();

                    bool enableDelisting = EnableDelisting;

                    var packages = _expandedPackageRepository.GetPackages().ToList();

                    //Parallel.ForEach(packages, package =>
                    foreach(var package in packages)
                    {
                        // Create server package
                        var serverPackage = CreateServerPackage(package, enableDelisting);

                        // Add the package to the cache, it should not exist already
                        if (cachedPackages.Contains(serverPackage))
                        {
                            _logger.WarnFormat("Duplicate package found - {0} {1}", package.Identity.Id, package.Identity.Version);
                        }
                        else
                        {
                            cachedPackages.Add(serverPackage);
                        }
                    };

                    _logger.InfoFormat("Finished reading {0} packages from disk.", cachedPackages.Count);
                    return new HashSet<ServerPackage>(cachedPackages, PackageEqualityComparer.IdAndVersion);
                }
                catch (Exception ex)
                {
                    _logger.Error("Error while reading packages from disk", ex);
                    throw;
                }
            }
        }

        public ServerPackage CreateServerPackage(LocalPackageInfo package, bool enableDelisting)
        {
            // File names
            var packageFileName = package.Path;
            var hashFileName = Path.ChangeExtension(packageFileName, PackagingCoreConstants.HashFileExtension);

            // File system
            var physicalFileSystem = _fileSystem as PhysicalFileSystem;

            // Build package info
            var packageDerivedData = new PackageDerivedData();

            // Read package hash
            using (var reader = new StreamReader(_fileSystem.OpenFile(hashFileName)))
            {
                packageDerivedData.PackageHash = reader.ReadToEnd().Trim();
            }

            // Read package info
            var localPackage = package;
            if (physicalFileSystem != null)
            {
                // Read package info from file system
                var fileInfo = new FileInfo(packageFileName);
                packageDerivedData.PackageSize = fileInfo.Length;

                packageDerivedData.LastUpdated = _fileSystem.GetLastModified(packageFileName);
                packageDerivedData.Created = _fileSystem.GetCreated(packageFileName);
                packageDerivedData.Path = packageFileName;
                packageDerivedData.FullPath = fileInfo.FullName;

                // if (enableDelisting && localPackage != null)
                // {
                //     // hidden packages are considered delisted
                //     localPackage.Listed = !fileInfo.Attributes.HasFlag(FileAttributes.Hidden);
                // }
            }
            else
            {
                throw new NotSupportedException("Read package info from package (slower)");
                // FIXME Read package info from package (slower)
                // using (var stream = package.GetStream())
                // {
                //     packageDerivedData.PackageSize = stream.Length;
                // }

                // packageDerivedData.LastUpdated = DateTime.MinValue;
                // packageDerivedData.Created = DateTime.MinValue;
            }

            // TODO: frameworks?

            // Build entry
            var serverPackage = new ServerPackage(package.Nuspec, packageDerivedData);
            return serverPackage;
        }

        /// <summary>
        /// Sets the current cache to null so it will be regenerated next time.
        /// </summary>
        public void ClearCache()
        {
            using (LockAndSuppressFileSystemWatcher())
            {
                // OptimizedZipPackage.PurgeCache();

                _serverPackageStore.Clear();
                _serverPackageStore.Persist();
                _needsRebuild = true;
                _logger.Info("Cleared package cache.");
            }
        }

        private void MonitorFileSystem(bool monitor)
        {
            if (!EnableFileSystemMonitoring || !_runBackgroundTasks)
            {
                return;
            }

            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = monitor;
            }
            else
            {
                if (monitor)
                {
                    RegisterFileSystemWatcher();
                }
                else
                {
                    UnregisterFileSystemWatcher();
                }
            }

            _logger.DebugFormat("Monitoring {0} for new packages: {1}", Source, monitor);
        }

        /// <summary>
        /// Registers the file system watcher to monitor changes on disk.
        /// </summary>
        private void RegisterFileSystemWatcher()
        {
            // When files are moved around, recreate the package cache
            if (EnableFileSystemMonitoring && _runBackgroundTasks && _fileSystemWatcher == null && !string.IsNullOrEmpty(Source) && Directory.Exists(Source))
            {
                // ReSharper disable once UseObjectOrCollectionInitializer
                _fileSystemWatcher = new FileSystemWatcher(Source);
                _fileSystemWatcher.Filter = "*";
                _fileSystemWatcher.IncludeSubdirectories = true;

                _fileSystemWatcher.Changed += FileSystemChanged;
                _fileSystemWatcher.Created += FileSystemChanged;
                _fileSystemWatcher.Deleted += FileSystemChanged;
                _fileSystemWatcher.Renamed += FileSystemChanged;

                _fileSystemWatcher.EnableRaisingEvents = true;

                _logger.DebugFormat("Created FileSystemWatcher - monitoring {0}.", Source);
            }
        }

        /// <summary>
        /// Unregisters and clears events of the file system watcher to monitor changes on disk.
        /// </summary>
        private void UnregisterFileSystemWatcher()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _fileSystemWatcher.Changed -= FileSystemChanged;
                _fileSystemWatcher.Created -= FileSystemChanged;
                _fileSystemWatcher.Deleted -= FileSystemChanged;
                _fileSystemWatcher.Renamed -= FileSystemChanged;
                _fileSystemWatcher.Dispose();
                _fileSystemWatcher = null;

                _logger.DebugFormat("Destroyed FileSystemWatcher - no longer monitoring {0}.", Source);
            }
        }
        
        private void FileSystemChanged(object sender, FileSystemEventArgs e)
        {
            if (_isFileSystemWatcherSuppressed)
            {
                return;
            }

            _logger.DebugFormat("File system changed. File: {0} - Change: {1}", e.Name, e.ChangeType);

            // 1) If a .nupkg is dropped in the root, add it as a package
            if (String.Equals(Path.GetDirectoryName(e.FullPath), _fileSystemWatcher.Path, StringComparison.OrdinalIgnoreCase)
                && String.Equals(Path.GetExtension(e.Name), ".nupkg", StringComparison.OrdinalIgnoreCase))
            {
                // When a package is dropped into the server packages root folder, add it to the repository.
                AddPackagesFromDropFolder();
            }

            // 2) If a file is updated in a subdirectory, *or* a folder is deleted, invalidate the cache
            if ((!String.Equals(Path.GetDirectoryName(e.FullPath), _fileSystemWatcher.Path, StringComparison.OrdinalIgnoreCase) && File.Exists(e.FullPath))
                || e.ChangeType == WatcherChangeTypes.Deleted)
            { 
                // TODO: invalidating *all* packages for every nupkg change under this folder seems more expensive than it should.
                // Recommend using e.FullPath to figure out which nupkgs need to be (re)computed.

                ClearCache();
            }
        }

        private bool AllowOverrideExistingPackageOnPush
        {
            get
            {
                return _config.AllowOverrideExistingPackageOnPush;
            }
        }

        private bool IgnoreSymbolsPackages
        {
            get
            {
                // If the setting is misconfigured, treat it as "false" (backwards compatibility).
                return _config.IgnoreSymbolsPackages;
            }
        }

        private bool EnableDelisting
        {
            get
            {
                // If the setting is misconfigured, treat it as off (backwards compatibility).
                return _config.EnableDelisting;
            }
        }

        private bool EnableFrameworkFiltering
        {
            get
            {
                // If the setting is misconfigured, treat it as off (backwards compatibility).
                return _config.EnableFrameworkFiltering;
            }
        }

        private bool EnableFileSystemMonitoring
        {
            get
            {
                // If the setting is misconfigured, treat it as on (backwards compatibility).
                return _config.EnableFileSystemMonitoring;
            }
        }
        
        private IDisposable LockAndSuppressFileSystemWatcher()
        {
            return new SupressedFileSystemWatcher(this);
        }

        IEnumerable<HostedPackage> IPackageService.FindPackagesById(string id, ClientCompatibility compatibility)
        {
            var found = this.FindPackagesById(id, compatibility);
            return found.Select(ToHostedPackage);
        }

        HostedPackage IPackageService.FindPackage(string packageId, NuGetVersion version)
        {
            ServerPackage serverPackage = this.FindPackage(packageId, version);
            return ToHostedPackage(serverPackage);
        }

        public HostedPackage ToHostedPackage(ServerPackage serverPackage)
        {
            if(serverPackage == null)
                return null;
            return new HostedPackage(new ODataPackage(serverPackage));
        }

        public void PushPackage(Stream pkgStream)
        {
            using (LockAndSuppressFileSystemWatcher())
            {
                // Copy to correct filesystem location
                var package = _expandedPackageRepository.AddPackage(pkgStream, this.AllowOverrideExistingPackageOnPush);

                // Add to metadata store
                _serverPackageStore.Store(CreateServerPackage(package, EnableDelisting));

                _logger.InfoFormat("Finished adding package {0} {1}.", package.Identity.Id, package.Identity.Version);
            }
        }

        public Func<Stream> GetStream(PackageIdentity packageIdentity)
        {
            return _expandedPackageRepository.GetStream(packageIdentity);
        }

        private class SupressedFileSystemWatcher : IDisposable
        {
            private readonly ServerPackageRepository _repository;

            public SupressedFileSystemWatcher(ServerPackageRepository repository)
            {
                if (repository == null)
                {
                    throw new ArgumentNullException(nameof(repository));
                }

                _repository = repository;

                // Lock the repository.
                bool lockTaken = false;
                try
                {
                    Monitor.Enter(_repository._syncLock, ref lockTaken);
                }
                catch
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(_repository._syncLock);
                    }

                    throw;
                }

                // Suppress the file system events.
                _repository._isFileSystemWatcherSuppressed = true;
            }

            public void Dispose()
            {
                Monitor.Exit(_repository._syncLock);

                _repository._isFileSystemWatcherSuppressed = false;
            }
        }
    }
}