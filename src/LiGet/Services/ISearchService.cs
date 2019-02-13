using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiGet.Entities;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace LiGet.Services
{
    public interface ISearchService
    {
        Task IndexAsync(Package package);

        Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int skip = 0, int take = 20);

        Task<IReadOnlyList<string>> AutocompleteAsync(string query, int skip = 0, int take = 20);
    }

    public class SearchResult
    {
        public SearchResult()
        {
        }

        public SearchResult(IPackageSearchMetadata result, IEnumerable<VersionInfo> versions)
        {
            this.Id = result.Identity.Id;
            this.Version = result.Identity.Version;
            this.Description = result.Description;
            this.Authors = result.Authors;
            this.IconUrl = result.IconUrl?.AbsoluteUri;
            this.LicenseUrl = result.LicenseUrl?.AbsoluteUri;
            this.ProjectUrl = result.ProjectUrl?.AbsoluteUri;
            this.Summary = result.Summary;
            if(result.Tags != null)
                this.Tags = result.Tags.Split(',');
            this.Title = result.Title;
            this.TotalDownloads = result.DownloadCount ?? 0;
            var list  = new List<SearchResultVersion>();
            foreach(var v in versions) {
                list.Add(new SearchResultVersion(v));
            }
            this.Versions = list;
        }

        public string Id { get; set; }

        public NuGetVersion Version { get; set; }

        public string Description { get; set; }
        public string Authors { get; set; }
        public string IconUrl { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string Summary { get; set; }
        public string[] Tags { get; set; }
        public string Title { get; set; }
        public long TotalDownloads { get; set; }

        public IReadOnlyList<SearchResultVersion> Versions { get; set; }
    }

    public class SearchResultVersion
    {

        public SearchResultVersion(VersionInfo v)
        {
            this.Version = v.Version;
            this.Downloads = v.DownloadCount ?? 0;
        }

        public SearchResultVersion(NuGetVersion version, long downloads)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Downloads = downloads;
        }

        public NuGetVersion Version { get; }

        public long Downloads { get; }
    }
}
