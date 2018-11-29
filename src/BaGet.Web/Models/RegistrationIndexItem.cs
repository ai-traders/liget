using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BaGet.Web.Models
{
    public class RegistrationIndexItem
    {
        
        public RegistrationIndexItem(
            string packageId,
            IReadOnlyList<RegistrationIndexLeaf> items,
            string lower,
            string upper)
        {
            if (string.IsNullOrEmpty(packageId)) throw new ArgumentNullException(nameof(packageId));
            if (string.IsNullOrEmpty(lower)) throw new ArgumentNullException(nameof(lower));
            if (string.IsNullOrEmpty(upper)) throw new ArgumentNullException(nameof(upper));

            PackageId = packageId;
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Lower = lower;
            Upper = upper;
        }

        [JsonProperty(PropertyName = "id")]
        public string PackageId { get; }

        public int Count => Items.Count;

        public IReadOnlyList<RegistrationIndexLeaf> Items { get; }

        public string Lower { get; }
        public string Upper { get; }
    }
}