// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Generic;
using System.Linq;
using NuGet;
using NuGet.Protocol;
using NuGet.Versioning;

namespace LiGet.NuGet.Server.Infrastructure
{
    public interface IServerPackageRepository : IServiceBasedRepository
    {
        void ClearCache();

        void RemovePackage(string packageId, SemanticVersion version);

        IQueryable<ServerPackage> GetPackages(ClientCompatibility compatibility);

        IEnumerable<ServerPackage> FindPackagesById(string packageId, ClientCompatibility compatibility);

        IQueryable<LocalPackageInfo> Search(
            string searchTerm,
            IEnumerable<string> targetFrameworks,
            bool allowPrereleaseVersions,
            ClientCompatibility compatibility);
    }
}
