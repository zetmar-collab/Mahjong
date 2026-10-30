param(
    [string]$CompilerPath,
    [switch]$SkipPublish
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$projectFile = Join-Path $projectRoot 'src/Mahjong.App/Mahjong.App.csproj'
$releaseFolder = Join-Path $projectRoot 'artifacts/Mahjong-win-x64'
$sourceScript = Join-Path $projectRoot 'installer/Mahjong.iss'
if (!$CompilerPath) {
    $candidates = @(
        (Join-Path $projectRoot 'artifacts/tools/inno-7.1.1/ISCC.exe'),
        "$env:LOCALAPPDATA/Programs/Inno Setup 7/ISCC.exe",
        "$env:ProgramFiles/Inno Setup 7/ISCC.exe",
        "${env:ProgramFiles(x86)}/Inno Setup 7/ISCC.exe"
    )
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { $candidates += $command.Source }
    $CompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!$CompilerPath -or !(Test-Path -LiteralPath $CompilerPath)) {
    throw 'Install Inno Setup 7 from https://jrsoftware.org/isdl.php, then pass -CompilerPath to ISCC.exe.'
}
[xml]$project = Get-Content -LiteralPath $projectFile -Raw
$version = @($project.Project.PropertyGroup.Version | Where-Object { $_ })[0]
if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw 'Invalid application version.' }
if (!$SkipPublish) {
    dotnet publish $projectFile -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -o $releaseFolder
    if ($LASTEXITCODE -ne 0) { throw 'Publishing failed.' }
}
foreach ($required in @('Mahjong.exe','Mahjong.dll','coreclr.dll','PresentationFramework.dll','Assets/layouts.json','Assets/pl.json','Assets/en.json')) {
    if (!(Test-Path -LiteralPath (Join-Path $releaseFolder $required))) { throw "Missing payload file: $required" }
}
$buildFolder = Join-Path $projectRoot ('artifacts/installer-build/' + [guid]::NewGuid().ToString('N'))
$payloadFolder = Join-Path $buildFolder 'payload'
New-Item -ItemType Directory -Path $payloadFolder -Force | Out-Null
$items = Get-ChildItem -LiteralPath $releaseFolder | Where-Object { $_.Name -notin @('SmokeData','SmokeResults') -and $_.Extension -ne '.pdb' }
foreach ($item in $items) { Copy-Item -LiteralPath $item.FullName -Destination $payloadFolder -Recurse }
foreach ($name in @('README.md','LICENSE','CHANGELOG.md','THIRD_PARTY_NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $payloadFolder -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'installer/USER_GUIDE.txt') -Destination $payloadFolder
Copy-Item -LiteralPath (Join-Path $projectRoot 'installer/Inno-Setup-LICENSE.txt') -Destination $payloadFolder
$manifest = @(Get-ChildItem -LiteralPath $payloadFolder -Recurse -File | ForEach-Object {
    [pscustomobject]@{
        Path = [IO.Path]::GetRelativePath($payloadFolder,$_.FullName).Replace('\','/')
        Length = $_.Length
        SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLower()
    }
})
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $buildFolder 'payload-manifest.json') -Encoding utf8
$outputFolder = Join-Path $projectRoot 'artifacts/installer'
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null
$compilerArgs = @('/Q',('/DAppVersion='+$version),('/DPayloadDir='+$payloadFolder),('/O'+$outputFolder),$sourceScript)
& $CompilerPath @compilerArgs 2>&1 | Tee-Object -FilePath (Join-Path $buildFolder 'compile.log')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$setup = Join-Path $outputFolder "Mahjong-Solitaire-$version-Setup-x64.exe"
if (!(Test-Path -LiteralPath $setup)) { throw 'Setup was not produced.' }
$hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLower()
Set-Content -LiteralPath ($setup+'.sha256') -Encoding ascii -Value ($hash+'  '+[IO.Path]::GetFileName($setup))
$buildInfo = [pscustomobject]@{Version=$version;Setup=$setup;BuildFolder=$buildFolder;Payload=$payloadFolder;SHA256=$hash;Files=$manifest.Count;Compiler=$CompilerPath}
$buildInfo | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputFolder 'build-info.json') -Encoding utf8
$buildInfo
