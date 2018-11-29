FROM microsoft/dotnet:2.1.4-aspnetcore-runtime-stretch-slim
EXPOSE 9011

RUN apt-get update && apt-get install -y sudo &&\
  apt-get -y autoremove && apt-get -y autoclean && apt-get -y clean &&\
  rm -rf /tmp/* /var/tmp/* && rm -rf /var/lib/apt/lists/*

ENV TINI_VERSION v0.16.1
ADD https://github.com/krallin/tini/releases/download/${TINI_VERSION}/tini /tini

RUN chmod +x /tini
ENTRYPOINT ["/tini", "--"]

RUN mkdir -p /home/liget /home/liget/.nuget/NuGet &&\
    mkdir -p /var/liget/packages /var/liget/db /var/liget/cache &&\
    groupadd -g 1000 liget &&\
    useradd -d /home/liget -s /bin/bash -u 1000 -g liget liget &&\
    chown -R liget:liget /home/liget /var/liget/

ENV ASPNETCORE_ENVIRONMENT=Production \
    ApiKeyHash=658489D79E218D2474D049E8729198D86DB0A4AF43981686A31C7DCB02DC0900 \
    Storage__Type=FileSystem \
    Storage__Path=/var/liget/packages \
    Database__RunMigrations=true \
    Database__Type=Sqlite \
    Database__ConnectionString="Data Source=/var/liget/db/sqlite.db" \
    Mirror__Enabled=true \
    Mirror__UpstreamIndex="https://api.nuget.org/v3/index.json" \
    Mirror__PackagesPath="/var/liget/cache" \
    Search__Type=Database


COPY /src/LiGet/bin/Release/netcoreapp2.1/publish/ /app

ADD docker-scripts/run.sh /app/run.sh
RUN chmod +x /app/run.sh
CMD /app/run.sh
