$ErrorActionPreference='Stop'
$out=Join-Path (Split-Path $PSScriptRoot -Parent) ('distribution-tests/v2/transaction-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $out -Force | Out-Null
$rsa=[Security.Cryptography.RSACryptoServiceProvider]::new(2048);$rsa.PersistKeyInCsp=$false
try{[IO.File]::WriteAllText((Join-Path $out 'fixture-key.xml'),$rsa.ToXmlString($true));[IO.File]::WriteAllText((Join-Path $out 'TestKey.cs'),'static class UpdateKey { public const string PublicKey = @"'+$rsa.ToXmlString($false).Replace('"','""')+'";}')}finally{$rsa.Dispose()}
& "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo /target:exe /main:TransactionTests /codepage:65001 "/out:$out\TransactionTests.exe" /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:Microsoft.CSharp.dll "$PSScriptRoot\TransactionTests.cs" "$PSScriptRoot\WidgetUpdater.cs" "$PSScriptRoot\InstallTransaction.cs" "$PSScriptRoot\UpdatePolicy.cs" "$PSScriptRoot\BuildInfo.cs" "$out\TestKey.cs"
if($LASTEXITCODE){throw 'Transaction test compilation failed'}
& "$out\TransactionTests.exe" "$out\fixture-key.xml"
if($LASTEXITCODE){throw 'Transaction tests failed'}
