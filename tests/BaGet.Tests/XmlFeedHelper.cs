using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

namespace BaGet.Tests
{
    // adapted from https://github.com/NuGet/NuGet.Client/blob/24005c4812695e74561579179765b9bda5290ae0/src/NuGet.Core/NuGet.Protocol/LegacyFeed/V2FeedParser.cs
    public class XmlFeedHelper
    {
        
        private const string W3Atom = "http://www.w3.org/2005/Atom";
        private const string MetadataNS = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private const string DataServicesNS = "http://schemas.microsoft.com/ado/2007/08/dataservices";

        // XNames used in the feed
        private static readonly XName _xnameEntry = XName.Get("entry", W3Atom);
        private static readonly XName _xnameTitle = XName.Get("title", W3Atom);
        private static readonly XName _xnameContent = XName.Get("content", W3Atom);
        private static readonly XName _xnameLink = XName.Get("link", W3Atom);
        private static readonly XName _xnameProperties = XName.Get("properties", MetadataNS);
        private static readonly XName _xnameId = XName.Get("Id", DataServicesNS);
        private static readonly XName _xnameVersion = XName.Get("Version", DataServicesNS);
        private static readonly XName _xnameSummary = XName.Get("summary", W3Atom);
        private static readonly XName _xnameDescription = XName.Get("Description", DataServicesNS);
        private static readonly XName _xnameIconUrl = XName.Get("IconUrl", DataServicesNS);
        private static readonly XName _xnameLicenseUrl = XName.Get("LicenseUrl", DataServicesNS);
        private static readonly XName _xnameProjectUrl = XName.Get("ProjectUrl", DataServicesNS);
        private static readonly XName _xnameTags = XName.Get("Tags", DataServicesNS);
        private static readonly XName _xnameReportAbuseUrl = XName.Get("ReportAbuseUrl", DataServicesNS);
        private static readonly XName _xnameDependencies = XName.Get("Dependencies", DataServicesNS);
        private static readonly XName _xnameRequireLicenseAcceptance = XName.Get("RequireLicenseAcceptance", DataServicesNS);
        private static readonly XName _xnameDownloadCount = XName.Get("DownloadCount", DataServicesNS);
        private static readonly XName _xnameCreated = XName.Get("Created", DataServicesNS);
        private static readonly XName _xnameLastEdited = XName.Get("LastEdited", DataServicesNS);
        private static readonly XName _xnamePublished = XName.Get("Published", DataServicesNS);
        private static readonly XName _xnameName = XName.Get("name", W3Atom);
        private static readonly XName _xnameAuthor = XName.Get("author", W3Atom);
        private static readonly XName _xnamePackageHash = XName.Get("PackageHash", DataServicesNS);
        private static readonly XName _xnamePackageHashAlgorithm = XName.Get("PackageHashAlgorithm", DataServicesNS);
        private static readonly XName _xnameMinClientVersion = XName.Get("MinClientVersion", DataServicesNS);

        /// <summary>
        /// Finds all entries on the page and parses them
        /// </summary>
        public static IEnumerable<V2FeedPackageInfo> ParsePage(XDocument doc)
        {
            MetadataReferenceCache metadataCache = new MetadataReferenceCache();

            if (doc.Root.Name == _xnameEntry)
            {
                return new List<V2FeedPackageInfo> { ParsePackage(doc.Root, metadataCache) };
            }
            else
            {
                return doc.Root.Elements(_xnameEntry)
                    .Select(x => ParsePackage(x, metadataCache));
            }
        }

        private static V2FeedPackageInfo ParsePackage(XElement element, MetadataReferenceCache metadataCache)
        {            
            var properties = element.Element(_xnameProperties);
            var idElement = properties.Element(_xnameId);
            var titleElement = element.Element(_xnameTitle);

            // If 'Id' element exist, use its value as accurate package Id
            // Otherwise, use the value of 'title' if it exist
            // Use the given Id as final fallback if all elements above don't exist
            string identityId = metadataCache.GetString(idElement?.Value ?? titleElement?.Value);
            string versionString = properties.Element(_xnameVersion).Value;
            NuGetVersion version = metadataCache.GetVersion(metadataCache.GetString(versionString));
            string downloadUrl = metadataCache.GetString(element.Element(_xnameContent).Attribute("src").Value);

            string title = metadataCache.GetString(titleElement?.Value);
            string summary = metadataCache.GetString(GetString(element, _xnameSummary));
            string description = metadataCache.GetString(GetString(properties, _xnameDescription));
            string iconUrl = metadataCache.GetString(GetString(properties, _xnameIconUrl));
            string licenseUrl = metadataCache.GetString(GetString(properties, _xnameLicenseUrl));
            string projectUrl = metadataCache.GetString(GetString(properties, _xnameProjectUrl));
            string reportAbuseUrl = metadataCache.GetString(GetString(properties, _xnameReportAbuseUrl));
            string tags = metadataCache.GetString(GetString(properties, _xnameTags));
            string dependencies = metadataCache.GetString(GetString(properties, _xnameDependencies));

            string downloadCount = metadataCache.GetString(GetString(properties, _xnameDownloadCount));
            bool requireLicenseAcceptance = StringComparer.OrdinalIgnoreCase.Equals(bool.TrueString, GetString(properties, _xnameRequireLicenseAcceptance));

            string packageHash = metadataCache.GetString(GetString(properties, _xnamePackageHash));
            string packageHashAlgorithm = metadataCache.GetString(GetString(properties, _xnamePackageHashAlgorithm));

            NuGetVersion minClientVersion = null;

            var minClientVersionString = GetString(properties, _xnameMinClientVersion);
            if (!string.IsNullOrEmpty(minClientVersionString))
            {
                if (NuGetVersion.TryParse(minClientVersionString, out minClientVersion))
                {
                    minClientVersion = metadataCache.GetVersion(minClientVersionString);
                }
            }

            DateTimeOffset? created = GetDate(properties, _xnameCreated);
            DateTimeOffset? lastEdited = GetDate(properties, _xnameLastEdited);
            DateTimeOffset? published = GetDate(properties, _xnamePublished);

            IEnumerable<string> owners = null;
            IEnumerable<string> authors = null;

            var authorNode = element.Element(_xnameAuthor);
            if (authorNode != null)
            {
                authors = authorNode.Elements(_xnameName).Select(e => metadataCache.GetString(e.Value));
            }

            return new V2FeedPackageInfo(new PackageIdentity(identityId, version), title, summary, description, authors,
                owners, iconUrl, licenseUrl, projectUrl, reportAbuseUrl, tags, created, lastEdited, published,
                dependencies, requireLicenseAcceptance, downloadUrl, downloadCount, packageHash, packageHashAlgorithm,
                minClientVersion);
        }

        /// <summary>
        /// Retrieve an XML <see cref="string"/> value safely
        /// </summary>
        private static string GetString(XElement parent, XName childName)
        {
            string value = null;

            if (parent != null)
            {
                XElement child = parent.Element(childName);

                if (child != null)
                {
                    value = child.Value;
                }
            }

            return value;
        }

        /// <summary>
        /// Retrieve an XML <see cref="DateTimeOffset"/> value safely
        /// </summary>
        private static DateTimeOffset? GetDate(XElement parent, XName childName)
        {
            var dateString = GetString(parent, childName);

            DateTimeOffset date;
            if (DateTimeOffset.TryParse(dateString, out date))
            {
                return date;
            }

            return null;
        }
    }
}