# LiGet

A nuget server created with linux-first approach.

## Why?

There seems to be no good nuget server for hosting private nuget packages and caching,
when working mainly with linux and dotnet core.
Running windows just to host a several nuget packages seems like a big waste.

## Goals

This project aims at following:
 * provide **self-hosted** nuget server for private package hosting.
 * hosted with kestrel on dotnet core 2.0
 * released as ready to use [docker image](#docker), preferably to be deployed to kubernetes.
 * continuously tested with paket including several common project setup cases
 * good performance when server is used by multiple clients,
 such as CI agents building various projects, downloading lots of packages at the same time.
 * easy to develop on linux in VS Code, not only in VS on windows.
 * if possible, implement caching mode for public packages from nuget.org

# Usage

## Docker

The simplest start command is

```bash
mkdir -p /tmp/liget-test
docker run -p 9011:9011 -v /tmp/liget-test/:/data tomzo/liget
```

 * Default current directory `/data`.
 * Main process starts with [tini](https://github.com/krallin/tini) as root,
 then drops privileges to run as `liget` user with `dotnet`.

For best reference see the [docker](/docker) directory with Dockerfile and startup script.

### Volume

All packages, cache, and temporary data is stored in `/data`.
By default in `/data/<backend>`.

`/data` will be always owned by `liget`. Startup script switches uid/gid at start
to fit with whatever was mounted from the host.
The exception to this is when `/data` is owned by `root`, then liget has to run as `root`.``

### Configuration

Everything can be configured with environment variables:

 * `LIGET_BACKEND` by default `simple`. Currently the only implementation
 * `LIGET_SIMPLE_ROOT_PATH` - root directory used by `simple` backend. By default `/data/simple`.
 * `LIGET_BACKGROUND_TASKS` - run background tasks periodically. By default `true`.
 * `LIGET_FS_MONITORING` - monitor `LIGET_SIMPLE_ROOT_PATH` for changes. By default `true`, which allows to drop packages directly to `LIGET_SIMPLE_ROOT_PATH` to be added to repo.
 * `LIGET_ALLOW_OVERWRITE`, by default `false`. When `true` allows push to replace previous package with same version.
 * `LIGET_FRAMEWORK_FILTERING`, by default `true`. Not implemented.
 * `LIGET_ENABLE_DELISTING`, by default `true`. Not implemented.
 * `LIGET_IGNORE_SYMBOLS`, by default `false`. Not implemented.

#### Logging

 * `LIGET_LOG_LEVEL` - by default `INFO`.
 * `LIGET_LOG_BACKEND` - by default `console`. Also can be `gelf` or `custom`.

Default logging is to console. `log4net` is configured by `/etc/liget/log4net.xml`.
If you set `LIGET_LOG_BACKEND=custom` then it is expected that you will provide `/etc/liget/log4net.xml`.

##### Gelf

Logging to graylog.

 * `LIGET_LOG_GELF_HOST` - no default. But should be configured when `LIGET_LOG_BACKEND=gelf`
 * `LIGET_LOG_GELF_PORT` - by default `12201`.

# Development

All building and tests are done with [IDE](https://github.com/ai-traders/ide) and docker.

### Build

```
dotnet restore
dotnet build
```

Or

```
./tasks.sh build
```

### Run unit tests

```
./tasks.sh build
./tasks.sh test
```

# License and authors

Firsly, this project is using lots of code from other nuget servers,
either as reference or actually porting pieces of code.
Credits:
 * [TanukiSharp/MinimalNugetServer as minimal dotnet core setup](https://github.com/TanukiSharp/MinimalNugetServer)
 * [emresenturk/NetCoreNugetServer another minimal dotnet core server](https://github.com/emresenturk/NetCoreNugetServer)
 * [official NuGet.Server](https://github.com/NuGet/NuGet.Server)
 * [NuGet.Lucene](https://github.com/themotleyfool/NuGet.Lucene/tree/master/source) which is part of klondike

This project is licenced under Apache License 2.0.
