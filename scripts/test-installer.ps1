param([switch]$SkipGameTests)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$build = Get-Content -LiteralPath (Join-Path $projectRoot 'artifacts/installer/build-info.json') -Raw | ConvertFrom-Json
$registryKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{469D0595-E34B-4A30-93A3-89D31AD4D134}_is1'
if (Test-Path -LiteralPath $registryKey) { throw 'Mahjong is already installed. Test refused to protect the existing installation.' }
$testRoot = Join-Path $projectRoot ('artifacts/installer-tests/' + [guid]::NewGuid().ToString('N'))
$appDir = Join-Path $testRoot 'app'
$groupName = 'Mahjong Setup Test ' + [guid]::NewGuid().ToString('N')
$groupDir = Join-Path ([Environment]::GetFolderPath('Programs')) $groupName
$desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Mahjong Solitaire.lnk'
$desktopExisted = Test-Path -LiteralPath $desktopLink
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$log = [Collections.Generic.List[string]]::new()
function Assert-Check([bool]$condition,[string]$name) {
    if (!$condition) { throw "FAIL: $name" }
    $log.Add('PASS '+$name)
    Add-Content -LiteralPath (Join-Path $testRoot 'progress.txt') -Value ('PASS '+$name)
}
function Snapshot-Data {
    $dataPath = Join-Path $env:LOCALAPPDATA 'QuietMahjong'
    $entries = @(foreach ($file in @('game.json','settings.json','results.json','background.png')) {
        $path = Join-Path $dataPath $file
        if (Test-Path -LiteralPath $path) { "$file=$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)" }
    })
    return ($entries -join '|')
}
function Check-Payload {
    $manifest = Get-Content -LiteralPath (Join-Path $build.BuildFolder 'payload-manifest.json') -Raw | ConvertFrom-Json
    foreach ($file in $manifest) {
        $path = Join-Path $appDir $file.Path
        Assert-Check (Test-Path -LiteralPath $path) ('Installed: '+$file.Path)
        Assert-Check ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLower() -eq $file.SHA256) ('SHA256: '+$file.Path)
    }
    Assert-Check (!(Test-Path -LiteralPath (Join-Path $appDir 'SmokeData'))) 'Test save data is not shipped'
}
function Install-Test([string]$language,[string]$tasks,[string]$logName) {
    $arguments = @('/VERYSILENT','/SUPPRESSMSGBOXES','/SP-','/NORESTART','/NOCLOSEAPPLICATIONS',
        ('/LANG='+$language),('/DIR="'+$appDir+'"'),('/GROUP="'+$groupName+'"'),('/TASKS="'+$tasks+'"'),('/LOG="'+(Join-Path $testRoot $logName)+'"'))
    $process = Start-Process -FilePath $build.Setup -ArgumentList $arguments -PassThru -WindowStyle Hidden -Wait
    Assert-Check ($process.ExitCode -eq 0) ('Setup '+$language+' exit code 0')
}
$initialData = Snapshot-Data
$installed = $false
$gameProcess = $null
try {
    Install-Test 'polish' '' 'install-polish.log'
    $installed = $true
    Assert-Check (Test-Path -LiteralPath $registryKey) 'Application registered for current user'
    $registration = Get-ItemProperty -LiteralPath $registryKey
    Assert-Check ($registration.DisplayVersion -eq $build.Version) 'Registered version matches release'
    Assert-Check ($registration.InstallLocation.TrimEnd('\') -eq $appDir.TrimEnd('\')) 'Registered install location is isolated test directory'
    Check-Payload
    Assert-Check (Test-Path -LiteralPath (Join-Path $groupDir 'Mahjong Solitaire.lnk')) 'Start menu game shortcut created'
    Assert-Check (Test-Path -LiteralPath (Join-Path $groupDir 'Instrukcja gry.lnk')) 'Polish help shortcut created'
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut((Join-Path $groupDir 'Mahjong Solitaire.lnk'))
    Assert-Check ($shortcut.TargetPath -eq (Join-Path $appDir 'Mahjong.exe')) 'Start menu shortcut target correct'
    Assert-Check ((Test-Path -LiteralPath $desktopLink) -eq $desktopExisted) 'Desktop shortcut opt-out respected'
    Assert-Check ((Snapshot-Data) -eq $initialData) 'Player files unchanged after installation'
    $marker = Join-Path $appDir 'player-note-test.txt'
    Set-Content -LiteralPath $marker -Value 'User-created file: preserve during upgrade and uninstall.'
    $markerHash = (Get-FileHash -LiteralPath $marker -Algorithm SHA256).Hash
    $upgradeTasks = if ($desktopExisted) { '' } else { 'desktopicon' }
    Install-Test 'english' $upgradeTasks 'upgrade-english.log'
    Check-Payload
    Assert-Check ((Get-FileHash -LiteralPath $marker -Algorithm SHA256).Hash -eq $markerHash) 'Upgrade preserves unknown user-created file'
    Assert-Check (Test-Path -LiteralPath (Join-Path $groupDir 'How to play.lnk')) 'English help shortcut created'
    if (!$desktopExisted) {
        Assert-Check (Test-Path -LiteralPath $desktopLink) 'Desktop shortcut opt-in respected'
        $shortcut = $shell.CreateShortcut($desktopLink)
        Assert-Check ($shortcut.TargetPath -eq (Join-Path $appDir 'Mahjong.exe')) 'Desktop shortcut target correct'
    }
    Assert-Check ((Snapshot-Data) -eq $initialData) 'Player files unchanged after upgrade'
    if (!$SkipGameTests) {
        $gameOutput = Join-Path $testRoot 'game-tests'
        $gameProcess = Start-Process -FilePath (Join-Path $appDir 'Mahjong.exe') -ArgumentList '--smoke','--smoke-output',('"'+$gameOutput+'"') -PassThru -WindowStyle Hidden
        if (!$gameProcess.WaitForExit(180000)) { throw 'Installed game test timed out.' }
        Assert-Check ($gameProcess.ExitCode -eq 0) 'Installed self-contained game exits full WPF suite successfully'
        $gameLog = Get-Content -LiteralPath (Join-Path $gameOutput 'ui-tests.txt') -Raw
        Assert-Check ($gameLog -match 'RESULT: 459 WPF checks passed\.') 'All 459 WPF checks pass from installed location'
        Assert-Check ((Snapshot-Data) -eq $initialData) 'Game tests did not change real player files'
    }
} catch {
    $log.Add('FAIL '+$_)
    throw
} finally {
    if ($installed) {
        if ($gameProcess -and !$gameProcess.HasExited) { Stop-Process -Id $gameProcess.Id }
        $resolvedApp = [IO.Path]::GetFullPath($appDir)
        if (!$resolvedApp.StartsWith($projectRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe uninstall target.' }
        if ((Get-ItemProperty -LiteralPath $registryKey).InstallLocation.TrimEnd('\') -ne $appDir.TrimEnd('\')) { throw 'Registration no longer points to the test directory. Uninstall refused.' }
        $uninstaller = Join-Path $appDir 'unins000.exe'
        $process = Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $testRoot 'uninstall.log')+'"') -WindowStyle Hidden -PassThru -Wait
        Assert-Check ($process.ExitCode -eq 0) 'Uninstall exit code 0'
        Assert-Check (!(Test-Path -LiteralPath (Join-Path $appDir 'Mahjong.exe'))) 'Uninstall removes installed executable'
        Assert-Check (!(Test-Path -LiteralPath (Join-Path $appDir 'coreclr.dll'))) 'Uninstall removes bundled runtime'
        Assert-Check (!(Test-Path -LiteralPath $registryKey)) 'Uninstall removes application registration'
        Assert-Check (!(Test-Path -LiteralPath (Join-Path $groupDir 'Mahjong Solitaire.lnk'))) 'Uninstall removes Start menu shortcut'
        if (!$desktopExisted) { Assert-Check (!(Test-Path -LiteralPath $desktopLink)) 'Uninstall removes its desktop shortcut' }
        Assert-Check ((Get-FileHash -LiteralPath $marker -Algorithm SHA256).Hash -eq $markerHash) 'Uninstall preserves unknown user-created file'
        Assert-Check ((Snapshot-Data) -eq $initialData) 'Uninstall preserves all real player files'
    }
    $log.Add('RESULT: '+(@($log | Where-Object {$_ -like 'PASS *'}).Count)+' checks passed; '+(@($log | Where-Object {$_ -like 'FAIL *'}).Count)+' failed.')
    $log | Set-Content -LiteralPath (Join-Path $testRoot 'installer-tests.txt') -Encoding utf8
    Set-Content -LiteralPath (Join-Path $projectRoot 'artifacts/installer/last-test-dir.txt') -Value $testRoot
}
Get-Content -LiteralPath (Join-Path $testRoot 'installer-tests.txt') -Tail 1
Write-Host "Logs: $testRoot"
