$ErrorActionPreference='Stop'
$out=Join-Path (Split-Path $PSScriptRoot -Parent) 'distribution-tests/v2'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$sources=@('FeatureTests.cs','AccountStore.cs','AccountExtras.cs','DailyHistory.cs','WidgetBackup.cs','UpdatePolicy.cs','BoardUi.cs','WidgetUi.cs','MiniBarRenderer.cs','TaskbarWidget.cs')|ForEach-Object{Join-Path $PSScriptRoot $_}
& "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo /target:exe /main:FeatureTests /codepage:65001 "/out:$out/FeatureTests.exe" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll @sources
if($LASTEXITCODE){throw 'Feature implementation missing or compilation failed'}
& "$out/FeatureTests.exe"
if($LASTEXITCODE){throw 'Feature tests failed'}
