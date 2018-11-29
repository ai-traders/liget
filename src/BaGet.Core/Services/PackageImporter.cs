using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BaGet.Core.Services
{
    public class PackageImporter
    {
        private readonly IIndexingService _indexingService;

        public PackageImporter(IIndexingService indexingService) {
            this._indexingService = indexingService;
        }

        public async Task ImportAsync(string pkgDirectory, TextWriter output)
        {
            var files = GetDirectoryFiles(pkgDirectory, "*.nupkg", SearchOption.AllDirectories, output);
            foreach (string file in files)
            {
                output.Write("Importing package {0} ", file);
                using (var uploadStream = File.OpenRead(file))
                {
                    var result = await _indexingService.IndexAsync(uploadStream, CancellationToken.None);
                    output.WriteLine(result);
                }
            }
        }

        /// <summary>
        /// A safe way to get all the files in a directory and sub directory without crashing on UnauthorizedException or PathTooLongException
        /// </summary>
        /// <param name="rootPath">Starting directory</param>
        /// <param name="patternMatch">Filename pattern match</param>
        /// <param name="searchOption">Search subdirectories or only top level directory for files</param>
        /// <returns>List of files</returns>
        public static IEnumerable<string> GetDirectoryFiles(string rootPath, string patternMatch, SearchOption searchOption, TextWriter output)
        {
            var foundFiles = Enumerable.Empty<string>();

            if (searchOption == SearchOption.AllDirectories)
            {
                try
                {
                    IEnumerable<string> subDirs = Directory.EnumerateDirectories(rootPath);
                    foreach (string dir in subDirs)
                    {
                        foundFiles = foundFiles.Concat(GetDirectoryFiles(dir, patternMatch, searchOption, output)); // Add files in subdirectories recursively to the list
                    }
                }
                catch (UnauthorizedAccessException) { 
                    output.WriteLine("Skipping {0} because of insufficient permissions", rootPath);
                }
                catch (PathTooLongException) {}
            }

            try
            {
                foundFiles = foundFiles.Concat(Directory.EnumerateFiles(rootPath, patternMatch)); // Add files from the current directory
            }
            catch (UnauthorizedAccessException) { 
                output.WriteLine("Skipping {0} because of insufficient permissions", rootPath);
            }

            return foundFiles;
        }
    }
}