#!/bin/bash
set -e
# PATCH currently failing private caching server
rm -f ~/.nuget/NuGet/NuGet.Config

function make_clean_dir {
  dir=$1
  rm -rf $dir && mkdir -p $dir && cd $dir
}

function build_inputs {
  cd e2e/input &&\
    make_clean_dir 'liget-test1' && dotnet new classlib && dotnet pack &&\
  cd ../..
}

command="$1"
case "${command}" in
  build)
    dotnet restore
    dotnet publish -c Release src/LiGet.App/LiGet.App.csproj
    dotnet publish -c Release tests/LiGet.Tests/LiGet.Tests.csproj
    ;;
  test)
    mkdir -p tools
    cd tools && nuget install xunit.runner.console -Version 2.3.0 && cd ..
    test_assembly='tests/LiGet.Tests/bin/Release/netcoreapp2.0/publish/LiGet.Tests.dll'
    shift
    dotnet tools/xunit.runner.console.2.3.0/tools/netcoreapp2.0/xunit.console.dll $test_assembly -parallel none -maxthreads 1 -verbose
    ;;
  qtest)
    dotnet publish -c Release tests/LiGet.Tests/LiGet.Tests.csproj
    test_assembly='tests/LiGet.Tests/bin/Release/netcoreapp2.0/publish/LiGet.Tests.dll'
    shift
    dotnet tools/xunit.runner.console.2.3.0/tools/netcoreapp2.0/xunit.console.dll $test_assembly -parallel none -maxthreads 1 -verbose $@
    ;;
  prep_qe2e)
    dotnet publish -c Release src/LiGet.App/LiGet.App.csproj
    build_inputs
    ;;
  qe2e)
    ide "./tasks.sh prep_qe2e"
    ide --idefile Idefile.e2e "./e2e/run.sh"
    ;;
  prep_itest)
    build_inputs
    ;;
  itest)
    ide "./tasks.sh prep_itest"
    ide --idefile Idefile.e2e "./e2e/run.sh"
    ;;
    *)
      echo "Invalid command: '${command}'"
      exit 1
    ;;
esac
