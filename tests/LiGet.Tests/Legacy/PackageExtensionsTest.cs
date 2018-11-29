using LiGet.Legacy;
using Xunit;

namespace LiGet.Tests.Legacy
{
    public class PackageExtensionsTest
    {
        [Fact]
        public void ConvertDependenciesToStringWhenNull() 
        {
            Assert.Null(PackageExtensions.ToDependenciesString(null));
        }

        [Fact]
        public void ConvertDependenciesToStringWhenEmpty() 
        {
            Assert.Null(PackageExtensions.ToDependenciesString(new Entities.PackageDependency[0]));
        }

        [Fact]
        public void ConvertDependenciesToStringWhenFrameworkSpecific() 
        {
            Assert.Equal("Consul:[0.7.2.6, ):.NETStandard2.0|YamlDotNet:[5.1.0, ):.NETStandard2.0",
                PackageExtensions.ToDependenciesString(new Entities.PackageDependency[2] {
                    new Entities.PackageDependency() { Id = "Consul", VersionRange = "[0.7.2.6, )", TargetFramework = ".NETStandard2.0" },
                    new Entities.PackageDependency() { Id = "YamlDotNet", VersionRange="[5.1.0, )", TargetFramework = ".NETStandard2.0" }
                }));
        }

        [Fact]
        public void ConvertDependenciesToStringWhenDependsOnFramework() 
        {
            Assert.Equal("::.NETStandard2.0",
                PackageExtensions.ToDependenciesString(new Entities.PackageDependency[1] {
                    new Entities.PackageDependency() { TargetFramework = ".NETStandard2.0" }
                }));
        }
    }
}