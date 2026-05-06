reg load HKU\TEMP "$env:LOCALAPPDATA\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\Settings\settings.dat" >$null

$regContent = @'
Windows Registry Editor Version 5.00

[HKEY_USERS\TEMP\LocalState]
"AutoCorrect"=hex(5f5e10b):00,cd,ff,04,45,95,13,dc,01
"GhostFile"=hex(5f5e10b):00,fc,13,31,4b,95,13,dc,01
"OpenFile"=hex(5f5e104):01,00,00,00,9f,01,46,4c,95,13,dc,01
"RecentFilesEnabled"=hex(5f5e10b):00,64,5d,84,4a,95,13,dc,01
"WordWrap"=hex(5f5e10b):00,ce,88,91,ac,84,8c,dc,01
"SpellCheckState"=hex(5f5e10c):7b,00,22,00,45,00,6e,00,61,00,62,00,6c,00,65,00,\
  64,00,22,00,3a,00,66,00,61,00,6c,00,73,00,65,00,2c,00,22,00,46,00,69,00,6c,\
  00,65,00,45,00,78,00,74,00,65,00,6e,00,73,00,69,00,6f,00,6e,00,73,00,4f,00,\
  76,00,65,00,72,00,72,00,69,00,64,00,65,00,73,00,22,00,3a,00,5b,00,5b,00,22,\
  00,2e,00,6d,00,64,00,22,00,2c,00,74,00,72,00,75,00,65,00,5d,00,2c,00,5b,00,\
  22,00,2e,00,61,00,73,00,73,00,22,00,2c,00,74,00,72,00,75,00,65,00,5d,00,2c,\
  00,5b,00,22,00,2e,00,6c,00,69,00,63,00,22,00,2c,00,74,00,72,00,75,00,65,00,\
  5d,00,2c,00,5b,00,22,00,2e,00,73,00,72,00,74,00,22,00,2c,00,74,00,72,00,75,\
  00,65,00,5d,00,2c,00,5b,00,22,00,2e,00,6c,00,72,00,63,00,22,00,2c,00,74,00,\
  72,00,75,00,65,00,5d,00,2c,00,5b,00,22,00,2e,00,74,00,78,00,74,00,22,00,2c,\
  00,74,00,72,00,75,00,65,00,5d,00,5d,00,7d,00,00,00,02,de,19,b1,84,8c,dc,01
'@

New-Item "$env:TEMP\Notepad.reg" -Value $regContent -Force | Out-Null

regedit.exe /s "$env:TEMP\Notepad.reg"
Start-Sleep 1
reg unload HKU\TEMP >$null
Remove-Item "$env:TEMP\Notepad.reg" -Force -ErrorAction SilentlyContinue