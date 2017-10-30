using LiGet.Models;

namespace LiGet.OData
{
    public class PackageWithUrls
    {

        private readonly string resourceIdUrl;
        private readonly string packageContentUrl;
        private readonly ODataPackage pkg;

        public PackageWithUrls(ODataPackage pkg, string resourceIdUrl, string packageContentUrl)
        {
            this.pkg = pkg;
            this.resourceIdUrl = resourceIdUrl;
            this.packageContentUrl = packageContentUrl;
        }

        public string ResourceIdUrl => resourceIdUrl;

        public string PackageContentUrl => packageContentUrl;

        public ODataPackage Pkg => pkg;
    }
}