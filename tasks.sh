#!/bin/bash
set -e
# PATCH currently failing private caching server
rm -f ~/.nuget/NuGet/NuGet.Config

dotnet restore

command="$1"
case "${command}" in
  build)
    dotnet publish -c Release src/LiGet.App/LiGet.App.csproj
    dotnet publish -c Release tests/LiGet.Tests/LiGet.Tests.csproj
    ;;
  test)
    mkdir -p tools
    cd tools && nuget install xunit.runner.console -Version 2.3.0 && cd ..
    test_assembly='tests/LiGet.Tests/bin/Release/netcoreapp2.0/publish/LiGet.Tests.dll'
    shift
    dotnet tools/xunit.runner.console.2.3.0/tools/netcoreapp2.0/xunit.console.dll $test_assembly $@
    ;;
  qtest)
    dotnet publish -c Release tests/LiGet.Tests/LiGet.Tests.csproj
    test_assembly='tests/LiGet.Tests/bin/Release/netcoreapp2.0/publish/LiGet.Tests.dll'
    shift
    dotnet tools/xunit.runner.console.2.3.0/tools/netcoreapp2.0/xunit.console.dll $test_assembly $@
    ;;
    *)
      echo "Invalid command: '${command}'"
      exit 1
    ;;
esac
