using System;
using System.IO;
using System.Xml.Linq;
using LiGet.Models;
using LiGet.OData;
using NuGet.Versioning;
using Xunit;

namespace LiGet.Tests.OData
{
    public class ODataPackageSerializerTest : IDisposable
    {
        ODataPackageSerializer serializer = new ODataPackageSerializer();
        private Stream outputStream = new MemoryStream();
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
            serializer.Serialize(outputStream, pkg, resourceUrl, packageContentUrl);
            var feed = XmlFeedHelper.ParsePage(readDocument());
            var entry = Assert.Single(feed);
            Assert.Equal("Minimal", entry.Id);
            Assert.Equal(NuGetVersion.Parse("1.0.0"), entry.Version);
        }

        private XDocument readDocument()
        {
            outputStream.Seek(0, SeekOrigin.Begin);
            return XDocument.Load(outputStream);
        }
    }
}