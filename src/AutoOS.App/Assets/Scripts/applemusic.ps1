reg load HKU\TEMP "$env:LOCALAPPDATA\Packages\AppleInc.AppleMusicWin_nzyj5cx40ttqa\Settings\settings.dat" >$null

$regContent = @'
Windows Registry Editor Version 5.00

[HKEY_USERS\TEMP\LocalState]
"KeepMiniplayerOnTop"=hex(5f5e10b):01,b9,5d,cc,e4,9a,13,dc,01
'@

New-Item "$env:TEMP\AppleMusic.reg" -Value $regContent -Force | Out-Null

regedit.exe /s "$env:TEMP\AppleMusic.reg"
Start-Sleep 1
reg unload HKU\TEMP >$null
Remove-Item "$env:TEMP\AppleMusic.reg" -Force -ErrorAction SilentlyContinue