using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core.Entities;
using NuGet.Packaging;

namespace BaGet.Core.Extensions
{
    public static class PackageArchiveReaderExtensions
    {
        private static readonly string[] OrderedReadmeFileNames = new[]
        {
            "readme.md",
            "readme.txt",
        };

        private static readonly HashSet<string> ReadmeFileNames = new HashSet<string>(
            OrderedReadmeFileNames,
            StringComparer.OrdinalIgnoreCase);

        public static bool HasReadme(this PackageArchiveReader package)
            => package.GetFiles().Any(ReadmeFileNames.Contains);

        public async static Task<Stream> GetReadmeAsync(
            this PackageArchiveReader package,
            CancellationToken cancellationToken)
        {
            var packageFiles = package.GetFiles();

            foreach (var readmeFileName in OrderedReadmeFileNames)
            {
                var readmePath = packageFiles.FirstOrDefault(f => f.Equals(readmeFileName, StringComparison.OrdinalIgnoreCase));

                if (readmePath != null)
                {
                    return await package.GetStreamAsync(readmePath, cancellationToken);
                }
            }

            throw new InvalidOperationException("Package does not have a readme!");
        }
        
        public static Package GetPackageMetadata(this PackageArchiveReader packageReader)
        {
            var nuspec = packageReader.NuspecReader;

            (var repositoryUri, var repositoryType) = GetRepositoryMetadata(nuspec);

            return new Package
            {
                Id = nuspec.GetId(),
                Version = nuspec.GetVersion(),
                Authors = ParseAuthors(nuspec.GetAuthors()),
                Description = nuspec.GetDescription(),
                HasReadme = packageReader.HasReadme(),
                Language = nuspec.GetLanguage() ?? string.Empty,
                Listed = true,
                MinClientVersion = nuspec.GetMinClientVersion()?.ToNormalizedString() ?? string.Empty,
                Published = DateTime.UtcNow,
                RequireLicenseAcceptance = nuspec.GetRequireLicenseAcceptance(),
                Summary = nuspec.GetSummary(),
                Title = nuspec.GetTitle(),
                IconUrl = ParseUri(nuspec.GetIconUrl()),
                LicenseUrl = ParseUri(nuspec.GetLicenseUrl()),
                ProjectUrl = ParseUri(nuspec.GetProjectUrl()),
                RepositoryUrl = repositoryUri,
                RepositoryType = repositoryType,
                Dependencies = GetDependencies(nuspec),
                Tags = ParseTags(nuspec.GetTags())
            };
        }

        private static Uri ParseUri(string uriString)
        {
            if (string.IsNullOrEmpty(uriString)) return null;

            return new Uri(uriString);
        }

        private static string[] ParseAuthors(string authors)
        {
            if (string.IsNullOrEmpty(authors)) return new string[0];

            return authors.Split(new[] { ',', ';', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string[] ParseTags(string tags)
        {
            if (string.IsNullOrEmpty(tags)) return new string[0];

            return tags.Split(new[] { ',', ';', ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static (Uri repositoryUrl, string repositoryType) GetRepositoryMetadata(NuspecReader nuspec)
        {
            var repository = nuspec.GetRepositoryMetadata();

            if (string.IsNullOrEmpty(repository?.Url) ||
                !Uri.TryCreate(repository.Url, UriKind.Absolute, out var repositoryUri))
            {
                return (null, null);
            }

            if (repositoryUri.Scheme != Uri.UriSchemeHttps)
            {
                return (null, null);
            }

            if (repository.Type.Length > 100)
            {
                throw new InvalidOperationException("Repository type must be less than or equal 100 characters");
            }

            return (repositoryUri, repository.Type);
        }

        private static List<PackageDependency> GetDependencies(NuspecReader nuspec)
        {
            var dependencies = new List<PackageDependency>();

            foreach (var group in nuspec.GetDependencyGroups())
            {
                var targetFramework = group.TargetFramework.GetShortFolderName();

                if (!group.Packages.Any())
                {
                    dependencies.Add(new PackageDependency
                    {
                        Id = null,
                        VersionRange = null,
                        TargetFramework = targetFramework,
                    });
                }

                foreach (var dependency in group.Packages)
                {
                    dependencies.Add(new PackageDependency
                    {
                        Id = dependency.Id,
                        VersionRange = dependency.VersionRange?.ToString(),
                        TargetFramework = targetFramework,
                    });
                }
            }

            return dependencies;
        }
    }
}
