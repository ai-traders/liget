using LiGet.Models;

namespace LiGet.OData
{
    public class ODataPackageResponse
    {
        private readonly ODataPackage package;
        private readonly PackageUrls urls;
        public ODataPackageResponse(ODataPackage package, PackageUrls urls)
        {
            this.urls = urls;
            this.package = package;

        }

        public ODataPackage Package => package;

        public PackageUrls Urls => urls;
    }
}