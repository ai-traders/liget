using System;
using System.IO;

namespace BaGet.Tests.Support
{
    public class TempDir : IDisposable
    {
        private string uniqueTempFolder;

        public TempDir() {
            uniqueTempFolder = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(uniqueTempFolder);
        }

        public string UniqueTempFolder { get => uniqueTempFolder; }

        public void Dispose()
        {
            Directory.Delete(uniqueTempFolder, true);
        }
    }
}