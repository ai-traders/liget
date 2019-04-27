SET PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%

dotnet fake run build.fsx %*
