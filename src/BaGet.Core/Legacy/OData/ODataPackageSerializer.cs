using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace BaGet.Core.Legacy.OData
{
    public static class XmlNamespaces
    {
        public static readonly XNamespace xmlns = "http://www.w3.org/2005/Atom";
        //public static readonly XNamespace baze = "https://www.nuget.org/api/v2/curated-feeds/microsoftdotnet";
        public static readonly XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        public static readonly XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        public static readonly XNamespace georss = "http://www.georss.org/georss";
        public static readonly XNamespace gml = "http://www.opengis.net/gml";
    }

    public static class XmlElements
    {
        public static readonly XName feed = XmlNamespaces.xmlns + "feed";
        public static readonly XName entry = XmlNamespaces.xmlns + "entry";
        public static readonly XName title = XmlNamespaces.xmlns + "title";
        public static readonly XName author = XmlNamespaces.xmlns + "author";
        public static readonly XName name = XmlNamespaces.xmlns + "name";
        public static readonly XName link = XmlNamespaces.xmlns + "link";
        public static readonly XName id = XmlNamespaces.xmlns + "id";
        public static readonly XName content = XmlNamespaces.xmlns + "content";

        public static readonly XName m_count = XmlNamespaces.m + "count";
        public static readonly XName m_properties = XmlNamespaces.m + "properties";

        public static readonly XName d_Id = XmlNamespaces.d + "Id";
        public static readonly XName d_Title = XmlNamespaces.d + "Title";
        public static readonly XName d_Version = XmlNamespaces.d + "Version";
        public static readonly XName d_NormalizedVersion = XmlNamespaces.d + "NormalizedVersion";
        public static readonly XName d_Authors = XmlNamespaces.d + "Authors";
        public static readonly XName d_Copyright = XmlNamespaces.d + "Copyright";
        public static readonly XName d_Dependencies = XmlNamespaces.d + "Dependencies";
        public static readonly XName d_Description = XmlNamespaces.d + "Description";
        public static readonly XName d_IconUrl = XmlNamespaces.d + "IconUrl";
        public static readonly XName d_LicenseUrl = XmlNamespaces.d + "LicenseUrl";
        public static readonly XName d_ProjectUrl = XmlNamespaces.d + "ProjectUrl";
        public static readonly XName d_Tags = XmlNamespaces.d + "Tags";
        public static readonly XName d_ReportAbuseUrl = XmlNamespaces.d + "ReportAbuseUrl";
        public static readonly XName d_RequireLicenseAcceptance = XmlNamespaces.d + "RequireLicenseAcceptance";
        public static readonly XName d_DownloadCount = XmlNamespaces.d + "DownloadCount";
        public static readonly XName d_Created = XmlNamespaces.d + "Created";
        public static readonly XName d_LastEdited = XmlNamespaces.d + "LastEdited";
        public static readonly XName d_Published = XmlNamespaces.d + "Published";
        public static readonly XName d_PackageHash = XmlNamespaces.d + "PackageHash";
        public static readonly XName d_PackageHashAlgorithm = XmlNamespaces.d + "PackageHashAlgorithm";
        public static readonly XName d_MinClientVersion = XmlNamespaces.d + "MinClientVersion";
        public static readonly XName d_PackageSize = XmlNamespaces.d + "PackageSize";

        public static readonly XName baze = XNamespace.Xmlns + "base";
        public static readonly XName m = XNamespace.Xmlns + "m";
        public static readonly XName d = XNamespace.Xmlns + "d";
        public static readonly XName georss = XNamespace.Xmlns + "georss";
        public static readonly XName gml = XNamespace.Xmlns + "gml";
    }

    public class ODataPackageSerializer : IODataPackageSerializer
    {
        public void Serialize(Stream outputStream, ODataPackage package, string serviceBaseUrl, string resourceIdUrl, string packageContentUrl)
        {
            var doc = new XElement(XmlElements.entry,
                new XAttribute(XmlElements.baze, XNamespace.Get(serviceBaseUrl)),
                new XAttribute(XmlElements.m, XmlNamespaces.m),
                new XAttribute(XmlElements.d, XmlNamespaces.d),
                new XAttribute(XmlElements.georss, XmlNamespaces.georss),
                new XAttribute(XmlElements.gml, XmlNamespaces.gml),
                new XElement(XmlElements.id, resourceIdUrl),
                new XElement(XmlElements.title, package.Title),
                new XElement(XmlElements.author, new XElement(XmlElements.name, package.Authors)),
                new XElement(
                    XmlElements.content,
                    new XAttribute("type", "application/zip"),
                    new XAttribute("src", packageContentUrl)
                ),
                GetProperties(package)
            );
            var writer = XmlWriter.Create(outputStream);
            doc.WriteTo(writer);
            writer.Flush();            
        }

        private static XElement GetProperties(ODataPackage package)
        {
            return new XElement(
                                XmlElements.m_properties,
                                new XElement(XmlElements.d_Id, package.Id),
                                new XElement(XmlElements.d_Title, package.Title),
                                new XElement(XmlElements.d_Version, package.Version),
                                new XElement(XmlElements.d_NormalizedVersion, package.NormalizedVersion),
                                new XElement(XmlElements.d_Authors, package.Authors),
                                new XElement(XmlElements.d_Copyright, package.Copyright),
                                new XElement(XmlElements.d_Dependencies, package.Dependencies),
                                new XElement(XmlElements.d_Description, package.Description),
                                new XElement(XmlElements.d_DownloadCount, package.DownloadCount), //TODO m:type="Edm.Int32"
                                new XElement(XmlElements.d_LastEdited, package.LastUpdated),
                                new XElement(XmlElements.d_Published, package.Published),
                                new XElement(XmlElements.d_PackageHash, package.PackageHash),
                                new XElement(XmlElements.d_PackageHashAlgorithm, package.PackageHashAlgorithm),
                                new XElement(XmlElements.d_PackageSize, package.PackageSize),
                                new XElement(XmlElements.d_ProjectUrl, package.ProjectUrl),
                                new XElement(XmlElements.d_IconUrl, package.IconUrl),
                                new XElement(XmlElements.d_LicenseUrl, package.LicenseUrl),
                                //new XElement(XmlElements.d_ReportAbuseUrl, package.ReportAbuseUrl),
                                new XElement(XmlElements.d_Tags, package.Tags),
                                new XElement(XmlElements.d_RequireLicenseAcceptance, package.RequireLicenseAcceptance)
                            );
        }

        public void Serialize(Stream outputStream, IEnumerable<PackageWithUrls> packages, string serviceBaseUrl)
        {
            var list = packages.ToList();
            var doc = new XElement(
                XmlElements.feed,
                new XAttribute(XmlElements.baze, XNamespace.Get(serviceBaseUrl)),
                new XAttribute(XmlElements.m, XmlNamespaces.m),
                new XAttribute(XmlElements.d, XmlNamespaces.d),
                new XAttribute(XmlElements.georss, XmlNamespaces.georss),
                new XAttribute(XmlElements.gml, XmlNamespaces.gml),
                new XElement(XmlElements.m_count, list.Count),
                list.Select(x =>
                    new XElement(
                        XmlElements.entry,
                        new XElement(XmlElements.id, x.ResourceIdUrl),
                        new XElement(XmlElements.title, x.Pkg.Title),
                        new XElement(XmlElements.author, new XElement(XmlElements.name, x.Pkg.Authors)),
                        new XElement(
                            XmlElements.content,
                            new XAttribute("type", "application/zip"),
                            new XAttribute("src", x.PackageContentUrl)
                        ),
                        GetProperties(x.Pkg)
                    )
                )
            );
            var writer = XmlWriter.Create(outputStream);
            doc.WriteTo(writer);
            writer.Flush();
        }
    }
}
