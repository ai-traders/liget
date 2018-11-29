#!/bin/bash
set -e

if [ -z "${E2E_PAKET_VERSION}" ]; then
  echo "E2E_PAKET_VERSION is not set"
  exit 2;
fi

echo "Overriding nuget configuration in /home/ide/.nuget/NuGet/NuGet.Config"
cat << EOF > /home/ide/.nuget/NuGet/NuGet.Config
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="baget" value="http://nuget:9090/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

echo "Sleeping 10s to wait for server to be ready"
sleep 10

COMPAT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
E2E_DIR="$COMPAT_DIR/.."
cd $COMPAT_DIR

PAKET_EXE="$E2E_DIR/.paket/paket.exe"

cat << EOF > $E2E_DIR/.paket/paket.bootstrapper.exe.config
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="PreferNuget" value="True"/>
    <add key="PaketVersion" value="${E2E_PAKET_VERSION}"/>
  </appSettings>
</configuration>
EOF

mono $E2E_DIR/.paket/paket.bootstrapper.exe

# BaGet running with BAGET_IMPORT_ON_BOOT should have all private packages imported already
cd $E2E_DIR/input

cd $COMPAT_DIR/paket
rm -rf paket-files
paket restore
