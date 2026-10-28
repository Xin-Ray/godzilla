param([ValidateSet("EditMode","PlayMode")][string]$Platform = "EditMode", [string]$Filter = "")
$unity = "C:\Program Files\Unity\Hub\Editor\6000.2.15f1\Editor\Unity.exe"
$proj = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Force "$proj\TestResults" | Out-Null
$results = "$proj\TestResults\$Platform.xml"
$log = "$proj\logs\tests_$Platform.log"
if (Test-Path $results) { Remove-Item $results }
$args = @("-batchmode","-projectPath",$proj,"-runTests","-testPlatform",$Platform,"-testResults",$results,"-logFile",$log)
if ($Filter -ne "") { $args += @("-testFilter",$Filter) }
$p = Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru -NoNewWindow
if (-not (Test-Path $results)) { Write-Host "NO RESULTS FILE (unity exit $($p.ExitCode)); see $log"; exit 2 }
[xml]$x = Get-Content $results
$r = $x."test-run"
Write-Host ("{0}: total={1} passed={2} failed={3} skipped={4} (unity exit {5})" -f $Platform,$r.total,$r.passed,$r.failed,$r.skipped,$p.ExitCode)
$x.SelectNodes("//test-case[@result='Failed']") | ForEach-Object { Write-Host ("FAIL " + $_.fullname); Write-Host ("  " + $_.failure.message.'#cdata-section') }
exit [int]$r.failed
