#!/bin/bash
set -e
# PATCH currently failing private caching server
rm -f ~/.nuget/NuGet/NuGet.Config

if [[ ! -f ./releaser ]];then
  wget --quiet http://http.archive.ai-traders.com/releaser/1.0.3/releaser
fi
source ./releaser
if [[ ! -f ./docker-ops ]];then
  wget --quiet http://http.archive.ai-traders.com/docker-ops/0.2.1/docker-ops
fi
source ./docker-ops
# This goes as last in order to let end user variables override default values
releaser_init

image_name_no_registry="liget"
private_image_name="docker-registry.ai-traders.com/${image_name_no_registry}"
public_image_name="tomzo/${image_name_no_registry}"
image_dir="./docker"
imagerc_filename="imagerc"

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
  build_inputs)
    build_inputs
    ;;
  itest)
    ide "./tasks.sh build_inputs"
    ide --idefile Idefile.e2e "./e2e/run.sh"
    ;;
  build_docker)
    # pwd is the ${image_dir}
    image_tag=$2
    docker_build "${image_dir}" "${imagerc_filename}" "${private_image_name}" "$image_tag"
    exit $?
    ;;
  test_docker)
    source "${image_dir}/${imagerc_filename}"
    if [[ -z "AIT_DOCKER_IMAGE_NAME" ]]; then
      echo "fail! AIT_DOCKER_IMAGE_NAME not set"
      return 1
    fi
    if [[ -z "AIT_DOCKER_IMAGE_TAG" ]]; then
      echo "fail! AIT_DOCKER_IMAGE_TAG not set"
      return 1
    fi
    ide "./tasks.sh build_inputs"
    ide --idefile Idefile.e2e-docker "./e2e/run.sh"
    ;;
    *)
      echo "Invalid command: '${command}'"
      exit 1
    ;;
esac
