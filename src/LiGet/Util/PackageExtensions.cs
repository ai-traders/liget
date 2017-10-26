using System;
using LiGet.Models;
using NuGet;

namespace LiGet.Util
{
    public static class PackageExtensions
    {
        public static ODataPackage ToODataPackage(this IPackage package)
        {
            //TODO in future when integrating lucene index
            // var lucenePackage = package as LucenePackage;

            // if (lucenePackage != null)
            //     return new ODataPackage(lucenePackage);

            var dataServicePackage = package as NuGet.DataServicePackage;

            if (dataServicePackage != null)
                return new ODataPackage(dataServicePackage);

            throw new ArgumentException("Cannot convert package of type " + package.GetType() + " to ODataPackage.");
        }
    }
}