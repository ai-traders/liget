#!/bin/bash
set -e

if [ -z "${E2E_PAKET_VERSION}" ]; then
  echo "E2E_PAKET_VERSION is not set"
  exit 2;
fi

echo "Overriding nuget configuration in /home/dojo/.nuget/NuGet/NuGet.Config"
# This is how < 1.0.0 was configured
cat << EOF > /home/dojo/.nuget/NuGet/NuGet.Config
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="liget-proxy" value="http://nuget:9011/api/cache/v3/index.json" protocolVersion="3" />
    <add key="liget" value="http://nuget:9011/api/v2" protocolVersion="2" />
  </packageSources>
</configuration>
EOF

echo "Sleeping 10s to wait for server to be ready"
sleep 10

COMPAT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

cd $COMPAT_DIR/run-old
bats .
