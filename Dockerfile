FROM microsoft/dotnet:2.1.4-aspnetcore-runtime-stretch-slim
EXPOSE 9011

RUN apt-get update && apt-get install -y sudo wget moreutils &&\
  apt-get -y autoremove && apt-get -y autoclean && apt-get -y clean &&\
  rm -rf /tmp/* /var/tmp/* && rm -rf /var/lib/apt/lists/*

ENV TINI_VERSION v0.16.1
ADD https://github.com/krallin/tini/releases/download/${TINI_VERSION}/tini /tini

RUN chmod +x /tini
ENTRYPOINT ["/tini", "--"]

RUN mkdir -p /home/liget /home/liget/.nuget/NuGet &&\
    mkdir -p /data/simple2 /data/ef.sqlite /cache/simple2 &&\
    groupadd -g 1000 liget &&\
    useradd -d /home/liget -s /bin/bash -u 1000 -g liget liget &&\
    chown -R liget:liget /home/liget /data /cache

RUN wget --tries=3 --retry-connrefused --wait=3 --random-wait --quiet --show-progress --progress=bar:force https://github.com/stedolan/jq/releases/download/jq-1.5/jq-linux64 &&\
  chmod +x ./jq-linux64 && mv -f ./jq-linux64 /usr/bin/jq

ENV ASPNETCORE_ENVIRONMENT=Production \
    LIGET_SKIP_APPCONFIG_GEN=false \
    LIGET_SKIP_RUNTIMECONFIG_GEN=false \
    LIGET_API_KEY_HASH=658489D79E218D2474D049E8729198D86DB0A4AF43981686A31C7DCB02DC0900 \
    LIGET_EF_RUN_MIGRATIONS=true \
    LIGET_DB_TYPE=Sqlite \
    LIGET_DB_CONNECTION_STRING="Data Source=/data/ef.sqlite/sqlite.db" \
    LIGET_SIMPLE2_ROOT_PATH=/data/simple2 \
    LIGET_STORAGE_BACKEND=simple2 \
    LIGET_SEARCH_PROVIDER=Database \
    LIGET_CACHE_ENABLED=true \
    LIGET_CACHE_PROXY_SOURCE_INDEX=https://api.nuget.org/v3/index.json \
    LIGET_NUPKG_CACHE_BACKEND=simple2 \
    LIGET_NUPKG_CACHE_SIMPLE2_ROOT_PATH=/cache/simple2 \
    LIGET_BAGET_COMPAT_ENABLED=false \
    LIGET_LOG_LEVEL=Warning \
    LIGET_LOG_BACKEND=console \
    LIGET_LOG_GELF_PORT=12201 \
    LIGET_LOG_GELF_SOURCE=liget \
    LIGET_GC_CONCURRENT=true \
    LIGET_GC_SERVER=true \
    LIGET_THREAD_POOL_MIN=16 \
    LIGET_THREAD_POOL_MAX=32

COPY /src/LiGet/bin/Release/netcoreapp2.1/publish/ /app

ADD docker-scripts/configure.sh /usr/bin/configure-liget
ADD docker-scripts/run.sh /app/run.sh
RUN chmod +x /app/run.sh /usr/bin/configure-liget
CMD /app/run.sh
