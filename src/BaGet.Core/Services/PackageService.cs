using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaGet.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace BaGet.Core.Services
{
    public class PackageService : IPackageService
    {
        private readonly IContext _context;

        public PackageService(IContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<PackageAddResult> AddAsync(Package package)
        {
            try
            {
                _context.Packages.Add(package);

                await _context.SaveChangesAsync();

                return PackageAddResult.Success;
            }
            catch (DbUpdateException e)
                when (_context.IsUniqueConstraintViolationException(e))
            {
                return PackageAddResult.PackageAlreadyExists;
            }
        }

        public Task<bool> ExistsAsync(PackageIdentity pid) {
            string id = pid.Id;
            NuGetVersion version = pid.Version;
            return _context.Packages
                .Where(p => p.Id == id)
                .Where(p => p.VersionString == version.ToNormalizedString())
                .AnyAsync();
        }

        public async Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted = false, bool includeDeps = false)
        {
            var query = _context.Packages.Where(p => p.Id == id);

            if (!includeUnlisted)
            {
                query = query.Where(p => p.Listed);
            }

            if(includeDeps)
            {
                query = query.Include(p => p.Dependencies);
            }

            return (await query.ToListAsync()).AsReadOnly();
        }

        public Task<Package> FindOrNullAsync(PackageIdentity pid, bool includeUnlisted = false, bool includeDeps = false)
        {
            string id = pid.Id;
            NuGetVersion version = pid.Version;
            var query = _context.Packages
                .Where(p => p.Id == id)
                .Where(p => p.VersionString == version.ToNormalizedString());

            if (!includeUnlisted)
            {
                query = query.Where(p => p.Listed);
            }

            if(includeDeps)
            {
                query = query.Include(p => p.Dependencies);
            }

            return query.FirstOrDefaultAsync();
        }

        public Task<bool> UnlistPackageAsync(PackageIdentity pid)
        {
            return TryUpdatePackageAsync(pid, p => p.Listed = false);
        }

        public Task<bool> RelistPackageAsync(PackageIdentity pid)
        {
            return TryUpdatePackageAsync(pid, p => p.Listed = true);
        }

        public Task<bool> IncrementDownloadCountAsync(PackageIdentity pid)
        {
            return TryUpdatePackageAsync(pid, p => p.Downloads += 1);
        }

        public async Task<bool> HardDeletePackageAsync(PackageIdentity pid)
        {
            string id = pid.Id;
            NuGetVersion version = pid.Version;
            var package = await _context.Packages
                .Where(p => p.Id == id)
                .Where(p => p.VersionString == version.ToNormalizedString())
                .Include(p => p.Dependencies)
                .FirstOrDefaultAsync();

            if (package == null)
            {
                return false;
            }

            _context.Packages.Remove(package);
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<bool> TryUpdatePackageAsync(PackageIdentity pid, Action<Package> action)
        {
            string id = pid.Id;
            NuGetVersion version = pid.Version;
            var package = await _context.Packages
                .Where(p => p.Id == id)
                .Where(p => p.VersionString == version.ToNormalizedString())
                .FirstOrDefaultAsync();

            if (package != null)
            {
                action(package);
                await _context.SaveChangesAsync();

                return true;
            }

            return false;
        }
    }
}
