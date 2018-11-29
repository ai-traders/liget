using System;
using System.Collections.Generic;
using System.Linq;
using BaGet.Web.Models;
using NuGet.Frameworks;
using Xunit;

namespace BaGet.Tests.Models
{
    public class CatalogEntryTest
    {
        List<NuGet.Packaging.PackageDependencyGroup> nugetFrameworkDeps = new List<NuGet.Packaging.PackageDependencyGroup>() {
            new NuGet.Packaging.PackageDependencyGroup(NuGetFramework.Parse("netstandard2.0"), new NuGet.Packaging.Core.PackageDependency[0]),
            new NuGet.Packaging.PackageDependencyGroup(NuGetFramework.Parse("net35"), new NuGet.Packaging.Core.PackageDependency[0])
        };

        List<Core.Entities.PackageDependency> frameworkDeps = new List<Core.Entities.PackageDependency>() {
            new Core.Entities.PackageDependency() { TargetFramework="netstandard2.0" },
            new Core.Entities.PackageDependency() { TargetFramework="net35" }
        };

        List<Core.Entities.PackageDependency> packagePerFrameworkDeps = new List<Core.Entities.PackageDependency>() {
            new Core.Entities.PackageDependency() { TargetFramework="netstandard2.0", Id="dep1"  },
            new Core.Entities.PackageDependency() { TargetFramework="netstandard2.0", Id="depX"  },
            new Core.Entities.PackageDependency() { TargetFramework="net35", Id="dep2" }
        };

        List<NuGet.Packaging.PackageDependencyGroup> nugetPackagePerFrameworkDeps = new List<NuGet.Packaging.PackageDependencyGroup>() {
            new NuGet.Packaging.PackageDependencyGroup(NuGetFramework.Parse("netstandard2.0"), new NuGet.Packaging.Core.PackageDependency[2] {
                new NuGet.Packaging.Core.PackageDependency("dep1"),
                new NuGet.Packaging.Core.PackageDependency("depX"),
            }),
            new NuGet.Packaging.PackageDependencyGroup(NuGetFramework.Parse("net35"), new NuGet.Packaging.Core.PackageDependency[1] {
                new NuGet.Packaging.Core.PackageDependency("dep2"),
            })
        };

        List<Core.Entities.PackageDependency> anyFrameworkPackageDeps = new List<Core.Entities.PackageDependency>() {
            new Core.Entities.PackageDependency() { Id="dep1"  },
            new Core.Entities.PackageDependency() { Id="dep2"  },
        };

        List<NuGet.Packaging.PackageDependencyGroup> nugetAnyFrameworkPackageDeps = new List<NuGet.Packaging.PackageDependencyGroup>() {
            new NuGet.Packaging.PackageDependencyGroup(NuGetFramework.AnyFramework, new NuGet.Packaging.Core.PackageDependency[2] {
                new NuGet.Packaging.Core.PackageDependency("dep1"),
                new NuGet.Packaging.Core.PackageDependency("dep2"),
            })
        };

        private static System.Uri GetRegistrationUrl(string packageid) {
            return new Uri($"http://packages/{packageid}.json");
        }

        [Fact]
        public void EntityToDependencyGroups_ShouldIncludeFrameworkDependencies()
        {
            var result = CatalogEntry.ToDependencyGroups(frameworkDeps, "http://catalog/package.json", GetRegistrationUrl);
            Assert.All(result, r => Assert.Null(r.Dependencies));
            Assert.Equal(new string[] { "netstandard2.0", "net35" }, result.Select(s => s.TargetFramework));
        }

        [Fact]
        public void NuGetGroupsToDependencyGroups_ShouldIncludeFrameworkDependencies()
        {
            var result = CatalogEntry.ToDependencyGroups(nugetFrameworkDeps, "http://catalog/package.json", GetRegistrationUrl);
            Assert.All(result, r => Assert.Null(r.Dependencies));
            Assert.Equal(new string[] { "netstandard2.0", "net35" }, result.Select(s => s.TargetFramework));
        }

        [Fact]
        public void EntityToDependencyGroups_ShouldGroupPerFrameworkDepsTogether()
        {
            var result = CatalogEntry.ToDependencyGroups(packagePerFrameworkDeps, "http://catalog/package.json", GetRegistrationUrl);
            var netstd2 = result.First(n => n.TargetFramework == "netstandard2.0");
            Assert.Equal(new string[] { "dep1", "depX" }, netstd2.Dependencies.Select(d => d.Id));
            var net35 = result.First(n => n.TargetFramework == "net35");
            Assert.Equal(new string[] { "dep2" }, net35.Dependencies.Select(d => d.Id));
        }

        [Fact]
        public void NuGetGroupsToDependencyGroups_ShouldGroupPerFrameworkDepsTogether()
        {
            var result = CatalogEntry.ToDependencyGroups(nugetPackagePerFrameworkDeps, "http://catalog/package.json", GetRegistrationUrl);
            var netstd2 = result.First(n => n.TargetFramework == "netstandard2.0");
            Assert.Equal(new string[] { "dep1", "depX" }, netstd2.Dependencies.Select(d => d.Id));
            var net35 = result.First(n => n.TargetFramework == "net35");
            Assert.Equal(new string[] { "dep2" }, net35.Dependencies.Select(d => d.Id));
        }

        [Fact]
        public void EntityToDependencyGroups_ShouldGroupDependenciesWithoutFrameworkTogether()
        {
            var result = CatalogEntry.ToDependencyGroups(anyFrameworkPackageDeps, "http://catalog/package.json", GetRegistrationUrl);
            var deps = result.First();
            Assert.Equal(new string[] { "dep1", "dep2" }, deps.Dependencies.Select(d => d.Id));
            Assert.All(result, r => Assert.Null(r.TargetFramework));
        }

        [Fact]
        public void NuGetGroupsToDependencyGroups_ShouldGroupDependenciesWithoutFrameworkTogether()
        {
            var result = CatalogEntry.ToDependencyGroups(nugetAnyFrameworkPackageDeps, "http://catalog/package.json", GetRegistrationUrl);
            var deps = result.First();
            Assert.Equal(new string[] { "dep1", "dep2" }, deps.Dependencies.Select(d => d.Id));
            Assert.All(result, r => Assert.Null(r.TargetFramework));
        }
    }
}