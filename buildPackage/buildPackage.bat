@del AttributeCaching*.nupkg

msbuild ..\AttributeCaching.sln /t:clean;build /p:Configuration=Release
@if errorlevel 1 goto BuildFailed

..\.nuget\nuget pack AttributeCaching.nuspec
@if errorlevel 1 goto BuildFailed

..\.nuget\nuget pack AttributeCaching.Redis.nuspec
@if errorlevel 1 goto BuildFailed

:BuildFailed
