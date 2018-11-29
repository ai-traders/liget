using Newtonsoft.Json;

namespace BaGet.Web.Models
{
    // see https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource#package-dependency-group
    public class DependencyGroup
    {
        public DependencyGroup() {
            Type = "PackageDependencyGroup";
        }

        [JsonProperty(PropertyName = "@id")]
        public string CatalogUrl { get; set; }

        [JsonProperty(PropertyName = "@type")]
        public string Type { get; set; }

        public string TargetFramework { get; set; }

        public PackageDependency[] Dependencies { get; set; }
    }
}