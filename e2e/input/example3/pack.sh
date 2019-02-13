#!/bin/bash

dotnet restore
msbuild /p:Configuration=Release
paket pack .