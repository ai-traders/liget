using System;
using BaGet.Core.Entities;
using Newtonsoft.Json;
using NuGet.Protocol.Core.Types;
using BaGet.Web.Extensions;

namespace BaGet.Web.Models
{
    public class RegistrationIndexLeaf
    {
        public RegistrationIndexLeaf(string packageId, CatalogEntry catalogEntry, string packageContent)
        {
            if (string.IsNullOrEmpty(packageId)) throw new ArgumentNullException(nameof(packageId));

            PackageId = packageId;
            CatalogEntry = catalogEntry ?? throw new ArgumentNullException(nameof(catalogEntry));
            PackageContent = packageContent ?? throw new ArgumentNullException(nameof(packageContent));
        }

        [JsonProperty(PropertyName = "id")]
        public string PackageId { get; }

        public CatalogEntry CatalogEntry { get; }

        public string PackageContent { get; }
    }
}
