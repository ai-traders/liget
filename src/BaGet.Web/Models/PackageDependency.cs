using Newtonsoft.Json;

namespace BaGet.Web.Models
{
    // https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource#package-dependency
    public class PackageDependency
    {
        public PackageDependency() {
            Type = "PackageDependency";
        }

        [JsonProperty(PropertyName = "@id")]
        public string CatalogUrl { get; set; }

        [JsonProperty(PropertyName = "@type")]
        public string Type { get; set; }

        public string Id { get; set; }

        public string Range { get; set; }

        public string Registration { get; set; }
    }
}