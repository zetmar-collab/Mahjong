$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
$qaFolder = Join-Path $projectRoot 'artifacts/qa'
$resultFolder = Join-Path $projectRoot 'artifacts/ui-tests'
dotnet build src/Mahjong.App/Mahjong.App.csproj -c Release -o $qaFolder
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
$testProcess = Start-Process -FilePath "$qaFolder/Mahjong.exe" -ArgumentList "--smoke --smoke-output `"$resultFolder`"" -WindowStyle Hidden -PassThru -Wait
if ($testProcess.ExitCode -ne 0) { throw "WPF tests failed. See $resultFolder/ui-tests.txt" }
Get-Content "$resultFolder/ui-tests.txt" -Tail 1
