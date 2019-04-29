#!/bin/bash

set -e

RELEASER_VERSION="2.1.0"
DOCKER_OPS_VERSION="2.0.0"
SECRET_OPS_VERSION="0.6.1"

SECRET_OPS_FILE="ops/secret-ops"
SECRET_OPS_TAR_FILE="ops/secret-ops-${SECRET_OPS_VERSION}.tar.gz"
RELEASER_FILE="ops/releaser-${RELEASER_VERSION}"
DOCKER_OPS_FILE="ops/docker-ops-${DOCKER_OPS_VERSION}"

mkdir -p ops
if [[ ! -f $RELEASER_FILE ]];then
  wget --quiet -O $RELEASER_FILE https://github.com/kudulab/releaser/releases/download/${RELEASER_VERSION}/releaser
fi
source $RELEASER_FILE
if [[ ! -f $DOCKER_OPS_FILE ]];then
  wget --quiet -O $DOCKER_OPS_FILE https://github.com/kudulab/docker-ops/releases/download/${DOCKER_OPS_VERSION}/docker-ops
fi
source $DOCKER_OPS_FILE
if [[ ! -f $SECRET_OPS_TAR_FILE ]];then
  wget --quiet -O $SECRET_OPS_TAR_FILE https://github.com/kudulab/secret-ops/releases/download/${SECRET_OPS_VERSION}/secret-ops.tar.gz
  tar -xf $SECRET_OPS_TAR_FILE -C ops
fi
source $SECRET_OPS_FILE

image_name="tomzo/liget"
image_registry="dockerhub"
image_dir="./image"
imagerc_filename="imagerc"

function docker_login {
  dockerhub_user=tomzo
  vault read -field=password secret/tomzo/dockerhub | docker login --username $dockerhub_user --password-stdin
}

# Fix for multi-line environment variables not working in docker envs
unset TRAVIS_COMMIT_MESSAGE

image_name="tomzo/liget"
image_registry="dockerhub"
image_dir="./"
imagerc_filename="imagerc"

function make_clean_dir {
  dir=$1
  rm -rf $dir && mkdir -p $dir && cd $dir
}

export E2E_PAKET_VERSION="5.198.0"

function get_version_tag {
  changelog_first_line=$(cat ${changelog_file} | head -1)
  changelog_version=$(releaser::get_last_version_from_changelog "${changelog_file}")
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
    dojo "./tasks.sh _build"
    ;;
  unit_test)
    dojo "./tasks.sh _unit_test"
    ;;
  _build_inputs)
    ./build.sh --target ExampleNuGets --single-target
    ;;
  itest)
    dojo "./tasks.sh build_inputs"
    dojo -c Dojofile.e2e "./e2e/run.sh"
    ;;
  build_docker_local)
    set +u
    image_tag=$2
    docker_ops::docker_build "${image_dir}" "${imagerc_filename}" "${image_name}" "${image_tag}" "${image_registry}"
    ;;
  build_docker)
    set +u
    docker_login
    ./tasks.sh build_docker_local $2
    docker_ops::push "${image_dir}" "${imagerc_filename}"
    ;;
  test_docker)
    docker_ops::source_imagerc "${image_dir}"  "${imagerc_filename}"
    dojo "./tasks.sh _build_inputs"
    rm -rf e2e/data/*/*
    rm -rf e2e/cache/*/*
    rm e2e/test_*/nuget*/*/ -rf
    dojo -c Dojofile.e2e-docker "./e2e/run.sh"
    ;;
  stress_docker)
    docker_ops::source_imagerc "${image_dir}"  "${imagerc_filename}"
    dojo -c Dojofile.e2e-docker "e2e/stress/run.sh"
    ;;
  baget_compat_docker)
    docker_ops::source_imagerc "${image_dir}"  "${imagerc_filename}"
    dojo "./tasks.sh _build_inputs"
    rm -rf e2e/baget-compat/data/*/*
    rm -rf e2e/baget-compat/cache/*/*
    export LIGET_BAGET_COMPAT_ENABLED=true
    export LIGET_IMPORT_ON_BOOT=/data/simple
    dojo -c Dojofile.baget-compat "e2e/baget-compat/run.sh"
    ;;
  liget0_compat_docker)
    docker_ops::source_imagerc "${image_dir}"  "${imagerc_filename}"
    dojo "./tasks.sh _build_inputs"
    rm -rf e2e/liget0-compat/data/*/*
    rm -rf e2e/liget0-compat/cache/*/*
    export LIGET_IMPORT_ON_BOOT=/data/simple
    # This will push a package in old liget
    dojo -c Dojofile.liget0-compat "e2e/liget0-compat/run-v0.sh"
    # This will get the package in new liget
    dojo -c Dojofile.liget1-compat "e2e/liget0-compat/run-v1.sh"
    ;;
  all)
    dojo "./build.sh --target All"
    ./tasks.sh build_docker_local
    ./tasks.sh test_docker
    ./tasks.sh liget0_compat_docker
    ./tasks.sh baget_compat_docker
    ./tasks.sh stress_docker
    ;;
  prepare_code_release)
    set +u
    version=$2
    if [[ -z "$version" ]]; then
      version=$(releaser::get_last_version_from_changelog "${changelog_file}")
    fi
    releaser::set_version_in_changelog "${changelog_file}" "${version}"
    ;;
  publish)
    docker_login
    version=$(releaser::get_last_version_from_whole_changelog "${changelog_file}")
    docker_ops::ensure_pulled_image "${image_dir}" "${imagerc_filename}"
    production_image_tag=$(get_version_tag)
    docker_ops::retag_push "${image_dir}"  "${imagerc_filename}" "${image_name}" "${production_image_tag}" "${image_registry}"
    ;;
  github_release)
    GITHUB_TOKEN=$(vault read -field=token secret/gocd/github_releases)
    export GITHUB_TOKEN
    dojo "./build.sh --target GitHubRelease"
    ;;
  generate_vault_token)
    vault_token=$(vault token create -orphan -ttl=48h -policy=gocd -policy=dockerhub-tomzo -field token -metadata gocd_renew=true)
    secured_token_gocd=$(secret_ops::encrypt_with_gocd_top "${vault_token}")
    echo "Generated token: ${vault_token} and encrypted by GoCD server"
    secret_ops::insert_vault_token_gocd_yaml "${secured_token_gocd}"
    ;;
  *)
    echo "Invalid command: '${command}'"
    exit 1
    ;;
esac
