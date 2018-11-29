using System;
using System.IO;
using LiGet.Configuration;

namespace LiGet.Extensions
{
    public static class OptionsExtensions
    {
        public static void EnsureValid(this DatabaseOptions options)
        {
            if (options == null) ThrowMissingConfiguration(nameof(LiGetOptions.Database));

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                ThrowMissingConfiguration(
                    nameof(LiGetOptions.Database),
                    nameof(DatabaseOptions.ConnectionString));
            }
        }

        public static void EnsureValid(this StorageOptions options)
        {
            if (options == null) ThrowMissingConfiguration(nameof(LiGetOptions.Storage));
        }

        public static void EnsureValid(this FileSystemStorageOptions options)
        {
            if (options == null) ThrowMissingConfiguration(nameof(LiGetOptions.Storage));

            options.Path = string.IsNullOrEmpty(options.Path)
                ? Path.Combine(Directory.GetCurrentDirectory(), "Packages")
                : options.Path;

            // Ensure the package storage directory exists
            Directory.CreateDirectory(options.Path);
        }

        public static void EnsureValid(this SearchOptions options)
        {
            if (options == null) ThrowMissingConfiguration(nameof(LiGetOptions.Search));
        }

        public static void EnsureValid(this CacheOptions options)
        {
            if (options == null)
            {
                ThrowMissingConfiguration(nameof(LiGetOptions.Cache));
            }

            if (!options.Enabled) return;

            if (options.UpstreamIndex == null)
            {
                ThrowMissingConfiguration(
                    nameof(LiGetOptions.Cache),
                    nameof(CacheOptions.UpstreamIndex));
            }

            if (options.PackagesPath == null)
            {
                ThrowMissingConfiguration(
                    nameof(LiGetOptions.Cache),
                    nameof(CacheOptions.PackagesPath));
            }

            if (options.PackageDownloadTimeoutSeconds <= 0)
            {
                options.PackageDownloadTimeoutSeconds = 600;
            }
        }

        public static void ThrowMissingConfiguration(params string[] segments)
        {
            var name = string.Join(":", segments);

            throw new InvalidOperationException($"The '{name}' configuration is missing");
        }
    }
}
