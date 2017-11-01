#!/bin/bash
set -e

echo "paket update to download a bunch of packages"

cd paket
paket update

echo "nuget push a bunch of packages"
./push_all.sh
