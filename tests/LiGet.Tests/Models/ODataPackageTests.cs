
using Xunit;

namespace LiGet.Models.Tests
{
    public class ODataPackageTests
    {
        [Fact]
        public void PackageEquals()
        {
            var a1 = new ODataPackage() { Id = "a", Version = "1.0" };
            var a2 = new ODataPackage() {Id = "a", Version = "1.0" };

            Assert.Equal(a1, a2);
        }

        [Fact]
        public void EqualsIgnoresOtherProperties()
        {
            var a1 = new ODataPackage() { Id = "a", Version = "1.0", Description = "ignore me" };
            var a2 = new ODataPackage() { Id = "a", Version = "1.0", Description = "ignore me and me too" };

            Assert.Equal(a1, a2);
        }

        [Fact]
        public void DifferentIdNotEqual()
        {
            var a1 = new ODataPackage() { Id = "a", Version = "1.0" };
            var a2 = new ODataPackage() { Id = "b", Version = "1.0" };

            Assert.NotEqual(a1, a2);
        }

        [Fact]
        public void DifferentVersionNotEqual()
        {
            var a1 = new ODataPackage() { Id = "a", Version = "1.0" };
            var a2 = new ODataPackage() { Id = "a", Version = "2.0" };

            Assert.NotEqual(a1, a2);
        }
    }
}
