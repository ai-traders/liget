using System.IO;
using LiGet.Models;

namespace LiGet.OData
{
    public interface IODataPackageSerializer
    {
        void Serialize(Stream outputStream, ODataPackage package, string resourceIdUrl, string packageContentUrl);
    }
}