using System;
using Newtonsoft.Json;

namespace BaGet.Web.Models
{
    public class SearchResultVersionModel
    {
        public SearchResultVersionModel(string registrationUrl, string version, long downloads)
        {
            if (string.IsNullOrEmpty(registrationUrl)) throw new ArgumentNullException(nameof(registrationUrl));
            if (string.IsNullOrEmpty(version)) throw new ArgumentNullException(nameof(version));

            RegistrationUrl = registrationUrl;
            Version = version;
            Downloads = downloads;
        }

        [JsonProperty(PropertyName = "id")]
        public string RegistrationUrl { get; }

        public string Version { get; }

        public long Downloads { get; }
    }
}