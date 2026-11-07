$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
& "$PSScriptRoot/test.ps1"
$releaseFolder = Join-Path $projectRoot 'artifacts/Mahjong-win-x64'
dotnet publish src/Mahjong.App/Mahjong.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -o $releaseFolder
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
Copy-Item README.md,LICENSE,CHANGELOG.md,THIRD_PARTY_NOTICES.md,TEST_RESULTS.md -Destination $releaseFolder
Copy-Item installer/USER_GUIDE.txt -Destination $releaseFolder
$zipPath = Join-Path $projectRoot 'artifacts/Mahjong-win-x64.zip'
$releaseItems = @(Get-ChildItem -LiteralPath $releaseFolder | Where-Object Name -notin @('SmokeData','SmokeResults') | ForEach-Object FullName)
Compress-Archive -LiteralPath $releaseItems -DestinationPath $zipPath -Force
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLower()
Set-Content -LiteralPath ($zipPath+'.sha256') -Encoding ascii -Value ($zipHash+'  '+[IO.Path]::GetFileName($zipPath))
Write-Host "Ready: $releaseFolder/Mahjong.exe"
