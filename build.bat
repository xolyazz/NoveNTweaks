@echo off
setlocal
cd /d "%~dp0"
dotnet build NoveNTweaks.sln -c Release
exit /b %errorlevel%
