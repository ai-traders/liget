namespace LiGet.OData
{
    public class PackageUrls
    {
        private readonly string serviceBaseUrl;
        private readonly string resourceIdUrl;
        private readonly string packageContentUrl;

        public PackageUrls(string serviceBaseUrl, string resourceIdUrl, string packageContentUrl) {
            this.serviceBaseUrl = serviceBaseUrl;
            this.resourceIdUrl = resourceIdUrl;
            this.packageContentUrl = packageContentUrl;
        }

        public string ServiceBaseUrl => serviceBaseUrl;

        public string ResourceIdUrl => resourceIdUrl;

        public string PackageContentUrl => packageContentUrl;
    }
}