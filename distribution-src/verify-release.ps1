param([Parameter(Mandatory=$true)][string]$Manifest,[Parameter(Mandatory=$true)][string]$Archive)
$ErrorActionPreference='Stop'
$exe=Join-Path ([IO.Path]::GetTempPath()) ('WidgetReleaseCheck-'+[guid]::NewGuid().ToString('N')+'.exe')
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:ReleaseCheck /codepage:65001 "/out:$exe" /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "$PSScriptRoot\ReleaseCheck.cs" "$PSScriptRoot\UpdatePolicy.cs" "$PSScriptRoot\UpdateKey.cs" "$PSScriptRoot\BuildInfo.cs"
if($LASTEXITCODE){throw 'Release checker compilation failed'}
try{& $exe $Manifest $Archive;if($LASTEXITCODE){throw 'Release validation failed'}}finally{Remove-Item -LiteralPath $exe -Force -ErrorAction SilentlyContinue}
