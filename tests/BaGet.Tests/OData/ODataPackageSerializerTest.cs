using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BaGet.Core.Legacy.OData;
using NuGet.Versioning;
using Xunit;

namespace BaGet.Tests.OData
{
    public class ODataPackageSerializerTest : IDisposable
    {
        ODataPackageSerializer serializer = new ODataPackageSerializer();
        private Stream outputStream = new MemoryStream();
        private string serviceUrl = "http://repo";
        private string resourceUrl = "http://repo/pkg/1.0";
        private string packageContentUrl = "http://repo/contents/pkg/1.0";

        public ODataPackageSerializerTest() {           
        }

        public void Dispose() {
             outputStream.Dispose();
        }

        [Fact]
        public void SerializesMinimalPackage() {
            var pkg = new ODataPackage() {
                Id = "Minimal",
                Version = "1.0.0"
            };
            serializer.Serialize(outputStream, pkg, serviceUrl, resourceUrl, packageContentUrl);
            var feed = XmlFeedHelper.ParsePage(readDocument());
            var entry = Assert.Single(feed);
            Assert.Equal("Minimal", entry.Id);
            Assert.Equal(NuGetVersion.Parse("1.0.0"), entry.Version);
        }

        [Fact]
        public void SerializesEmptyCollection() {
            serializer.Serialize(outputStream, new List<PackageWithUrls>(), serviceUrl);
            var feed = XmlFeedHelper.ParsePage(readDocument());
            Assert.Empty(feed);
        }

         [Fact]
        public void SerializesCollectionOf2() {
            serializer.Serialize(outputStream, new PackageWithUrls[] {  
                new PackageWithUrls(new ODataPackage() {
                    Id = "Minimal",
                    Version = "1.0.0"
                }, resourceUrl, packageContentUrl),
                new PackageWithUrls(new ODataPackage() {
                    Id = "Minimal",
                    Version = "1.1.0"
                }, "http://repo/pkg/1.1", "http://repo/contents/pkg/1.1") 
            }, serviceUrl);
            var feed = XmlFeedHelper.ParsePage(readDocument()).ToList();
            Assert.Equal(2, feed.Count);
            Assert.Equal(new [] { "1.0.0",  "1.1.0" }, feed.Select(f => f.Version.OriginalVersion));
        }

        private XDocument readDocument()
        {
            outputStream.Seek(0, SeekOrigin.Begin);
            return XDocument.Load(outputStream);
        }
    }
}