
using System;
using NuGet;
using NuGet.Protocol;
using NuGet.Versioning;

namespace BaGet.Core.Legacy.OData
{
    public class ODataPackage : IEquatable<ODataPackage>
    {
        public ODataPackage() {}

        public ODataPackage(Entities.Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if(package.Version == null)
                throw new ArgumentException("server package version is null");
            Version = package.Version.OriginalVersion;
            NormalizedVersion = package.Version.ToNormalizedString();

            Authors = package.Authors == null ? null : string.Join(",", package.Authors);
            IconUrl = package.IconUrl?.AbsoluteUri;
            LicenseUrl = package.LicenseUrl?.AbsoluteUri;
            ProjectUrl = package.ProjectUrl?.AbsoluteUri;
            Dependencies = package.Dependencies?.ToDependenciesString();

            Id = package.Id;
            Title = package.Title;
            RequireLicenseAcceptance = package.RequireLicenseAcceptance;
            Description = package.Description;
            Summary = package.Summary;            
            Language = package.Language;
            Tags = package.Tags == null ? null : string.Join(",", package.Tags);
            //PackageHashAlgorithm = package.PackageHashAlgorithm;
            //LastUpdated = package.LastUpdated.UtcDateTime;
            Published = package.Published;
            // IsAbsoluteLatestVersion = package.IsAbsoluteLatestVersion;
            // IsLatestVersion = package.IsLatestVersion;
            // IsPrerelease = !package.IsReleaseVersion();
            Listed = package.Listed;
            DownloadCount = (int)package.Downloads;
            if(package.MinClientVersion != null)
                MinClientVersion = package.MinClientVersion;

            //PackageSize = package.PackageSize;
            //Created = package.Created.UtcDateTime;
            //VersionDownloadCount = package.VersionDownloadCount;
        }

        public string Id { get; set; }

        public string Version { get; set; }

        public string NormalizedVersion { get; set; }

        public bool IsPrerelease { get; set; }

        public string Title { get; set; }

        public string Authors { get; set; }

        public string Owners { get; set; }

        public string IconUrl { get; set; }

        public string LicenseUrl { get; set; }

        public string ProjectUrl { get; set; }

        public int DownloadCount { get; set; }

        public bool RequireLicenseAcceptance { get; set; }

        public bool DevelopmentDependency { get; set; }

        public string Description { get; set; }

        public string Summary { get; set; }

        public string ReleaseNotes { get; set; }

        public DateTime Published { get; set; }

        public DateTime LastUpdated { get; set; }

        public string Dependencies { get; set; }

        public string PackageHash { get; set; }

        public string PackageHashAlgorithm { get; set; }

        public int PackageSize { get; set; }

        public string Copyright { get; set; }

        public string Tags { get; set; }

        public bool IsAbsoluteLatestVersion { get; set; }

        public bool IsLatestVersion { get; set; }

        public bool Listed { get; set; }

        public int VersionDownloadCount { get; set; }

        public string MinClientVersion { get; set; }

        public string Language { get; set; }

        public override string ToString() {
            return string.Format("{0} {1}",Id,Version);
        }

        public bool Equals(ODataPackage other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(Id, other.Id) && Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ODataPackage);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode()*5432 + Version.GetHashCode()*754;
        }
    }
}
