using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core.Entities;
using BaGet.Core.Extensions;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace BaGet.Core.Services
{
    using BaGetPackageDependency = Entities.PackageDependency;

    public class IndexingService : IIndexingService
    {
        private readonly IPackageService _packages;
        private readonly IPackageStorageService _storage;
        private readonly ISearchService _search;
        private readonly ILogger<IndexingService> _logger;

        public IndexingService(
            IPackageService packages,
            IPackageStorageService storage,
            ISearchService search,
            ILogger<IndexingService> logger)
        {
            _packages = packages ?? throw new ArgumentNullException(nameof(packages));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IndexingResult> IndexAsync(Stream packageStream, CancellationToken cancellationToken)
        {
            // Try to extract all the necessary information from the package.
            Package package;
            Stream nuspecStream;
            Stream readmeStream;

            try
            {
                using (var packageReader = new PackageArchiveReader(packageStream, leaveStreamOpen: true))
                {
                    package = packageReader.GetPackageMetadata();
                    nuspecStream = await packageReader.GetNuspecAsync(cancellationToken);

                    if (package.HasReadme)
                    {
                        readmeStream = await packageReader.GetReadmeAsync(cancellationToken);
                    }
                    else
                    {
                        readmeStream = null;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Uploaded package is invalid");

                return IndexingResult.InvalidPackage;
            }

            // The package is well-formed. Ensure this is a new package.
            if (await _packages.ExistsAsync(new PackageIdentity(package.Id, package.Version)))
            {
                return IndexingResult.PackageAlreadyExists;
            }

            // TODO: Add more package validations
            _logger.LogInformation(
                "Validated package {PackageId} {PackageVersion}, persisting content to storage...",
                package.Id,
                package.VersionString);

            try
            {
                packageStream.Position = 0;

                await _storage.SavePackageContentAsync(
                    package,
                    packageStream,
                    nuspecStream,
                    readmeStream,
                    cancellationToken);
            }
            catch (Exception e)
            {
                // This may happen due to concurrent pushes.
                // TODO: Make IPackageStorageService.SavePackageContentAsync return a result enum so this
                // can be properly handled.
                _logger.LogError(
                    e,
                    "Failed to persist package {PackageId} {PackageVersion} content to storage",
                    package.Id,
                    package.VersionString);

                throw;
            }

            _logger.LogInformation(
                "Persisted package {Id} {Version} content to storage, saving metadata to database...",
                package.Id,
                package.VersionString);

            var result = await _packages.AddAsync(package);
            if (result == PackageAddResult.PackageAlreadyExists)
            {
                _logger.LogWarning(
                    "Package {Id} {Version} metadata already exists in database",
                    package.Id,
                    package.VersionString);

                return IndexingResult.PackageAlreadyExists;
            }

            if (result != PackageAddResult.Success)
            {
                _logger.LogError($"Unknown {nameof(PackageAddResult)} value: {{PackageAddResult}}", result);

                throw new InvalidOperationException($"Unknown {nameof(PackageAddResult)} value: {result}");
            }

            _logger.LogInformation(
                "Successfully persisted package {Id} {Version} metadata to database. Indexing in search...",
                package.Id,
                package.VersionString);

            await _search.IndexAsync(package);

            _logger.LogInformation(
                "Successfully indexed package {Id} {Version} in search",
                package.Id,
                package.VersionString);

            return IndexingResult.Success;
        }
    }
}