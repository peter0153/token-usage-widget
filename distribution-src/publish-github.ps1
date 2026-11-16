param([ValidateSet('build','manifest')][string]$Phase)
$ErrorActionPreference='Stop'
if($env:GITHUB_REPOSITORY -ne 'peter0153/token-usage-widget' -or -not $env:GITHUB_TOKEN){throw 'Run this only in the authorized repository workflow'}
$repo=$env:GITHUB_REPOSITORY
$root=Split-Path $PSScriptRoot -Parent
$version=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'version.txt')).Trim()
if($version -notmatch '^\d+\.\d+\.\d+$'){throw 'Invalid version'}
$headers=@{Authorization=('Bearer '+$env:GITHUB_TOKEN);Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2026-03-10'}
$api='https://api.github.com/repos/'+$repo
function Upload-ReleaseAsset($release,$file){$name=[IO.Path]::GetFileName($file);if(@($release.assets|Where-Object name -eq $name).Count){throw ('Release asset already exists: '+$name)};$url=$release.upload_url -replace '\{.*$','';Invoke-RestMethod -Method Post -Uri ($url+'?name='+[Uri]::EscapeDataString($name)) -Headers $headers -ContentType 'application/octet-stream' -InFile $file | Out-Null}
if($Phase -eq 'build'){
 $existing=$null
 try{$existing=Invoke-RestMethod -Uri ($api+'/releases/tags/v'+$version) -Headers $headers}catch{if([int]$_.Exception.Response.StatusCode -ne 404){throw}}
 if($existing){Write-Output ('Version '+$version+' already has a release; assets were not replaced. Increase version.txt for a new release.');return}
 $out=Join-Path $root ('output/windows-release/'+$version)
 $archive=Join-Path $out ('TokenWidget-'+$version+'-win64.zip')
 if(-not (Test-Path -LiteralPath $archive)){throw 'Validated build archive missing'}
 $release=Invoke-RestMethod -Method Post -Uri ($api+'/releases') -Headers $headers -ContentType 'application/json' -Body (@{tag_name=('v'+$version);target_commitish=$env:GITHUB_SHA;name=('내 AI 한도 '+$version);body='Windows 사용량 위젯. 서명된 업데이트 정보 검증 후 정식 배포됩니다.';draft=$false;prerelease=$true;make_latest='false'}|ConvertTo-Json)
 Upload-ReleaseAsset $release $archive
 Upload-ReleaseAsset $release (Join-Path $out 'SHA256.txt')
 Write-Output ('Prerelease prepared: '+$release.html_url)
}else{
 $manifest=Join-Path $root 'release/update.json'
 $envelope=Get-Content -LiteralPath $manifest -Raw|ConvertFrom-Json
 $info=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($envelope.payload))|ConvertFrom-Json
 $expected='https://github.com/'+$repo+'/releases/download/v'+$version+'/TokenWidget-'+$version+'-win64.zip'
 if($info.version -ne $version -or $info.package_url -ne $expected){throw 'Manifest target mismatch'}
 $release=Invoke-RestMethod -Uri ($api+'/releases/tags/v'+$version) -Headers $headers
 if(-not $release.prerelease){throw 'Release is already final; use a new version'}
 $archive=Join-Path $env:RUNNER_TEMP ('TokenWidget-'+$version+'-win64.zip')
 Invoke-WebRequest -Uri $expected -OutFile $archive
 & "$PSScriptRoot\verify-release.ps1" -Manifest $manifest -Archive $archive
 Upload-ReleaseAsset $release $manifest
 Invoke-RestMethod -Method Patch -Uri ($api+'/releases/'+$release.id) -Headers $headers -ContentType 'application/json' -Body (@{prerelease=$false;make_latest='true';body=('Windows '+$version+': 라이트 회청색 배경과 밝은 카드, 미니바 둥근 모서리 바깥 투명 처리. ZIP을 모두 풀고 설치.cmd를 실행하면 사용자 폴더에 설치하고 바로가기를 만듭니다. GitHub 계정은 필요하지 않습니다. 설치 안내: https://github.com/'+$repo+'#설치와-사용')}|ConvertTo-Json) | Out-Null
 Write-Output ('Release published: '+$release.html_url)
}
