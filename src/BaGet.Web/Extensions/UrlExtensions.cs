using System;
using Microsoft.AspNetCore.Http;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace BaGet.Web.Extensions
{
    public static class CarterUrlExtensions
    {
        public static string UriCombine (this string val, string append)
        {
            if (string.IsNullOrEmpty(val)) return append;
            if (string.IsNullOrEmpty(append)) return val;
            return val.TrimEnd('/') + "/" + append.TrimStart('/');
        }

        public static string V3Index(this HttpRequest request, string prefix) => request.AbsoluteUrl(prefix.UriCombine("v3/index.json"));

        public static string PackageBase(this HttpRequest request, string prefix) => request.AbsoluteUrl(prefix.UriCombine("v3/package/"));
        public static string RegistrationsBase(this HttpRequest request, string prefix) => request.AbsoluteUrl(prefix.UriCombine("v3/registration/"));

        public static string PackagePublish(this HttpRequest request, string prefix) => request.AbsoluteUrl(prefix.UriCombine("v2/package"));
        public static string PackageSearch(this HttpRequest request, string prefix) => request.AbsoluteUrl(prefix.UriCombine("v3/search"));
        public static string PackageAutocomplete(this HttpRequest request, string prefix) => request.AbsoluteUrl(prefix.UriCombine("v3/autocomplete"));

        public static string PackageDownload(this HttpRequest request, PackageIdentity pid, string prefix)
        {
            var id = pid.Id.ToLowerInvariant();
            var versionString = pid.Version.ToNormalizedString().ToLowerInvariant();
            var relativePath = string.Format("v3/package/{0}/{1}/{0}.{1}.nupkg", id, versionString);
            relativePath = prefix.UriCombine(relativePath);
            return request.AbsoluteUrl(relativePath);
        }

        public static string PackageRegistration(this HttpRequest request, string id, string prefix) {
            id = id.ToLowerInvariant();
            var relativePath = string.Format("v3/registration/{0}/index.json", id);
            relativePath = prefix.UriCombine(relativePath);
            return request.AbsoluteUrl(relativePath);
        }

        public static string PackageRegistration(this HttpRequest request, PackageIdentity pid, string prefix) {
            var id = pid.Id.ToLowerInvariant();
            var versionString = pid.Version.ToNormalizedString().ToLowerInvariant();
            var relativePath = string.Format("v3/registration/{0}/{1}.json", id, versionString);
            relativePath = prefix.UriCombine(relativePath);
            return request.AbsoluteUrl(relativePath);
        }

        public static string AbsoluteUrl(this HttpRequest request, string relativePath)
        {
            return new Uri(new Uri(request.Scheme + "://" + request.Host.Value), relativePath).ToString();
        }

        public static Uri GetUri(this HttpRequest request)
        {
            var builder = new UriBuilder();
            builder.Scheme = request.Scheme;
            builder.Host = request.Host.Host;
            builder.Port = request.Host.Port ?? 80;
            builder.Path = request.Path;
            builder.Query = request.QueryString.ToUriComponent();
            return builder.Uri;
        }
    }
}
