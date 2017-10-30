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
    make_clean_dir 'liget-test1' && dotnet new classlib && dotnet pack && cd .. &&\
    make_clean_dir 'liget-two' && dotnet new classlib && dotnet pack && dotnet pack /p:PackageVersion=2.1.0 &&\
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
      exit 1
    fi
    if [[ -z "AIT_DOCKER_IMAGE_TAG" ]]; then
      echo "fail! AIT_DOCKER_IMAGE_TAG not set"
      exit 1
    fi
    ide "./tasks.sh build_inputs"
    ide --idefile Idefile.e2e-docker "./e2e/run.sh"
    ;;
  prepare_code_release)
    version=$2
    if [[ -z "$version" ]]; then
      version=$(get_last_version_from_changelog "${changelog_file}")
    fi
    set_version_in_changelog "${changelog_file}" "${version}"
    exit $?
    ;;
  code_release)
    # conditional release
    git fetch origin
    current_commit_git_tags=$(git tag -l --points-at HEAD)
    if [[ "${current_commit_git_tags}" != "" ]];then
      log_error "Current commit is already tagged"
      exit 1
    else
      log_info "Current commit has no tags, starting code release..."
      version_from_changelog=$(get_last_version_from_changelog "${changelog_file}")
      validate_version_is_semver "${version_from_changelog}"
      changelog_first_line=$(cat ${changelog_file} | head -1)
      if [[ "${changelog_first_line}" == "#"*"Unreleased"* ]];then
        log_error "Top of changelog has 'Unreleased' flag"
        exit 1
      fi
      if git tag | grep "${version_from_changelog}"; then
        log_error "The last version from changelog was already git tagged: ${version_from_changelog}"
        exit 1
      fi
      git tag "${version_from_changelog}" && git push origin "${version_from_changelog}"
    fi
    exit $?
    ;;
  publish_docker_private)
    source_imagerc "${image_dir}"  "${imagerc_filename}"
    production_image_tag=$(get_last_version_from_changelog "${changelog_file}")
    docker_push "${AIT_DOCKER_IMAGE_NAME}" "${AIT_DOCKER_IMAGE_TAG}" "${production_image_tag}"
    exit $?
    ;;
    *)
      echo "Invalid command: '${command}'"
      exit 1
    ;;
esac
