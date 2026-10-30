$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet build tests/Mahjong.Tests/Mahjong.Tests.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
dotnet run --project tests/Mahjong.Tests -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
