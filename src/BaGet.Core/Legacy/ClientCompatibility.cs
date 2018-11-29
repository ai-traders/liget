using System;
using NuGet.Versioning;

namespace BaGet.Core.Legacy
{
    public class ClientCompatibility
    {
        /// <summary>
        /// A set of client compatibilities with yielding the maximum set of packages.
        /// </summary>
        public static readonly ClientCompatibility Max = new ClientCompatibility(
            semVerLevel: SemanticVersion.Parse("2.0.0"));

        /// <summary>
        /// A set of client compatibilities with yielding the minimum set of packages.
        /// </summary>
        public static readonly ClientCompatibility Default = new ClientCompatibility(
            semVerLevel: SemanticVersion.Parse("1.0.0"));

        public ClientCompatibility(SemanticVersion semVerLevel)
        {
            if (semVerLevel == null)
            {
                throw new ArgumentNullException(nameof(semVerLevel));
            }

            SemVerLevel = semVerLevel;
            AllowSemVer2 = semVerLevel.Major >= 2;
        }

        public SemanticVersion SemVerLevel { get; }

        public bool AllowSemVer2 { get; }
    }
}