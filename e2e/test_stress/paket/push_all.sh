#!/bin/bash
set -e

PAKET_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

for nupkg in `find $PAKET_DIR -mindepth 1 -type f -name '*.nupkg'`
do
    echo "Pushing $nupkg"
    nuget push $nupkg -src http://liget:9011/api/v2
done
