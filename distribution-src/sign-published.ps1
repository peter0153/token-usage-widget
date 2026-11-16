param([Parameter(Mandatory=$true)][string]$Archive,[string]$Notes='메뉴 유지 수정, 가변 미니바, 5일/10일 일별 토큰 그래프, 초기화권 확인·사용량 링크, 기본 다크 테마')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$version=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'version.txt')).Trim()
if($version -notmatch '^\d+\.\d+\.\d+$' -or [IO.Path]::GetFileName($Archive) -ne ('TokenWidget-'+$version+'-win64.zip')){throw 'Archive/version mismatch'}
$out=Join-Path $root ('output/windows-release/'+$version+'/published-manifest')
New-Item -ItemType Directory -Path $out -Force | Out-Null
$hash=(Get-FileHash -LiteralPath $Archive).Hash.ToLowerInvariant()
$payload=[ordered]@{version=$version;package_url=('https://github.com/peter0153/token-usage-widget/releases/download/v'+$version+'/TokenWidget-'+$version+'-win64.zip');sha256=$hash;size=(Get-Item -LiteralPath $Archive).Length;notes=$Notes}|ConvertTo-Json -Compress
$bytes=[Text.Encoding]::UTF8.GetBytes($payload)
$rsa=[Security.Cryptography.RSACryptoServiceProvider]::new();$rsa.PersistKeyInCsp=$false
try{$rsa.FromXmlString([IO.File]::ReadAllText((Join-Path $root '.release-signing/update-key.xml')));$envelope=@{payload=[Convert]::ToBase64String($bytes);signature=[Convert]::ToBase64String($rsa.SignData($bytes,'SHA256'))}|ConvertTo-Json -Compress;[IO.File]::WriteAllText((Join-Path $out 'update.json'),$envelope,[Text.UTF8Encoding]::new($false))}finally{$rsa.Dispose()}
[IO.File]::WriteAllText((Join-Path $out 'SHA256.txt'),$hash+'  '+[IO.Path]::GetFileName($Archive)+[Environment]::NewLine)
& "$PSScriptRoot\verify-release.ps1" -Manifest (Join-Path $out 'update.json') -Archive $Archive
Get-Item -LiteralPath (Join-Path $out 'update.json') | Select-Object FullName,Length
