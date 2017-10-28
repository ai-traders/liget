// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Newtonsoft.Json;
using NuGet;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

namespace LiGet.NuGet.Server.Infrastructure
{
    //TODO !!! for parsing deps use - https://github.com/NuGet/NuGet.Client/blob/c282092fc575891b30186d7baa1962ce3fb1d4b4/src/NuGet.Core/NuGet.Protocol/LegacyFeed/V2FeedPackageInfo.cs#L228
    //Package stored locally in server's folder-based data store. Already accepted, hashed, with extracted nuspec.
    public class ServerPackage :  IPackage
    {
        public ServerPackage()
        {
        }

        [Obsolete("Do not use IPackage")]
        public ServerPackage(IPackage package, PackageDerivedData packageDerivedData)
        {
            Id = package.Id;
            Version = package.Version;
            Title = package.Title;
            Authors = string.Join(",", package.Authors);
            Owners = string.Join(",",package.Owners);
            IconUrl = package.IconUrl;
            LicenseUrl = package.LicenseUrl;
            ProjectUrl = package.ProjectUrl;
            RequireLicenseAcceptance = package.RequireLicenseAcceptance;
            DevelopmentDependency = package.DevelopmentDependency;
            Description = package.Description;
            Summary = package.Summary;
            ReleaseNotes = package.ReleaseNotes;
            Language = package.Language;
            Tags = package.Tags;
            Copyright = package.Copyright;
            MinClientVersion = package.MinClientVersion;
            ReportAbuseUrl = package.ReportAbuseUrl;
            DownloadCount = package.DownloadCount;
            SemVer1IsAbsoluteLatest = false;
            SemVer1IsLatest = false;
            SemVer2IsAbsoluteLatest = false;
            SemVer2IsLatest = false;
            Listed = package.Listed;
            Published = package.Published;

            IsSemVer2 = IsPackageSemVer2(package);

            _dependencySets = package.DependencySets.ToList();
            Dependencies = DependencySetsAsString(package.DependencySets);

            _supportedFrameworks = package.GetSupportedFrameworks().ToList();
            SupportedFrameworks = string.Join("|", package.GetSupportedFrameworks().Select(f => f.GetFrameworkString()));

            PackageSize = packageDerivedData.PackageSize;
            PackageHash = packageDerivedData.PackageHash;
            PackageHashAlgorithm = packageDerivedData.PackageHashAlgorithm;
            LastUpdated = packageDerivedData.LastUpdated;
            Created = packageDerivedData.Created;
            Path = packageDerivedData.Path;
            FullPath = packageDerivedData.FullPath;
        }

        public ServerPackage(LocalPackageInfo localPackage, PackageDerivedData packageDerivedData)
        {
            var package = localPackage.Nuspec;
            Id = package.GetId();
            Version = package.GetVersion();
            Title = package.GetTitle();
            Authors = package.GetAuthors();
            Owners = package.GetOwners();
            IconUrl = package.GetIconUrl();
            LicenseUrl = package.GetLicenseUrl();
            ProjectUrl = package.GetProjectUrl();
            RequireLicenseAcceptance = package.GetRequireLicenseAcceptance();
            DevelopmentDependency = package.GetDevelopmentDependency();
            Description = package.GetDescription();
            Summary = package.GetSummary();
            ReleaseNotes = package.GetReleaseNotes();
            Language = package.GetLanguage();
            Tags = package.GetTags();
            Copyright = package.GetCopyright();
            MinClientVersion = package.GetMinClientVersion();
            ReportAbuseUrl = null;
            DownloadCount = 0;
            SemVer1IsAbsoluteLatest = false;
            SemVer1IsLatest = false;
            SemVer2IsAbsoluteLatest = false;
            SemVer2IsLatest = false;
            //FIXME is this OK?
            Listed = true; 

            IsSemVer2 = IsPackageSemVer2(package);
    
             _dependencySets = package.GetDependencyGroups().ToList();
            Dependencies = DependencySetsAsString(_dependencySets);

            _supportedFrameworks = package.GetFrameworkReferenceGroups().Select(f => f.TargetFramework).ToList();
            SupportedFrameworks = string.Join("|", _supportedFrameworks.Select(f => f.GetFrameworkString()));

            PackageSize = packageDerivedData.PackageSize;
            PackageHash = packageDerivedData.PackageHash;
            PackageHashAlgorithm = packageDerivedData.PackageHashAlgorithm;
            LastUpdated = packageDerivedData.LastUpdated;
            Created = packageDerivedData.Created;
            Path = packageDerivedData.Path;
            FullPath = packageDerivedData.FullPath;
        }

