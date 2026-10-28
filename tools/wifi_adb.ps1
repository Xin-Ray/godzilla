# Wireless adb bootstrap for Quest 3 (run once while USB is attached; then unplug).
$adb = "C:\Program Files\Unity\Hub\Editor\6000.2.15f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$usb = (& $adb devices) | Select-String -Pattern "^\S+\s+device$" | Where-Object { $_ -notmatch ":5555" } | ForEach-Object { ($_ -split "\s+")[0] } | Select-Object -First 1
if (-not $usb) { Write-Host "No USB device authorized. Plug in the Quest and accept the USB debugging dialog."; exit 1 }
$ip = ((& $adb -s $usb shell ip -f inet addr show wlan0) | Select-String -Pattern "inet (\d+\.\d+\.\d+\.\d+)").Matches[0].Groups[1].Value
Write-Host "Headset IP: $ip"
& $adb -s $usb tcpip 5555 | Out-Null
Start-Sleep -Seconds 2
& $adb connect "${ip}:5555"
Write-Host "If the headset shows a second authorize dialog, tap 'Always allow'. Then: .\tools\deploy_quest.ps1 -Serial ${ip}:5555"
& $adb devices
