using System;

namespace NuGet
{
    internal class Constants
    {
        public static readonly DateTimeOffset Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));
        // Starting from nuget 2.0, we use a file with the special name '_._' to represent an empty folder.
        internal const string PackageEmptyFileName = "_._";
    }
}