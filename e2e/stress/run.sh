#!/bin/bash
set -e

echo "paket update to download a bunch of packages"
cd paket
paket update
echo "nuget push a bunch of packages"
./push_all.sh
cd ..

for i in `seq 1 4`;
do
        echo "Generating specialized HOME for paket load in /home/ide/$i"
        mkdir -p paket-$i /home/ide/$i/.nuget/NuGet
        cp paket/paket.dependencies paket-$i
        cat << EOF > /home/ide/$i/.nuget/NuGet/NuGet.Config
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="liget-proxy" value="http://liget:9011/api/cache/v3/index.json" protocolVersion="3" />
    <add key="liget" value="http://liget:9011/api/v2" protocolVersion="2" />
  </packageSources>
</configuration>
EOF
        bash -c "cd paket-$i && HOME=/home/ide/$i paket install -f" &
done

time wait
