FROM microsoft/dotnet:2.0.0-runtime-jessie

RUN apt-get update && apt-get install -y sudo

ENV TINI_VERSION v0.16.1
ADD https://github.com/krallin/tini/releases/download/${TINI_VERSION}/tini /tini
ADD https://github.com/krallin/tini/releases/download/${TINI_VERSION}/tini.asc /tini.asc
RUN gpg --keyserver hkp://p80.pool.sks-keyservers.net:80 --recv-keys 595E85A6B1B4779EA4DAAEC70B588DFF0527A9B7 \
 && gpg --verify /tini.asc

RUN chmod +x /tini
ENTRYPOINT ["/tini", "--"]

RUN mkdir /home/liget &&\
    groupadd -g 1000 liget &&\
    useradd -d /home/liget -s /bin/bash -u 1000 -g liget liget &&\
    chown -R liget:liget /home/liget

ADD liget.sh /liget.sh
RUN chmod +x /liget.sh

RUN mkdir -p /etc/liget/ /data
ENV LIGET_BACKEND=simple \
 LIGET_SIMPLE_ROOT_PATH=/data/simple \
 LIGET_BACKGROUND_TASKS=true \
 LIGET_FS_MONITORING=true \
 LIGET_FRAMEWORK_FILTERING=true \
 LIGET_ENABLE_DELISTING=true \
 LIGET_IGNORE_SYMBOLS=false \
 LIGET_ALLOW_OVERWRITE=false \
 LIGET_LOG_LEVEL=INFO \
 LIGET_LOG_BACKEND=console \
 LIGET_LOG_GELF_PORT=12201

EXPOSE 9011

ADD publish /app

CMD ["/liget.sh"]
