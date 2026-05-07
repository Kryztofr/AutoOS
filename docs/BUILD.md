# ⚙️ Build instructions

### 1. 🖥️ Visual Studio 2026 Insiders

Ensure that your installation includes the appropriate workloads:

- On the **Workloads** tab of the Visual Studio installer, check:
  - **.NET Desktop Development**
  - **.Desktop development with C++**
  - **WinUI Application Development**

### 2. 🔗 Clone the repository

If the debugger is not attaching to the process, you are required to set EnableLua to 0 and restart your PC. This has been a problem for 5 years and Microsoft hasn't provided a fix:
```bat
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableLUA /t REG_DWORD /d 0 /f
```
