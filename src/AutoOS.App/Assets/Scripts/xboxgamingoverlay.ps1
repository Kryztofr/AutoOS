reg load HKU\TEMP "$env:LOCALAPPDATA\Packages\Microsoft.XboxGamingOverlay_8wekyb3d8bbwe\Settings\settings.dat" >$null

$regContent = @'
Windows Registry Editor Version 5.00

[HKEY_USERS\TEMP\LocalState]
"AppTheme"=hex(5f5e107):00,00,00,00,00,00,00,00,1f,43,21,af,c9,d4,dc,01
"ClosedControllerBarTip"=hex(5f5e10b):01,43,8e,00,d5,c9,d4,dc,01
"CompactModeEnabled"=hex(5f5e10b):01,a9,17,87,dc,c9,d4,dc,01
"DesktopCompactModeUserPreference"=hex(5f5e10b):01,a9,17,87,dc,c9,d4,dc,01
"FeedbackNotifications"=hex(5f5e10b):00,e7,da,9e,92,c9,d4,dc,01
"HdrNotifications"=hex(5f5e10b):00,61,28,3e,95,c9,d4,dc,01
"RecordingNotifications"=hex(5f5e10b):01,c7,40,17,88,c9,d4,dc,01
"SuppressEnableCompactModeFlyout"=hex(5f5e10b):01,a9,17,87,dc,c9,d4,dc,01
"SuppressFullscreenNotifications"=hex(5f5e10b):01,81,23,85,97,c9,d4,dc,01
'@

New-Item "$env:TEMP\XboxGamingOverlay.reg" -Value $regContent -Force | Out-Null

regedit.exe /s "$env:TEMP\XboxGamingOverlay.reg"
Start-Sleep 1
reg unload HKU\TEMP >$null
Remove-Item "$env:TEMP\XboxGamingOverlay.reg" -Force -ErrorAction SilentlyContinue