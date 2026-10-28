$unity = "C:\Program Files\Unity\Hub\Editor\6000.2.15f1\Editor\Unity.exe"
$proj = Split-Path -Parent $PSScriptRoot
$log = "$proj\logs\build_quest.log"
$p = Start-Process -FilePath $unity -ArgumentList @("-batchmode","-quit","-nographics","-buildTarget","Android","-projectPath",$proj,"-executeMethod","Godzilla.Editor.QuestBatchBuild.Build","-logFile",$log) -Wait -PassThru -NoNewWindow
Write-Host "unity exit $($p.ExitCode)"
if (Test-Path "$proj\Builds\Godzilla.apk") { Write-Host ("APK: {0:N1} MB" -f ((Get-Item "$proj\Builds\Godzilla.apk").Length/1MB)) } else { Write-Host "NO APK; see $log" }
exit $p.ExitCode
