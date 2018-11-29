#!/bin/bash
set -e

PAKET_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

for nupkg in `find $PAKET_DIR -mindepth 1 -type f -name '*.nupkg'`
do
    echo "Pushing $nupkg"
    dotnet nuget push $nupkg --source http://liget:9011/api/v2/package --api-key NUGET-SERVER-API-KEY
done
