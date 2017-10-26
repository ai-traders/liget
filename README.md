# LiGet

A nuget server created with linux-first approach.

## Why?

There seems to be no good nuget server for hosting private nuget packages and caching,
when working mainly with linux and dotnet core.

## Goals

This project aims at following:
 * provide **self-hosted** nuget server for private package hosting.
 * hosted with kestrel on dotnet core 2.0
 * released as ready to use docker image, preferably to be deployed to kubernetes.
 * continuously tested with paket including several common project setup cases
 * good performance when server is used by multiple clients,
 such as CI agents building various projects, downloading lots of packages at the same time.
 * easy to develop on linux in VS Code, not only in VS on windows.
 * if possible, implement caching mode for public packages from nuget.org

# Development

All building and tests are done with [IDE](https://github.com/ai-traders/ide) and docker.

### Build

```
dotnet restore
dotnet build
```

### Run unit tests

```
dotnet restore
cd tests/LiGet.Tests/
dotnet xunit
```

# License and authors

Firsly, this project is using lots of code from other nuget servers,
either as reference or actually porting pieces of code.
 * [TanukiSharp/MinimalNugetServer as minimal dotnet core setup](https://github.com/TanukiSharp/MinimalNugetServer)
 * [emresenturk/NetCoreNugetServer another minimal dotnet core server](https://github.com/emresenturk/NetCoreNugetServer)
 * [official NuGet.Server](https://github.com/NuGet/NuGet.Server)
 * [NuGet.Lucene](https://github.com/themotleyfool/NuGet.Lucene/tree/master/source) which is part of klondike

This project is licenced under Apache License 2.0
