#!/bin/bash
set -e

echo "Sleeping 4s to wait for server to be ready"
sleep 4

STRESS_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
E2E_DIR="$STRESS_DIR/../"
cd $E2E_DIR

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

cd $STRESS_DIR

PAKET_EXE="$E2E_DIR/.paket/paket.exe"

echo "paket update to download a bunch of packages"
cd paket
mono $PAKET_EXE update
echo "nuget push a bunch of packages"
./push_all.sh
cd ..

for i in `seq 1 6`;
do
        echo "Generating specialized HOME for paket load in /home/ide/$i"
        mkdir -p paket-$i /home/ide/$i/.nuget/NuGet
        cp paket/paket.dependencies paket-$i
        cat << EOF > /home/ide/$i/.nuget/NuGet/NuGet.Config
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="cache" value="http://liget:9011/cache/v3/index.json" protocolVersion="3" />
    <add key="liget" value="http://liget:9011/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF
        bash -c "cd paket-$i && HOME=/home/ide/$i mono $PAKET_EXE install -f" &
done

time wait
