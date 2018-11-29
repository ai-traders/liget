using System;
using System.Collections.Generic;
using System.Linq;
using BaGet.Core.Entities;
using BaGet.Core.Legacy;
using Newtonsoft.Json;
using NuGet.Frameworks;
using NuGet.Protocol.Core.Types;

namespace BaGet.Web.Models
{
    public class CatalogEntry
    {
        public CatalogEntry(Package package, string catalogUri, string packageContent, Func<string, Uri> getRegistrationUrl)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));

            CatalogUri = catalogUri ?? throw new ArgumentNullException(nameof(catalogUri));

            PackageId = package.Id;
            Version = package.VersionString;
            Authors = package.Authors == null ? null : string.Join(", ", package.Authors);
            Description = package.Description;
            Downloads = package.Downloads;
            HasReadme = package.HasReadme;
            IconUrl = package.IconUrlString;
            Language = package.Language;
            LicenseUrl = package.LicenseUrlString;
            Listed = package.Listed;
            MinClientVersion = package.MinClientVersion;
            PackageContent = packageContent;
            ProjectUrl = package.ProjectUrlString;
            RepositoryUrl = package.RepositoryUrlString;
            RepositoryType = package.RepositoryType;
            Published = package.Published;
            RequireLicenseAcceptance = package.RequireLicenseAcceptance;
            Summary = package.Summary;
            Tags = package.Tags;
            Title = package.Title;
            DependencyGroups = ToDependencyGroups(package.Dependencies, catalogUri, getRegistrationUrl);
        }

        public static DependencyGroup[] ToDependencyGroups(List<Core.Entities.PackageDependency> dependencies, 
            string catalogUri, Func<string, Uri> getRegistrationUrl)
        {
            if(dependencies == null || !dependencies.Any())
                return null;

            var groups = new List<DependencyGroup>();
            var frameworkDeps = dependencies.Where(d => d.IsFrameworkDependency()).Select(d => d.TargetFramework).Distinct();
            foreach(var frameworkDep in frameworkDeps) {
                var g = new DependencyGroup() {
                    CatalogUrl = catalogUri + $"#dependencygroup/.{frameworkDep}",
                    TargetFramework = frameworkDep
                };
                groups.Add(g);
            }
            // empty string key implies no target framework
            Dictionary<string, List<PackageDependency>> dependenciesByFramework = new Dictionary<string, List<PackageDependency>>();
            foreach (var packageDependency in dependencies.Where(d => !d.IsFrameworkDependency()))
            {
                var dep = new PackageDependency() {
                    Id = packageDependency.Id,
                    Range = packageDependency.VersionRange
                };
                string framework = packageDependency.TargetFramework == null ? "" : packageDependency.TargetFramework;
                List<PackageDependency> deps = new List<PackageDependency>();
                if (!dependenciesByFramework.TryGetValue(framework, out deps)) {
                    deps = new List<PackageDependency>();
                    dependenciesByFramework.Add(framework, deps);
                }
                deps.Add(dep);
            }
            var perFrameworkDeps = 
                dependenciesByFramework.GroupBy(d => d.Key)
                .Select(grouppedDeps => 
                {
                    var framework = string.IsNullOrEmpty(grouppedDeps.Key) ? null : grouppedDeps.Key;
                    string catalogForGroup = catalogUri + "#dependencygroup";
                    if(framework != null)
                        catalogForGroup = catalogUri + $"#dependencygroup/.{framework}";
                    var g = new DependencyGroup() {         
                        CatalogUrl = catalogForGroup,           
                        TargetFramework = framework,
                        Dependencies = grouppedDeps.SelectMany(d => d.Value)
                            .Select(d => new PackageDependency() {
                                CatalogUrl = catalogUri + $"#dependencygroup/.{grouppedDeps.Key}/{d.Id}",
                                Id = d.Id,
                                Range = d.Range,
                                Registration = getRegistrationUrl(d.Id).AbsoluteUri
                            }).ToArray()
                    };
                    return g;
                });

            return groups.Concat(perFrameworkDeps).ToArray();
        }

        public CatalogEntry(IPackageSearchMetadata package, string catalogUri, string packageContent, Func<string, Uri> getRegistrationUrl)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));

            CatalogUri = catalogUri ?? throw new ArgumentNullException(nameof(catalogUri));

            PackageId = package.Identity.Id;
            Version = package.Identity.Version.ToFullString();
            Authors = string.Join(", ", package.Authors);
            Description = package.Description;
            Downloads = package.DownloadCount.GetValueOrDefault(0);
            HasReadme = false; //
            IconUrl = NullSafeToString(package.IconUrl);
            Language = null; //
            LicenseUrl = NullSafeToString(package.LicenseUrl);
            Listed = package.IsListed;
            //MinClientVersion =
            PackageContent = packageContent;
            ProjectUrl = NullSafeToString(package.ProjectUrl);
            //RepositoryUrl = package.RepositoryUrlString;
            //RepositoryType = package.RepositoryType;
            //Published = package.Published.GetValueOrDefault(DateTimeOffset.MinValue);
            RequireLicenseAcceptance = package.RequireLicenseAcceptance;
            Summary = package.Summary;
            Tags = package.Tags == null ? null : package.Tags.Split(',');
            Title = package.Title;
            DependencyGroups = ToDependencyGroups(package.DependencySets, catalogUri, getRegistrationUrl);
        }

        public static DependencyGroup[] ToDependencyGroups(IEnumerable<NuGet.Packaging.PackageDependencyGroup> dependencies, string catalogUri,
            Func<string, Uri> getRegistrationUrl)
        {
            return dependencies.Select(grouppedDeps => {
                string targetFramework;
                if(grouppedDeps.TargetFramework == null || grouppedDeps.TargetFramework.Equals(NuGetFramework.AnyFramework))
                    targetFramework = null;
                else
                    targetFramework = grouppedDeps.TargetFramework.GetShortFolderName();
                string catalogForGroup = catalogUri + "#dependencygroup";
                if(targetFramework != null)
                    catalogForGroup = catalogUri + $"#dependencygroup/.{targetFramework}";
                var g = new DependencyGroup() {         
                    CatalogUrl = catalogForGroup,           
                    TargetFramework = targetFramework,
                    Dependencies = grouppedDeps.Packages
                        .Select(d => new PackageDependency() {
                            CatalogUrl = catalogUri + $"#dependencygroup/.{targetFramework}/{d.Id}",
                            Id = d.Id,
                            Range = d.VersionRange.ToNormalizedString(),
                            Registration = getRegistrationUrl(d.Id).AbsoluteUri
                        }).ToArray()
                };
                if(g.Dependencies.Length == 0)
                    g.Dependencies = null;
                return g;
                }).ToArray();
        }

        private string NullSafeToString(object prop)
        {
            if(prop == null)
                return null;
            return prop.ToString();
        }

        [JsonProperty(PropertyName = "@id")]
        public string CatalogUri { get; }

        [JsonProperty(PropertyName = "id")]
        public string PackageId { get; }

        public string Version { get; }
        public string Authors { get; }
        public string Description { get; }
        public long Downloads { get; }
        public bool HasReadme { get; }
        public string IconUrl { get; }
        public string Language { get; }
        public string LicenseUrl { get; }
        public bool Listed { get; }
        public string MinClientVersion { get; }
        public string PackageContent { get; }
        public string ProjectUrl { get; }
        public string RepositoryUrl { get; }
        public string RepositoryType { get; }
        public DateTime Published { get; }
        public bool RequireLicenseAcceptance { get; }
        public string Summary { get; }
        public string[] Tags { get; }
        public string Title { get; }

        public DependencyGroup[] DependencyGroups { get; }
    }
}
