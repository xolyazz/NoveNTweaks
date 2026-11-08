@echo off
setlocal
cd /d "%~dp0"
dotnet publish NoveNTweaks\NoveNTweaks.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts\publish
exit /b %errorlevel%
