using System;
using System.Collections.Generic;
using System.IO;
using LiGet.Models;
using LiGet.NuGet.Server.Infrastructure;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

namespace LiGet
{
    public class HostedPackage {
        private ODataPackage _packageInfo;
        // TODO accessor to nupkg, and anything else that web service may need
        public HostedPackage(ODataPackage packageInfo) {
            _packageInfo = packageInfo;
        }

        public ODataPackage PackageInfo { get => _packageInfo; }
    }

    /// <summary>
    /// Provides package queries and operations for the web API.
    /// </summary>
    public interface IPackageService
    {
        IEnumerable<HostedPackage> FindPackagesById(string id, ClientCompatibility compatibility);

        HostedPackage FindPackage(string packageId, NuGetVersion version);

        void PushPackage(Stream value);

        Func<Stream> GetStream(PackageIdentity packageIdentity);
    }
}