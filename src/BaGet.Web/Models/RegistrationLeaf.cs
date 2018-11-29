using System;
using Newtonsoft.Json;

namespace BaGet.Web.Models
{
    public class RegistrationLeaf
    {
        public RegistrationLeaf(
            string registrationUri,
            bool listed,
            long downloads,
            string packageContentUri,
            DateTimeOffset published,
            string registrationIndexUri)
        {
            RegistrationUri = registrationUri ?? throw new ArgumentNullException(nameof(registrationIndexUri));
            Listed = listed;
            Published = published;
            Downloads = downloads;
            PackageContent = packageContentUri ?? throw new ArgumentNullException(nameof(packageContentUri));
            RegistrationIndexUri = registrationIndexUri ?? throw new ArgumentNullException(nameof(registrationIndexUri));
        }

        [JsonProperty(PropertyName = "@id")]
        public string RegistrationUri { get; }

        public bool Listed { get; }

        public long Downloads { get; }

        public string PackageContent { get; }

        public DateTimeOffset Published { get; }

        [JsonProperty(PropertyName = "registration")]
        public string RegistrationIndexUri { get; }
    }
}