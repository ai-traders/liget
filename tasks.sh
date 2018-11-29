#!/bin/bash

set -e

source .build/docker-ops
source .build/releaser

releaser_init

# Fix for multi-line environment variables not working in docker envs
unset TRAVIS_COMMIT_MESSAGE

image_name_no_registry="liget"
private_image_name="docker-registry.ai-traders.com/${image_name_no_registry}"
public_image_name="tomzo/${image_name_no_registry}"
image_dir="./"
imagerc_filename="imagerc"

function make_clean_dir {
  dir=$1
  rm -rf $dir && mkdir -p $dir && cd $dir
}

export E2E_PAKET_VERSION="5.181.1"

function get_version_tag {
  changelog_first_line=$(cat ${changelog_file} | head -1)
  changelog_version=$(get_last_version_from_changelog "${changelog_file}")
  short_sha=$(git rev-parse --short=8 HEAD)
  if [[ "${changelog_first_line}" == "#"*"Unreleased"* ]] || [[ "${changelog_first_line}" == "#"*"unreleased"* ]] || [[ "${changelog_first_line}" == "#"*"UNRELEASED"* ]];then
    log_info "Top of changelog has 'Unreleased' flag"
    echo "$changelog_version-$short_sha"
  else
    echo "$changelog_version"
  fi
}

command="$1"
case "${command}" in
  _build)
      ./build.sh --target Build
      ./build.sh --target SpaPublish
      ;;
  _unit_test)
      ./build.sh --target Build --single-target
      ;;
  build)
    ide "./tasks.sh _build"
    ;;
  unit_test)
    ide "./tasks.sh _unit_test"
    ;;
  _build_inputs)
    ./build.sh --target ExampleNuGets --single-target
    ;;
  itest)
    ide "./tasks.sh build_inputs"
    ide --idefile Idefile.e2e "./e2e/run.sh"
    ;;
  build_docker)
    image_tag=$2
    docker_build "${image_dir}" "${imagerc_filename}" "${private_image_name}" "$image_tag"
    exit $?
    ;;
  test_docker)
    source_imagerc "${image_dir}"  "${imagerc_filename}"
    ide "./tasks.sh _build_inputs"
    rm -rf e2e/data/db/*
    rm -rf e2e/data/packages/*
    rm -rf e2e/data/cache/*
    rm e2e/test_*/nuget*/*/ -rf
    ide --idefile Idefile.e2e-docker "./e2e/run.sh"
    ;;
  stress_docker)
    source_imagerc "${image_dir}"  "${imagerc_filename}"
    ide --idefile Idefile.e2e-docker "e2e/stress/run.sh"
    ;;
  liget_compat_docker)
    source_imagerc "${image_dir}"  "${imagerc_filename}"
    ide "./tasks.sh _build_inputs"
    rm -rf e2e/baget-compat/data/db/*
    rm -rf e2e/baget-compat/data/packages/*
    rm -rf e2e/baget-compat/data/cache/*
    export BaGetCompat__Enabled=true
    export LIGET_IMPORT_ON_BOOT=/data/simple
    ide --idefile Idefile.baget-compat "e2e/baget-compat/run.sh"
    ;;
  all)
    ide "./build.sh --target All"
    ./tasks.sh build_docker
    ./tasks.sh test_docker
    ./tasks.sh liget_compat_docker
    ./tasks.sh stress_docker
    ;;
  prepare_code_release)
    version=$2
    if [[ -z "$version" ]]; then
      version=$(get_last_version_from_changelog "${changelog_file}")
    fi
    set_version_in_changelog "${changelog_file}" "${version}"
    exit $?
    ;;
  publish_docker_private)
    source_imagerc "${image_dir}"  "${imagerc_filename}"
    production_image_tag=$(get_version_tag)
    docker_push "${AIT_DOCKER_IMAGE_NAME}" "${AIT_DOCKER_IMAGE_TAG}" "${production_image_tag}"
    exit $?
    ;;
  publish_docker_public)
    source_imagerc "${image_dir}"  "${imagerc_filename}"
    production_image_tag=$(get_version_tag)
    docker login --username tomzo --password ${DOCKERHUB_PASSWORD}
    testing_image_tag="${AIT_DOCKER_IMAGE_TAG}"

    log_info "testing_image_tag set to: ${testing_image_tag}"
    log_info "production_image_tag set to: ${production_image_tag}"
    if ! docker images ${AIT_DOCKER_IMAGE_NAME} | awk '{print $2}' | grep ${testing_image_tag} 1>/dev/null ; then
      # if docker image does not exist locally, then "docker tag" will fail,
      # so pull it. However, do not always pull it, the image may be not pushed
      # and only available locally.
      set -x -e
      docker pull "${AIT_DOCKER_IMAGE_NAME}:${testing_image_tag}"
    fi
    set -x -e
    # When tagging a docker image using docker 1.8.3, we can use `docker tag -f`.
    # When using docker 1.12, there is no `-f` option, but `docker tag`
    # always works as if force was used.
    docker tag -f "${AIT_DOCKER_IMAGE_NAME}:${testing_image_tag}" "${public_image_name}:${production_image_tag}" || docker tag "${AIT_DOCKER_IMAGE_NAME}:${testing_image_tag}" "${public_image_name}:${production_image_tag}"
    docker tag -f "${AIT_DOCKER_IMAGE_NAME}:${testing_image_tag}" "${public_image_name}:latest" || docker tag "${AIT_DOCKER_IMAGE_NAME}:${testing_image_tag}" "${public_image_name}:latest"
    if [[ "${dryrun}" != "true" ]];then
      docker push "${public_image_name}:${production_image_tag}"
      docker push "${public_image_name}:latest"
    fi
    set +x +e
    exit $?
    ;;
  github_release)
    ide "./build.sh --target GitHubRelease"
    ;;
    *)
      echo "Invalid command: '${command}'"
      exit 1
    ;;
esac