        [JsonRequired]
        public string Id { get; set; }

        [JsonRequired, JsonConverter(typeof(SemanticVersionJsonConverter))]
        public SemanticVersion Version { get; set; }

        public string Title { get; set; }

        public string Authors { get; set; }

        public string Owners { get; set; }

        public string IconUrl { get; set; }

        public string LicenseUrl { get; set; }

        public string ProjectUrl { get; set; }

        public bool RequireLicenseAcceptance { get; set; }

        public bool DevelopmentDependency { get; set; }

        public string Description { get; set; }

        public string Summary { get; set; }

        public string ReleaseNotes { get; set; }

        public string Language { get; set; }

        public string Tags { get; set; }

        public string Copyright { get; set; }

        public string Dependencies { get; set; }

        private List<PackageDependencyGroup> _dependencySets;

        [JsonIgnore]
        public IEnumerable<PackageDependencyGroup> DependencySets
        {
            get
            {
                if (String.IsNullOrEmpty(Dependencies))
                {
                    return Enumerable.Empty<PackageDependencyGroup>();
                }

                if (_dependencySets == null)
                {
                    _dependencySets = ParseDependencySet(Dependencies);
                }

                return _dependencySets;
            }
        }

        [JsonConverter(typeof(SemanticVersionJsonConverter))]
        public NuGetVersion MinClientVersion { get; set; }

        public Uri ReportAbuseUrl { get; set; }

        public int DownloadCount { get; set; }

        public string SupportedFrameworks { get; set; }

        private List<NuGetFramework> _supportedFrameworks;
        public IEnumerable<NuGetFramework> GetSupportedFrameworks()
        {
            if (String.IsNullOrEmpty(SupportedFrameworks))
            {
                return Enumerable.Empty<NuGetFramework>();
            }

            if (_supportedFrameworks == null)
            {
                var supportedFrameworksAsStrings = SupportedFrameworks.Split('|').ToList();

                _supportedFrameworks = supportedFrameworksAsStrings
                    .Select(NuGetFramework.Parse)
                    .ToList();
            }

            return _supportedFrameworks;
        }

        public bool IsAbsoluteLatestVersion => IsSemVer2 ? SemVer2IsAbsoluteLatest : SemVer1IsAbsoluteLatest;

        public bool IsLatestVersion => IsSemVer2 ? SemVer2IsLatest : SemVer1IsLatest;

        public bool SemVer1IsAbsoluteLatest { get; set; }

        public bool SemVer1IsLatest { get; set; }

        public bool SemVer2IsAbsoluteLatest { get; set; }

        public bool SemVer2IsLatest { get; set; }

        public bool Listed { get; set; }

        public DateTimeOffset? Published { get; set; }

        public bool IsSemVer2 { get; set; }

        public long PackageSize { get; set; }

        public string PackageHash { get; set; }

        public string PackageHashAlgorithm { get; set; }

        public DateTimeOffset LastUpdated { get; set; }

        public DateTimeOffset Created { get; set; }

        public string Path { get; set; }

        public string FullPath { get; set; }

        private static string DependencySetsAsString(IEnumerable<PackageDependencyGroup> dependencySets)
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

