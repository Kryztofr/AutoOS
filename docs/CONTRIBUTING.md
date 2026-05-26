# Contributing to AutoOS

Thank you for your interest in contributing to **AutoOS**! 🎉  
This guide will help you get started with setting up your development environment, following project conventions, and contributing code or features effectively.

---

## 📅 Project Board

Stay updated with the current progress, planned features, and active tasks:
- [🚀 AutoOS Project Board](https://github.com/users/tinodin/projects/2/views/1)

---

## ⚙️ Build and Compile the Source

> [!TIP]  
> Please confirm that your development environment meets the requirements before compiling.

### 1. 🖥️ Visual Studio 2026

Ensure that your installation includes the appropriate workloads:

- On the **Workloads** tab of the Visual Studio installer, check:
  - **.NET Desktop Development**
  - **Desktop development with C++** (Required for Native AOT and Win32 interop)
  - **WinUI Application Development**

### 2. 🛠️ SDKs

Ensure you have the following installed:
- .Net **10.x**
- Windows 11 SDK (10.0.26100.0)

### 3. Installed the **XAML Styler** extension (Optional for Building, Required for Contribute):
[XAML Styler for Visual Studio](https://marketplace.visualstudio.com/items?itemName=TeamXavalon.XAMLStyler2022)

### 4. 🔗 Debugging & Admin Privileges

AutoOS requires Administrator privileges for many of its operations. If the debugger is not attaching to the process, you are required to set `EnableLua` to `0` and restart your PC. This is a known WinUI 3 limitation:

```bat
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableLUA /t REG_DWORD /d 0 /f
```

Make sure your environment matches these requirements to avoid issues during compilation.

---

## 📦 Repository Structure

```bash
AutoOS/
  docs/ # Project documentation
  deploy/ # Deployment and installation scripts
  src/
    AutoOS.App/ # Main WinUI 3 Application
      Assets # Assets  
        Assets/Applications # Third Party Applications
        Assets/Fluent # Icons
        Assets/NavViewMenu # Navigation View
        Assets/Scripts # Installation Scripts
      Views/Installer # AutoOS Installer pages
        Views/Installer/Stages # AutoOS Installer Stages
      Views/Settings # AutoOS Settings pages
        Views/Settings/BIOS # Components of BIOS page
        Views/Settings/Games # Components of Games page
        Views/Settings/Power # Components of Energy & Power page
        Views/Settings/Scheduling # Components of Per-CPU Scheduling page
      Views/Startup # AutoOS Startup
      Viwes/Updater # AutoOS Updater
        Views/Updater/Stages # AutoOS Update Stages
    AutoOS.App.Sound/ # Separate C++ Application for applying Sound Buffer Sizes
    AutoOS.Core/ # Helper functions and Models
      NativeMethods.txt # Native Methods for Win32 interop
  Directory.Build.props # Central project configuration
  Directory.Packages.props # Central package configuration
  settings.xamlstyler # Configuration for XAML formatting
```

---

## 🖌 Code Style & Formatting

- **All XAML files** must be formatted using [XAML Styler](https://marketplace.visualstudio.com/items?itemName=TeamXavalon.XAMLStyler2022).
- The settings are defined in `Settings.XamlStyler` located in the root directory.
- Run **XAML Styler** on any new or updated XAML files before committing.

---

## 🧼 Code Guidelines

### ✂️ Partial Classes
All helper and component classes must be marked as `partial` to support **Native AOT** compilation and source generation.

```csharp
public partial class MyHelper
{
    // ...
}
```

or

```csharp
public static partial class MyHelper
{
    // ...
}
```

### 🚫 Avoid Reflection
Avoid using reflection-based logic, or any indirect type access. These break Native AOT compatibility..<br/>
Don't use `DllImport` or `LibraryImport`. Use `NativeMethods.txt` instead.

---

## 🤝 Ready to Contribute?

1. **Fork** the repository.
2. **Create a branch** for your feature or bugfix.
3. **Make your changes**, ensuring you follow the existing code style.
4. **Commit** with a descriptive message.
5. **Push** and create a **Pull Request**.

---

## Thank you for helping make AutoOS better! 🌟
Questions? Open an issue or contact @tinodin on the [AutoOS Discord Server](https://discord.gg/bZU4dMMWpg).
