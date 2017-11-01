using System;
using System.Collections.Generic;

namespace LiGet.Cache.Proxy
{
    public class UrlReplacementsProvider : IUrlReplacementsProvider
    {
        private string v3baseAddress;
        private string v3flatcontainer;

        public UrlReplacementsProvider(ICachingProxyConfig config) {
            //TODO This implementation is too simple, actually we should make a request to index.json and 
            // then look through all service endpoints. 
            // Currently it works by accident that all URLs are starting with v3
            v3baseAddress = config.V3NugetIndexSource.Replace("/index.json","");
            v3flatcontainer = config.V3NugetIndexSource.Replace("v3/index.json","v3-flatcontainer");
        }

        public Uri GetOriginUri(Uri fullLiGetUrl)
        {
            string absoluteLiget = fullLiGetUrl.AbsoluteUri;
            string ligetBase = new Uri(fullLiGetUrl.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped) + "/api/cache/v3").AbsoluteUri;
            return new Uri(absoluteLiget.Replace(ligetBase, v3baseAddress));
        }

        public Dictionary<string, string> GetReplacements(string ligetV3Url)
        {
            return new Dictionary<string, string>() {
                { v3baseAddress, ligetV3Url }
            };
        }
    }
}