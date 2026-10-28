param([string]$Serial = "", [switch]$NoLogcat)
$adb = "C:\Program Files\Unity\Hub\Editor\6000.2.15f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$proj = Split-Path -Parent $PSScriptRoot
$apk = "$proj\Builds\Godzilla.apk"
$s = @(); if ($Serial -ne "") { $s = @("-s",$Serial) }
& $adb @s install -r $apk
& $adb @s shell am force-stop com.xin.godzilla
& $adb @s logcat -c
& $adb @s shell am start -n com.xin.godzilla/com.unity3d.player.UnityPlayerGameActivity
if (-not $NoLogcat) { & $adb @s logcat -v time | Select-String -Pattern "\[City\]|\[Window\]|\[Place\]|\[Giant\]|\[App\]|Unity   :|AndroidRuntime|FATAL" }
