# LiGet

[![Join the chat at https://gitter.im/AI-Traders/liget-server](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/AI-Traders/liget-server?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

A nuget server created with linux-first approach.

### Why? and goals

There seems to be no good nuget server for hosting private nuget packages and caching,
when working mainly with linux and dotnet core.
Running windows just to host a several nuget packages seems like a big waste.

This project aims at following:
 * provide **self-hosted** nuget server for private package hosting.
 * hosted with kestrel on dotnet core 2.0
 * released as ready to use [docker image](#docker), preferably to be deployed to kubernetes.
 * continuously tested with paket including several common project setup cases
 * good performance when server is used by multiple clients,
 such as CI agents building various projects, downloading lots of packages at the same time.
 * easy to develop on linux in VS Code, not only in VS on windows.
 * if possible, implement caching mode for public packages from nuget.org

## Features and limitations

 * Limited **NuGet V2 API for hosting private packages**. Includes endpoints `FindPackagesById()`, `Packages()` and `PUT /api/v2`.
 Which is sufficient for clients to download, push, find or restore packages.
 * **Caching proxy of with limited NuGet V3 API**. It intercepts responses from selected
 services of `https://api.nuget.org/v3/index.json` replacing `https://api.nuget.org/v3`
 by local LiGet server URL.
   - Allows to cache `.nupkg` packages on server,
rather than downloading them from the Internet each time.
   - Caches package metadata and invalidates when upstream changes are detected using [NuGet.CatalogReader](https://github.com/emgarten/NuGet.CatalogReader).
   - For end user effect is similar to running a mirror of nuget.org,
   but instead of downloading all packages, cache keeps only the ones which were ever requested.

Not implemented:

 * V2 search, filter and alike queries. These seem to used only by UI or nuget gallery.
 * Authentication and user-based access. Currently the server is open for all requests.

# Usage

## On client side

### Usage only as private repository

For **dotnet CLI and nuget** you need to configure nuget config `~/.nuget/NuGet/NuGet.Config` with something like:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://www.nuget.org/api/v2" protocolVersion="2" />
    <add key="liget" value="http://liget:9011/api/v2" protocolVersion="2" />
  </packageSources>
</configuration>
```

For paket, in `paket.dependencies`, just specify another source:
```
source http://liget:9011/api/v2
```

### Usage as caching proxy

For **dotnet CLI and nuget** you need to configure nuget config `~/.nuget/NuGet/NuGet.Config` with something like:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="liget-proxy" value="http://liget:9011/api/cache/v3/index.json" protocolVersion="3" />
    <add key="liget" value="http://liget:9011/api/v2" protocolVersion="2" />
  </packageSources>
</configuration>
```

For paket, in `paket.dependencies`, just specify liget as the 2 only sources
```
source http://liget:9011/api/cache/v3/index.json
# public packages...

source http://liget:9011/api/v2
# private packages...
```


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

#### Runtime

Every dotnet Core application has `.runtimeconfig.json`, which can configure garbage collector.
You may want to set following:
 * `LIGET_GC_CONCURRENT` - by default `true`
 * `LIGET_GC_SERVER` - by default `true`, beware though that [this may cause higher memory use](https://blog.markvincze.com/troubleshooting-high-memory-usage-with-asp-net-core-on-kubernetes/).
 * `LIGET_THREAD_POOL_MIN` - minimal number of worker threads. By default 16.
 * `LIGET_THREAD_POOL_MAX` - minimal number of worker threads. By default 32.

Kestrel specific:
 * `LIGET_LIBUV_THREAD_COUNT` - number of libuv threads handling the requests. By default not set, determined by libuv default.

#### Cache

 * `LIGET_CACHE_PROXY_SOURCE_INDEX` - address of original V3 API to cache. By default `https://api.nuget.org/v3/index.json`.
 * `LIGET_CACHE_INVALIDATION_CHECK_PERIOD` - defines frequency at which a check with upstream server is made to see if cache is invalid. By default `60` (seconds).
 * `LIGET_NUPKG_CACHE_BACKEND` - backend of the .nupkg caching proxy. By default `dbreeze`,
 which, currently is the only implementation.
 * `LIGET_NUPKG_CACHE_DBREEZE_ROOT_PATH` - root directory where dbreeze will store cached packages.
 By default `/data/cache/dbreeze`.
 * `LIGET_NUPKG_CACHE_DBREEZE_BACKEND` - storage backend of dbreeze, can be `disk` or `memory`.
 By default `disk`.

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
