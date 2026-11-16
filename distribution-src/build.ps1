$ErrorActionPreference = 'Stop'
$widgetVersion=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'version.txt')).Trim()
if($widgetVersion -notmatch '^\d+\.\d+\.\d+$'){throw 'version.txt must use major.minor.patch'}
$widgetBuildInfo='using System.Reflection;'+[Environment]::NewLine+'[assembly:AssemblyVersion("'+$widgetVersion+'.0")]'+[Environment]::NewLine+'[assembly:AssemblyFileVersion("'+$widgetVersion+'.0")]'+[Environment]::NewLine+'static class BuildInfo {public const string Version="'+$widgetVersion+'";public const string Repository="peter0153/token-usage-widget";}'
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'BuildInfo.cs'),$widgetBuildInfo,[Text.UTF8Encoding]::new($false))
$widgetManifest=Join-Path $PSScriptRoot 'app.manifest'
$widgetXml=[IO.File]::ReadAllText($widgetManifest) -replace 'assemblyIdentity version="[^"]+"',('assemblyIdentity version="'+$widgetVersion+'.0"')
[IO.File]::WriteAllText($widgetManifest,$widgetXml,[Text.UTF8Encoding]::new($false))
$widgetVendor = Join-Path $PSScriptRoot 'vendor'
$widgetRuntime = Join-Path $widgetVendor 'runtime\codex-app-server-x86_64-pc-windows-msvc.exe'
$widgetExpectedHash = '0628f6a5d5e304b7970dc0f53abd4a3ff084e9e14b79429ac0ebf92e37329412'
if ((Get-FileHash -LiteralPath $widgetRuntime -Algorithm SHA256).Hash.ToLowerInvariant() -ne $widgetExpectedHash) { throw 'Runtime integrity failed.' }
$widgetInfo = 'static class RuntimeInfo { public const string Sha256 = "' + $widgetExpectedHash + '"; public const string EntryName = "codex-app-server-x86_64-pc-windows-msvc.exe"; }'
Set-Content -LiteralPath (Join-Path $PSScriptRoot 'RuntimeInfo.cs') -Value $widgetInfo -Encoding utf8
$widgetOut = Join-Path (Split-Path $PSScriptRoot -Parent) '배포용'
New-Item -ItemType Directory -Path $widgetOut -Force | Out-Null
Add-Type -AssemblyName System.Drawing
$widgetBitmap = [Drawing.Bitmap]::new(64,64)
$widgetGraphics = [Drawing.Graphics]::FromImage($widgetBitmap)
$widgetGraphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$widgetGraphics.Clear([Drawing.Color]::FromArgb(18,23,31))
$widgetPen = [Drawing.Pen]::new([Drawing.Color]::FromArgb(115,228,191),5)
$widgetGraphics.DrawArc($widgetPen,9,9,46,46,125,290)
$widgetFont = [Drawing.Font]::new('Segoe UI',21,[Drawing.FontStyle]::Bold)
$widgetGraphics.DrawString('%',$widgetFont,[Drawing.Brushes]::White,13,12)
$widgetIcon = [Drawing.Icon]::FromHandle($widgetBitmap.GetHicon())
$widgetIconStream = [IO.File]::Create((Join-Path $PSScriptRoot 'app.ico'))
$widgetIcon.Save($widgetIconStream)
$widgetIconStream.Dispose()
$widgetIcon.Dispose()
$widgetGraphics.Dispose()
$widgetBitmap.Dispose()
$widgetPen.Dispose()
$widgetFont.Dispose()
$widgetCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $widgetCompiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /main:WidgetUpdater "/out:$widgetOut\WidgetUpdater.exe" /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "$PSScriptRoot\WidgetUpdater.cs" "$PSScriptRoot\InstallTransaction.cs" "$PSScriptRoot\UpdatePolicy.cs" "$PSScriptRoot\BuildInfo.cs" "$PSScriptRoot\UpdateKey.cs"
if($LASTEXITCODE -ne 0){throw 'Updater compilation failed'}
$widgetHelperHash=(Get-FileHash -LiteralPath "$widgetOut\WidgetUpdater.exe" -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'HelperInfo.cs'),'static class HelperInfo { public const string Sha256 = "'+$widgetHelperHash+'"; }',[Text.UTF8Encoding]::new($false))
& $widgetCompiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /main:WidgetLauncher "/out:$widgetOut\TokenWidget.exe" "/win32manifest:$PSScriptRoot\app.manifest" "/win32icon:$PSScriptRoot\app.ico" /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "$PSScriptRoot\WidgetLauncher.cs" "$PSScriptRoot\InstallTransaction.cs" "$PSScriptRoot\BuildInfo.cs"
if($LASTEXITCODE -ne 0){throw 'Launcher compilation failed'}
$widgetLauncherHash=(Get-FileHash -LiteralPath "$widgetOut\TokenWidget.exe" -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'LauncherInfo.cs'),'static class LauncherInfo { public const string Sha256 = "'+$widgetLauncherHash+'"; }',[Text.UTF8Encoding]::new($false))
$widgetNames=@('UsageWidget.cs','BoardWindow.cs','BoardWindow.Layout.cs','BoardWindow.Verify.cs','BoardWindow.FeatureVerify.cs','BoardWindow.Hint.cs','BoardWindow.History.cs','WidgetUi.cs','MiniWidget.cs','TaskbarWidget.cs','MiniBarRenderer.cs','BoardUi.cs','AccountStore.cs','AccountExtras.cs','DailyHistory.cs','WidgetInstaller.cs','WidgetBackup.cs','InstallTransaction.cs','UpdateClient.cs','UpdatePolicy.cs','UpdateKey.cs','BuildInfo.cs','HelperInfo.cs','LauncherInfo.cs','LocalUsage.cs','AppPaths.cs','RuntimeInfo.cs')
$widgetSource=@($widgetNames|ForEach-Object{Join-Path $PSScriptRoot $_})
& $widgetCompiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /main:UsageWidget "/out:$widgetOut\CodexUsageWidget.exe" "/win32manifest:$PSScriptRoot\app.manifest" "/win32icon:$PSScriptRoot\app.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/resource:$widgetVendor\codex-app-server-0.163.0.zip,runtime.zip" "/resource:$widgetVendor\Codex-LICENSE.txt,Codex-LICENSE.txt" "/resource:$widgetVendor\Codex-NOTICE.txt,Codex-NOTICE.txt" @widgetSource
if ($LASTEXITCODE -ne 0) { throw 'Distribution build failed.' }
Copy-Item -LiteralPath (Join-Path $widgetVendor 'Codex-LICENSE.txt'),(Join-Path $widgetVendor 'Codex-NOTICE.txt') -Destination $widgetOut
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README-distribution.txt') -Destination (Join-Path $widgetOut '먼저 읽어주세요.txt') -Force
$widgetExe = Join-Path $widgetOut 'CodexUsageWidget.exe'
$widgetExeHash = (Get-FileHash -LiteralPath $widgetExe -Algorithm SHA256).Hash
Set-Content -LiteralPath (Join-Path $widgetOut 'SHA256.txt') -Value "$widgetExeHash  CodexUsageWidget.exe" -Encoding utf8
Get-Item -LiteralPath $widgetExe | Select-Object FullName,Length
