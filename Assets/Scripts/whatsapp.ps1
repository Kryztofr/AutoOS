reg load HKU\TEMP "$env:LOCALAPPDATA\Packages\5319275A.WhatsAppDesktop_cv1g1gvanyjgm\Settings\settings.dat" >$null

$regContent = @'
Windows Registry Editor Version 5.00

[HKEY_USERS\TEMP\LocalState\web_preferences]
"WindowsIsSystemTrayEnabled"=hex(5f5e10c):66,00,61,00,6c,00,73,00,65,00,00,00,\
  2d,b8,83,d6,f4,98,dc,01
'@

New-Item "$env:TEMP\WhatsApp.reg" -Value $regContent -Force | Out-Null

regedit.exe /s "$env:TEMP\WhatsApp.reg"
Start-Sleep 1
reg unload HKU\TEMP >$null
Remove-Item "$env:TEMP\WhatsApp.reg" -Force -ErrorAction SilentlyContinue