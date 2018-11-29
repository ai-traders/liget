[![Build Status](https://travis-ci.com/ai-traders/liget.svg?branch=master)](https://travis-ci.com/ai-traders/liget)

# LiGet

[![Join the chat at https://gitter.im/AI-Traders/liget-server](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/AI-Traders/liget-server?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

A [NuGet server](https://docs.microsoft.com/en-us/nuget/api/overview) with linux-first approach.

### Why? and goals

There seems to be no good nuget server for hosting private nuget packages and caching,
when working mainly with linux and dotnet core.
Running windows just to host a several nuget packages seems like a big waste.

This project aims at following:
 * provide **self-hosted** nuget server for private package hosting.
 * provide caching mode for public packages from nuget.org
 * hosted with kestrel on dotnet core 2.0
 * released as ready to use, **end-to-end** tested [docker image](#docker), preferably to be deployed to kubernetes.
 * continuously tested with paket including several common project setup cases
 * good performance when server is used by multiple clients,
 such as CI agents building various projects, downloading lots of packages at the same time.
 * easy to develop on linux in VS Code, not only in VS on windows.

#### BaGet fork

TL;DR since `1.0.0` LiGet is a fork of BaGet. Read lower why...

We have previously created and used LiGet from various pojects, just to get it working on dotnet core.
When [BaGet](https://github.com/loic-sharma/BaGet) started to look promissing,
we contributed some work there with indention to migrate from LiGet to BaGet and obsolete the project.
However, following was deal-breaker:
 - What we consider critical basis for mature project [was not merged](https://github.com/loic-sharma/BaGet/pull/108):
    - build must be reproducible, which in current .Net world means `paket.lock` commited in source repository.
    - released product must be built CD-style. Which in short means to build artifacts only once, and run them through a pipeline of tests and QA. It is not acceptable to run `dotnet build` or `dotnet publish` several times for same commit. There must be a well-defined set of binaries which were tested through all pipeline stages.
    - if docker is released then docker image must be tested with end-case tests running actual nuget clients.
  - long feedback time for PRs in BaGet. I spend only a few days at time to get job done. I cannot wait weeks for review.

How is this fork different from upstream BaGet:
- using FAKE for build system, rather than scripting in MsBuild.
- added unit, integration tests and e2e tests with paket and nuget cli.
- we use docker and [CLI tool IDE](https://github.com/ai-traders/ide) to create reproducible [development](#Development) environment for LiGet.
- added release cycle and testing of docker image using continuous delivery practices.
- implements read-through cache as separate endpoint. Which at the time [does not work upstream](https://github.com/loic-sharma/BaGet/issues/93).
- uses paket and FAKE for build system.
- uses [Carter](https://github.com/CarterCommunity/Carter) for routing rather than bare Asp routing.
- adds ability to log to graylog
- adds V2 implementation from old LiGet
- caching proxy has different endpoint `/api/cache/v3/index.json` than private packages `/api/v3/index.json`

# Usage

See [releases](https://github.com/ai-traders/LiGet/releases) to get docker image version.

```
docker run -ti -p 9011:9011 tomzo/liget:<version>
```

For persistent data, you should mount **volumes**:
 - `/data/simple2` contains pushed private packages
 - `/data/ef.sqlite` contains sqlite database
 - `/cache/simple2` contains cached public packages

You should change the default api key (`NUGET-SERVER-API-KEY`) used for pushing packages,
by setting SHA256 into `ApiKeyHash` environment variable. You can generate it with `echo -n 'my-secret' | sha256sum`.

### Logging to graylog

LiGet is using [GELF provider for Microsoft.Extensions.Logging](https://github.com/mattwcole/gelf-extensions-logging)
to optionally configure logging via GELF to graylog.
To configure docker image for logging to your graylog, you can set following environment variables:
```
Graylog__Host=your-graylog.com
Graylog__Port=12201
Graylog__AdditionalFields__environment=development
```

## On client side

### Usage only as private repository

For **dotnet CLI and nuget** you need to configure nuget config `~/.nuget/NuGet/NuGet.Config` with something like:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="liget" value="http://liget:9011/api/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

For paket, in `paket.dependencies`, just specify another source:
```
source http://liget:9011/api/v3/index.json
```

### Pushing packages

```
dotnet nuget push mypackage.1.0.0.nupkg --source http://liget:9011/api/v3/index.json --api-key NUGET-SERVER-API-KEY
```

### Usage as caching proxy

For **dotnet CLI and nuget** you need to configure nuget config `~/.nuget/NuGet/NuGet.Config` with something like:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="liget" value="http://liget:9011/api/cache/v3/index.json" protocolVersion="3" />
    <add key="liget" value="http://liget:9011/api/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

For paket, in `paket.dependencies`, just specify liget as the 2 only sources
```
source http://liget:9011/api/cache/v3/index.json
# public packages...

source http://liget:9011/api/v3/index.json
# private packages...
```

## Migrating from BaGet

If you have been using BaGet before, then many of your nuget sources in projects,
 could look like this, e.g. in paket:
```
source http://my-nuget.com/cache/v3/index.json
# public packages (only in ai-traders fork)

source http://my-nuget.com/v3
# private packages
```
Above endpoints end up in `paket.lock` too.
LiGet has different endpoints (with `/api` before endpoints).
If you want to deploy LiGet in place of BaGet and (at least temporarily) keep above endpoints,
you can enable BaGet compatibity mode in LiGet.
```
BaGetCompat__Enabled=true
```
This will enable following behavior:
 - `/cache/v3/index.json` returns same content as our fork's BaGet's `/api/cache/v3/index.json`. Upstream BaGet does not have separate endpoint for public packages anyway.
 - `/v2/*` returns **V2** resources, same as `/api/v2/*`

### Importing packages

To make transition from old (<1.0.0) LiGet or any other server which keeps `.nupkg` files in a directory,
there is an `import` command:
```
dotnet LiGet.dll import --path dir
```
In the docker image you can setup environment variable - `LIGET_IMPORT_ON_BOOT=/data/simple`
which will cause liget to first search for `nupkg` files in `$LIGET_IMPORT_ON_BOOT`, before starting server.
Packages which were already added are skipped.
Setting `LIGET_IMPORT_ON_BOOT=/data/simple` is sufficient for migration from (<1.0.0) LiGet.

*Note: you only need to set this variable once to perform initial migration.
You should unset it in later deployments to avoid uncessary scanning.*


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

All packages, cache, and temporary data is stored in /data. By default in `/data/<backend>`.

`/data` will be always owned by `liget`. Startup script switches uid/gid at start
to fit with whatever was mounted from the host.
The exception to this is when `/data` is owned by `root`, then liget has to run as `root`.``

### Configuration

Everything can be configured with environment variables:

 * `LIGET_BACKEND` by default `simple2`. In `1.0.0` introduced as the only implementation, replacing previous `simple`.
 * `LIGET_SIMPLE2_ROOT_PATH` - root directory used by `simple` backend. By default `/data/simple2`.

#### Runtime

*TODO: implement as was in liget*

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
 * `LIGET_NUPKG_CACHE_BACKEND` - backend of the .nupkg caching proxy. By default `simple2`,
 which in `1.0.0` was introduced as the only implementation.
 * `LIGET_NUPKG_CACHE_SIMPLE2_ROOT_PATH` - root directory where dbreeze will store cached packages.
 By default `/cache/simple2`.

#### Logging

 * `LIGET_LOG_LEVEL` - by default `INFO`.
 * `LIGET_LOG_BACKEND` - by default `console`. Also can be `gelf` or `custom`.

##### Gelf

Logging to graylog.

 * `LIGET_LOG_GELF_HOST` - no default. But should be configured when `LIGET_LOG_BACKEND=gelf`
 * `LIGET_LOG_GELF_PORT` - by default `12201`.

# Development

We rely heavily on docker to create reproducible development environment.
This allows to execute entire build process on any machine which has:
 - local docker daemon
 - docker-compose
 - `ide` script on path. It is a [CLI tool](https://github.com/ai-traders/ide)
  wrapper around docker and docker-compose which deals with issues such as ownership of files,
  mounting proper volumes, cleanup, etc.

You can execute entire build from scratch to e2e tests (like [travis](.travis.yml)).
 - Install docker daemon if you haven't already
 - Install docker-compose
 - Install IDE
```
sudo bash -c "`curl -L https://raw.githubusercontent.com/ai-traders/ide/master/install.sh`"
```

Then to execute entire build:
```
./tasks.sh all
```

This will pull `dotnet-ide` [docker image](https://github.com/ai-traders/docker-dotnet-ide) which
has all build and test dependencies: dotnet SDK, mono, paket CLI, FAKE, Node.js.

Usage of IDE is optional and you can easily contribute if you have above tools installed on your machine.

## Release cycle

Releases are automated from the master branch, executed by GoCD pipeline, release is published only if all tests have passed.
[Travis](https://travis-ci.com/ai-traders/LiGet) executes the same tasks in the same environment and is for reference to the public community.
If there is `- Unreleased` note at the top of [Changelog](CHANGELOG.md),
then release is a preview, tagged as `<version>-<short-commit-sha>`.
Otherwise it is a full release, tagged as `<version>`.

### Submitting patches

1. Fork and create branch.
2. Commit your changes.
3. Submit a PR, travis will run all tests.
4. Address issues in the review and build failures.
5. Before merge rebase on master `git rebase -i master` and possibly squash some of the commits.

### Issues

If you have an idea or found a bug, open an issue to discuss it.

# License and authors

Firsly, this project is using lots of code from other nuget servers,
either as reference or actually porting pieces of code.
Credits:
 * Loïc Sharma for creating [BaGet](https://github.com/loic-sharma/BaGet)
 * [TanukiSharp/MinimalNugetServer as minimal dotnet core setup](https://github.com/TanukiSharp/MinimalNugetServer)
 * [emresenturk/NetCoreNugetServer another minimal dotnet core server](https://github.com/emresenturk/NetCoreNugetServer)
 * [official NuGet.Server](https://github.com/NuGet/NuGet.Server)
 * [NuGet.Lucene](https://github.com/themotleyfool/NuGet.Lucene/tree/master/source) which is part of klondike

This project is licenced under MIT.