        private static List<PackageDependencyGroup> ParseDependencySet(string value)
        {
            var dependencySets = new List<PackageDependencyGroup>();

            var dependencies = value.Split('|').Select(ParseDependency).ToList();

            // group the dependencies by target framework
            var groups = dependencies.GroupBy(d => d.Item3);

            dependencySets.AddRange(
                groups.Select(g => new PackageDependencyGroup(
                    g.Key,   // target framework
                    g.Where(pair => !String.IsNullOrEmpty(pair.Item1))                   // the Id is empty when a group is empty.
                     .Select(pair => new PackageDependency(pair.Item1, pair.Item2)))));  // dependencies by that target framework
            return dependencySets;
        }

        /// <summary>
        /// Parses a dependency from the feed in the format:
        ///     id or id:versionSpec, or id:versionSpec:targetFramework
        /// </summary>
        private static Tuple<string, VersionRange, NuGetFramework> ParseDependency(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            // IMPORTANT: Do not pass StringSplitOptions.RemoveEmptyEntries to this method, because it will break
            // if the version spec is null, for in that case, the Dependencies string sent down is "<id>::<target framework>".
            // We do want to preserve the second empty element after the split.
            string[] tokens = value.Trim().Split(new[] { ':' });

            if (tokens.Length == 0)
            {
                return null;
            }

            // Trim the id
            string id = tokens[0].Trim();

            VersionRange versionSpec = null;
            if (tokens.Length > 1)
            {
                // Attempt to parse the version
                VersionRange.TryParse(tokens[1], out versionSpec);
            }

            var targetFramework = (tokens.Length > 2 && !String.IsNullOrEmpty(tokens[2]))
                                    ? NuGetFramework.Parse(tokens[2])
                                    : null;

            return Tuple.Create(id, versionSpec, targetFramework);
        }

        private static bool IsPackageSemVer2(IPackage package)
        {
            if (package.Version.IsSemVer2())
            {
                return true;
            }

            if (package.DependencySets != null)
            {
                foreach (var dependencySet in package.DependencySets)
                {
                    foreach (var dependency in dependencySet.Packages)
                    {
                        var range = dependency.VersionRange;
                        if (range == null)
                        {
                            continue;
                        }

                        if (range.MinVersion != null && range.MinVersion.IsSemVer2())
                        {
                            return true;
                        }

                        if (range.MaxVersion != null && range.MaxVersion.IsSemVer2())
                        {
                            return true;
                        }
                    }
                }
            }


            return false;
        }

        private static bool IsPackageSemVer2(NuspecReader package)
        {
            if (package.GetVersion().IsSemVer2())
            {
                return true;
            }

            if (package.GetDependencyGroups() != null)
            {
                foreach (var dependencySet in package.GetDependencyGroups())
                {
                    foreach (var dependency in dependencySet.Packages)
                    {
                        var range = dependency.VersionRange;
                        if (range == null)
                        {
                            continue;
                        }

                        if (range.MinVersion != null && range.MinVersion.IsSemVer2())
                        {
                            return true;
                        }

                        if (range.MaxVersion != null && range.MaxVersion.IsSemVer2())
                        {
                            return true;
                        }
                    }
                }
            }


            return false;
        }

        #region Unsupported operations

        public IEnumerable<IPackageFile> GetFiles()
        {
            throw new NotImplementedException("The NuGet.Server.ServerPackage type does not support getting files.");
        }

        public Stream GetStream()
        {
            throw new NotImplementedException("The NuGet.Server.ServerPackage type does not support getting a stream.");
        }

        public void ExtractContents(IFileSystem fileSystem, string extractPath)
        {
            throw new NotImplementedException("The NuGet.Server.ServerPackage type does not support extracting contents.");
        }

        [JsonIgnore]
        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get
            {
                throw new NotImplementedException("The NuGet.Server.ServerPackage type does not support enumerating FrameworkAssemblies.");
            }
        }

        [JsonIgnore]
        public ICollection<PackageReferenceSet> PackageAssemblyReferences
        {
            get
            {
                throw new NotImplementedException("The NuGet.Server.ServerPackage type does not support enumerating PackageAssemblyReferences.");
            }
        }

        [JsonIgnore]
        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get
            {
                throw new NotImplementedException("The NuGet.Server.ServerPackage type does not support enumerating AssemblyReferences.");
            }
        }

        #endregion
    }
}
