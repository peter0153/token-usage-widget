$ErrorActionPreference = 'Stop'
$widgetDownloadRoot = Join-Path $PSScriptRoot 'vendor'
New-Item -ItemType Directory -Path $widgetDownloadRoot -Force | Out-Null
$widgetRuntimeZip = Join-Path $widgetDownloadRoot 'codex-app-server-0.163.0.zip'
$widgetRuntimeUrl = 'https://github.com/openai/codex/releases/download/rust-v0.163.0/codex-app-server-x86_64-pc-windows-msvc.exe.zip'
$widgetRuntimeHash = 'c12aef2643b5e8b93b8626e4fe81df7fd6793ed597e60790fdd4837a6d45122c'
if (-not (Test-Path -LiteralPath $widgetRuntimeZip) -or (Get-FileHash -LiteralPath $widgetRuntimeZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $widgetRuntimeHash) {
    & "$env:WINDIR\System32\curl.exe" --fail --location --ssl-revoke-best-effort --retry 2 --retry-delay 2 --silent --show-error --output $widgetRuntimeZip $widgetRuntimeUrl
    if ($LASTEXITCODE -ne 0) { throw 'Official runtime download failed.' }
}
if ((Get-FileHash -LiteralPath $widgetRuntimeZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $widgetRuntimeHash) {
    throw 'Official runtime SHA-256 mismatch.'
}
Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/openai/codex/rust-v0.163.0/LICENSE' -OutFile (Join-Path $widgetDownloadRoot 'Codex-LICENSE.txt')
Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/openai/codex/rust-v0.163.0/NOTICE' -OutFile (Join-Path $widgetDownloadRoot 'Codex-NOTICE.txt')
Expand-Archive -LiteralPath $widgetRuntimeZip -DestinationPath (Join-Path $widgetDownloadRoot 'runtime') -Force
Get-ChildItem -LiteralPath $widgetDownloadRoot -Recurse -File | Select-Object Name,Length
Get-Content -LiteralPath (Join-Path $widgetDownloadRoot 'Codex-NOTICE.txt') -TotalCount 12
