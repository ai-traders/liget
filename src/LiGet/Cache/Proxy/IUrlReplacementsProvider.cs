using System;
using System.Collections.Generic;

namespace LiGet.Cache.Proxy
{
    public interface IUrlReplacementsProvider
    {
        Dictionary<string, string> GetReplacements(string ligetV3Url);
        Uri GetOriginUri(string relativeLigetPathAfterV3);
        Uri GetOriginNupkgUri(string relativeLigetPathAfterFlatContainer);
    }
}