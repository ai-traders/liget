using System;
using System.Collections.Generic;
using System.Globalization;
using NuGet.Versioning;

namespace NuGet
{
    public static class VersionExtensions
    {
        public static bool IsSemVer2(this SemanticVersion v) {
            return NuGetVersion.Parse(v.ToFullString()).IsSemVer2;
        }

        public static Func<IPackage, bool> ToDelegate(this VersionRange versionInfo)
        {
            if (versionInfo == null)
            {
                throw new ArgumentNullException("versionInfo");
            }
            return versionInfo.ToDelegate<IPackage>(p => p.Version);
        }

        public static Func<T, bool> ToDelegate<T>(this VersionRange versionInfo, Func<T, SemanticVersion> extractor)
        {
            if (versionInfo == null)
            {
                throw new ArgumentNullException("versionInfo");
            }
            if (extractor == null)
            {
                throw new ArgumentNullException("extractor");
            }

            return p =>
            {
                SemanticVersion version = extractor(p);
                bool condition = true;
                if (versionInfo.MinVersion != null)
                {
                    if (versionInfo.IsMinInclusive)
                    {
                        condition = condition && version >= versionInfo.MinVersion;
                    }
                    else
                    {
                        condition = condition && version > versionInfo.MinVersion;
                    }
                }

                if (versionInfo.MaxVersion != null)
                {
                    if (versionInfo.IsMaxInclusive)
                    {
                        condition = condition && version <= versionInfo.MaxVersion;
                    }
                    else
                    {
                        condition = condition && version < versionInfo.MaxVersion;
                    }
                }

                return condition;
            };
        }

        /// <summary>
        /// Determines if the specified version is within the version spec
        /// </summary>
        public static bool Satisfies(this VersionRange versionSpec, SemanticVersion version)
        {
            // The range is unbounded so return true
            if (versionSpec == null)
            {
                return true;
            }
            return versionSpec.ToDelegate<SemanticVersion>(v => v)(version);
        }

        // was in core SemanticVersion
        private static Version NormalizeVersionValue(Version version)
        {
            return new Version(version.Major,
                               version.Minor,
                               Math.Max(version.Build, 0),
                               Math.Max(version.Revision, 0));
        }

        // TODO use something new instead
        // public static IEnumerable<string> GetComparableVersionStrings(this SemanticVersion version)
        // {
        //     //FIXME changed, might not work in all cases
        //     Version coreVersion = Version.Parse(version.ToNormalizedString());
        //     string specialVersion = String.IsNullOrEmpty(version.SpecialVersion) ? String.Empty : "-" + version.SpecialVersion;

        //     string originalVersion = version.ToString();
        //     string[] originalVersionComponents = version.GetOriginalVersionComponents();

        //     var paths = new LinkedList<string>();

        //     if (coreVersion.Revision == 0)
        //     {
        //         if (coreVersion.Build == 0)
        //         {
        //             string twoComponentVersion = String.Format(
        //                 CultureInfo.InvariantCulture,
        //                 "{0}.{1}{2}",
        //                 originalVersionComponents[0],
        //                 originalVersionComponents[1],
        //                 specialVersion);

        //             AddVersionToList(originalVersion, paths, twoComponentVersion);
        //         }

        //         string threeComponentVersion = String.Format(
        //             CultureInfo.InvariantCulture,
        //             "{0}.{1}.{2}{3}",
        //             originalVersionComponents[0],
        //             originalVersionComponents[1],
        //             originalVersionComponents[2],
        //             specialVersion);

        //         AddVersionToList(originalVersion, paths, threeComponentVersion);
        //     }

        //     string fullVersion = String.Format(
        //            CultureInfo.InvariantCulture,
        //            "{0}.{1}.{2}.{3}{4}",
        //            originalVersionComponents[0],
        //            originalVersionComponents[1],
        //            originalVersionComponents[2],
        //            originalVersionComponents[3],
        //            specialVersion);

        //     AddVersionToList(originalVersion, paths, fullVersion);

        //     return paths;
        // }

        private static void AddVersionToList(string originalVersion, LinkedList<string> paths, string nextVersion)
        {
            if (nextVersion.Equals(originalVersion, StringComparison.OrdinalIgnoreCase))
            {
                // IMPORTANT: we want to put the original version string first in the list. 
                // This helps the DataServicePackageRepository reduce the number of requests
                // int the Exists() and FindPackage() methods.
                paths.AddFirst(nextVersion);
            }
            else
            {
                paths.AddLast(nextVersion);
            }
        }
    }
}