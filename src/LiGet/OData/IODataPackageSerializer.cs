using System.Collections.Generic;
using System.IO;
using LiGet.Models;

namespace LiGet.OData
{
    public interface IODataPackageSerializer
    {
        void Serialize(Stream outputStream, ODataPackage package, string serviceBaseUrl, string resourceIdUrl, string packageContentUrl);

        void Serialize(Stream outputStream, IEnumerable<PackageWithUrls> package, string serviceBaseUrl);
    }
}