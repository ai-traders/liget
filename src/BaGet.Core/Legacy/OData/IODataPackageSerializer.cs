using System.Collections.Generic;
using System.IO;

namespace BaGet.Core.Legacy.OData
{
    public interface IODataPackageSerializer
    {
        void Serialize(Stream outputStream, ODataPackage package, string serviceBaseUrl, string resourceIdUrl, string packageContentUrl);

        void Serialize(Stream outputStream, IEnumerable<PackageWithUrls> package, string serviceBaseUrl);
    }
}
