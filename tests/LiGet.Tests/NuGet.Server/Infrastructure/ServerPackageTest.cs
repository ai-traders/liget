// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Linq;
using System.Runtime.Versioning;
using LiGet.NuGet.Server.Infrastructure;
using Moq;
using NuGet;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Xunit;

namespace LiGet.NuGet.Server.Tests
{
    public class ServerPackageTest
    {
        [Theory]
        [InlineData("1.0.0", false)]
        [InlineData("1.0.0-alpha", false)]
        [InlineData("1.0.0-alpha.1", true)]
        [InlineData("1.0.0+githash", true)]
        [InlineData("1.0.0-alpha+githash", true)]
        [InlineData("1.0.0-alpha.1+githash", true)]
        public void IsSemVer2_CanBeDeterminedByPackageVersion(string version, bool isSemVer2)
        {
            // Arrange
            var package = new Mock<IPackage>();
            package.Setup(x => x.Version).Returns(SemanticVersion.Parse(version));
            var packageDerivedData = new PackageDerivedData();

            // Act
            var serverPackage = new ServerPackage(package.Object, packageDerivedData);

            // Assert
            Assert.Equal(isSemVer2, serverPackage.IsSemVer2);
        }

        [Theory]
        [InlineData("1.0.0", false)]
        [InlineData("1.0.0-alpha", false)]
        [InlineData("1.0.0-alpha.1", true)]
        [InlineData("[1.0.0-alpha.1, 2.0.0)", true)]
        [InlineData("[1.0.0+githash, 2.0.0)", true)]
        [InlineData("[1.0.0-alpha, 2.0.0-alpha.1)", true)]
        [InlineData("[1.0.0, 2.0.0+githash)", true)]
        [InlineData("[1.0.0-alpha, 2.0.0)", false)]
        public void IsSemVer2_CanBeDeterminedByDependencyVersionRange(string versionRange, bool isSemVer2)
        {
            // Arrange
            var package = new Mock<IPackage>();
            package
                .Setup(x => x.Version)
                .Returns(SemanticVersion.Parse("1.0.0"));
            package
                .Setup(x => x.DependencySets)
                .Returns(new[]
                {
                    new PackageDependencyGroup(
                        new NuGetFramework(".NETFramework,Version=v4.5"),
                        new[]
                        {
                            new PackageDependency("OtherPackage", VersionRange.Parse(versionRange))
                        })
                }.AsEnumerable());
            var packageDerivedData = new PackageDerivedData();

            // Act
            var serverPackage = new ServerPackage(package.Object, packageDerivedData);

            // Assert
            Assert.Equal(isSemVer2, serverPackage.IsSemVer2);
        }
    }
}
