using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaGet.Core.Entities;
using BaGet.Core.Legacy.OData;
using NuGet;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

namespace BaGet.Core.Legacy
{
    public static class PackageExtensions
    {
        public static string ToDependenciesString(this IEnumerable<Entities.PackageDependency> dependencies) 
        {
            if(dependencies == null || !dependencies.Any())
                return null;

            var texts = new List<string>();
            var frameworkDeps = dependencies.Where(d => IsFrameworkDependency(d)).Select(d => d.TargetFramework).Distinct();
            foreach(var frameworkDep in frameworkDeps) {
                texts.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}", null, null, frameworkDep));
            }
            foreach (var packageDependency in dependencies.Where(d => !IsFrameworkDependency(d)))
            {
                texts.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}",
                            packageDependency.Id, 
                            packageDependency.VersionRange == null ? null : packageDependency.VersionRange, 
                            packageDependency.TargetFramework));
            }

            return string.Join("|", texts);
        }

        public static bool IsFrameworkDependency(this Entities.PackageDependency dependency)
        {
            return dependency.Id == null && dependency.VersionRange == null;
        }
    }
}
