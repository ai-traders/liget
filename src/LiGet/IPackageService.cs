using System.Collections.Generic;
using LiGet.Models;

namespace LiGet
{
    /// <summary>
    /// Provides package queries and operations for the web API.
    /// </summary>
    public interface IPackageService
    {
        IEnumerable<ODataPackage> FindPackagesById(string id);
    }
}