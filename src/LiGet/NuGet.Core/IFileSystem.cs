using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NuGet.Packaging;

namespace NuGet
{
    public interface IFileSystem
    {
        string Root { get; }
        void DeleteDirectory(string path, bool recursive);
        IEnumerable<string> GetFiles(string path, string filter, bool recursive);
        IEnumerable<string> GetDirectories(string path);
        string GetFullPath(string path);
        void DeleteFile(string path);
        void DeleteFiles(IEnumerable<IPackageFile> files, string rootDir);

        bool FileExists(string path);
        bool DirectoryExists(string path);
        void AddFile(string path, Stream stream);
        void AddFile(string path, Action<Stream> writeToStream);
        void AddFiles(IEnumerable<IPackageFile> files, string rootDir);

        void MakeFileWritable(string path);
        void MoveFile(string source, string destination);
        Stream CreateFile(string path);
        Stream OpenFile(string path);
        DateTimeOffset GetLastModified(string path);
        DateTimeOffset GetCreated(string path);
        DateTimeOffset GetLastAccessed(string path);
        void MakeDirectoryForFile(string nupkgPath);
    }

    public static class FileSystemExtensions
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(FileSystemExtensions));

        internal static IEnumerable<string> GetDirectoriesSafe(this IFileSystem fileSystem, string path)
        {
            try
            {
                return fileSystem.GetDirectories(path);
            }
            catch (Exception e)
            {
                _log.Warn("Failed to get directories",e);
            }

            return Enumerable.Empty<string>();
        }

        // for tests
        public static void AddFile(this IFileSystem fileSystem, string path, string content) {
            fileSystem.AddFile(path,new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }

        public static string ReadAllText(this IFileSystem fileSystem, string path)
        {
            return new StreamReader(fileSystem.OpenFile(path)).ReadToEnd();
        }
    }
}