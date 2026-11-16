param([string]$Notes='Compact Board UI, isolated Codex accounts, taskbar mini bar and signed update checks.',[switch]$Unsigned)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$version=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'version.txt')).Trim()
$source=Join-Path $root '배포용'
$out=Join-Path $root ('output/windows-release/'+$version)
$package=Join-Path $out 'package'
New-Item -ItemType Directory -Path $package -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $source 'CodexUsageWidget.exe'),(Join-Path $source 'WidgetUpdater.exe'),(Join-Path $source 'TokenWidget.exe'),(Join-Path $source 'Codex-LICENSE.txt'),(Join-Path $source 'Codex-NOTICE.txt') -Destination $package -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README-distribution.txt') -Destination (Join-Path $package 'README.txt') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'License-Windows.txt') -Destination (Join-Path $package 'LICENSE.txt') -Force
$cmd='@echo off'+[Environment]::NewLine+'start "" "%~dp0CodexUsageWidget.exe" --install'+[Environment]::NewLine
[IO.File]::WriteAllText((Join-Path $package '설치.cmd'),$cmd,[Text.Encoding]::ASCII)
$allowed=@('CodexUsageWidget.exe','WidgetUpdater.exe','TokenWidget.exe','Codex-LICENSE.txt','Codex-NOTICE.txt','README.txt','LICENSE.txt','설치.cmd')
$extra=Get-ChildItem -LiteralPath $package | Where-Object {$_.Name -notin $allowed -or $_.PSIsContainer}
if($extra){throw 'Unexpected package content'}
$zip=Join-Path $out "TokenWidget-$version-win64.zip"
Compress-Archive -LiteralPath @($allowed|ForEach-Object{Join-Path $package $_}) -DestinationPath $zip -CompressionLevel Optimal -Force
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$payload=[ordered]@{version=$version;package_url="https://github.com/peter0153/token-usage-widget/releases/download/v$version/TokenWidget-$version-win64.zip";sha256=$hash;size=(Get-Item -LiteralPath $zip).Length;notes=$Notes}|ConvertTo-Json -Compress
$bytes=[Text.Encoding]::UTF8.GetBytes($payload)
if(-not $Unsigned){$rsa=[Security.Cryptography.RSACryptoServiceProvider]::new();$rsa.PersistKeyInCsp=$false
try{$rsa.FromXmlString([IO.File]::ReadAllText((Join-Path $root '.release-signing/update-key.xml')));$envelope=@{payload=[Convert]::ToBase64String($bytes);signature=[Convert]::ToBase64String($rsa.SignData($bytes,'SHA256'))}|ConvertTo-Json -Compress;[IO.File]::WriteAllText((Join-Path $out 'update.json'),$envelope,[Text.UTF8Encoding]::new($false))}finally{$rsa.Dispose()}}
[IO.File]::WriteAllText((Join-Path $out 'SHA256.txt'),$hash+"  TokenWidget-$version-win64.zip"+[Environment]::NewLine)
Get-Item -LiteralPath $zip | Select-Object Name,Length
if(-not $Unsigned){Get-Item -LiteralPath (Join-Path $out 'update.json') | Select-Object Name,Length}
