param([string]$ReportFolder)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(-not $ReportFolder){$ReportFolder=Join-Path $root ('distribution-tests/v2/qa-'+[guid]::NewGuid().ToString('N'))}
New-Item -ItemType Directory -Path $ReportFolder -Force | Out-Null
$statusFile=Join-Path $env:LOCALAPPDATA 'TeacherCodexUsageWidget/status.json'
$wasRunning=$false
if(Test-Path -LiteralPath $statusFile){try{$previous=Get-Content -LiteralPath $statusFile -Raw|ConvertFrom-Json;if($previous.running){$previousProcess=Get-Process -Id $previous.pid -ErrorAction Stop;$expected=Join-Path $env:LOCALAPPDATA 'Programs/TeacherCodexUsageWidget/CodexUsageWidget.exe';if($previousProcess.Path -eq $expected){$wasRunning=$true;$event=[Threading.EventWaitHandle]::OpenExisting('Local\TeacherCodexUsageWidget.Exit');try{[void]$event.Set()}finally{$event.Dispose()};if(-not $previousProcess.WaitForExit(10000)){throw 'Graceful widget exit failed'}}}}catch{if($wasRunning){throw}}}
try{$process=Start-Process -FilePath (Join-Path $root '배포용/CodexUsageWidget.exe') -ArgumentList ('--verify-board --data-dir "'+$ReportFolder+'\data" --report-dir "'+$ReportFolder+'\report"') -PassThru -WindowStyle Hidden
if(-not $process.WaitForExit(35000)){Stop-Process -Id $process.Id;throw 'UI verification timed out'}
$result=Get-Content -LiteralPath (Join-Path $ReportFolder 'report/ui-verification.json') -Raw|ConvertFrom-Json
$failures=@($result.PSObject.Properties|Where-Object {$_.Value -is [bool] -and -not $_.Value})
[ordered]@{report=$ReportFolder;version=$result.version;failures=@($failures.Name);menuCloseReason=$result.menuCloseReason}|ConvertTo-Json -Compress
if($failures.Count -or $result.taskbarError){throw 'UI verification failed'}
}finally{if($wasRunning){Start-Process -FilePath (Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'Codex 사용량 위젯.lnk') -WindowStyle Hidden}}
