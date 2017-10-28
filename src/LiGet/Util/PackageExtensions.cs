using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LiGet.Models;
using LiGet.NuGet.Server.Infrastructure;
using NuGet;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;

namespace LiGet.Util
{
    public static class PackageExtensions
    {
        public static V2FeedPackageInfo ToV2FeedPackageInfo(
            this NuspecReader reader, PackageDerivedData packageDerivedData, string downloadUrl, long downloadCount)
        {
            return new V2FeedPackageInfo(
                identity: new PackageIdentity(reader.GetId(),reader.GetVersion()),
                title: reader.GetTitle(),
                summary: reader.GetSummary(),
                description: reader.GetDescription(),
                authors: reader.GetAuthors().Split(','),
                owners: reader.GetOwners().Split(','),
                iconUrl: reader.GetIconUrl(),
                licenseUrl: reader.GetLicenseUrl(),
                projectUrl: reader.GetProjectUrl(),
                reportAbuseUrl: reader.GetProjectUrl(),
                tags: reader.GetTags(),
                created: packageDerivedData.Created,
                lastEdited: packageDerivedData.LastUpdated,
                published: packageDerivedData.LastUpdated,
                dependencies: DependencySetsAsString(reader.GetDependencyGroups()),
                requireLicenseAccept: reader.GetRequireLicenseAcceptance(),
                downloadUrl: downloadUrl,
                downloadCount: downloadCount.ToString(),
                packageHash: packageDerivedData.PackageHash,
                packageHashAlgorithm: packageDerivedData.PackageHashAlgorithm,
                minClientVersion: reader.GetMinClientVersion()
            );

        }

        public static string DependencySetsAsString(this IEnumerable<PackageDependencyGroup> dependencySets)
        {
            if (dependencySets == null)
            {
                return null;
            }

            var dependencies = new List<string>();
            foreach (var dependencySet in dependencySets)
            {
                if (!dependencySet.Packages.Any())
                {
                    dependencies.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}", null, null, dependencySet.TargetFramework.GetFrameworkString()));
                }
                else
                {
                    foreach (var dependency in dependencySet.Packages.Select(d => new { d.Id, d.VersionRange, dependencySet.TargetFramework }))
                    {
                        dependencies.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}", 
                            dependency.Id, dependency.VersionRange == null ? null : dependency.VersionRange.ToNormalizedString(), dependencySet.TargetFramework.GetFrameworkString()));
                    }
                }
            }

            return string.Join("|", dependencies);
        }
    }
}