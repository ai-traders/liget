using System;
using Newtonsoft.Json;

namespace BaGet.Web.Models
{
    public class ServiceResource
    {
        public ServiceResource(string type, string id, string comment = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Comment = comment ?? string.Empty;
        }

        [JsonProperty(PropertyName = "@id")]
        public string Id { get; }

        [JsonProperty(PropertyName = "@type")]
        public string Type { get; }

        public string Comment { get; }
    }
}