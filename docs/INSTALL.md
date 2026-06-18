# 🚀 Installation Guide

> [!NOTE]  
> This guide does **NOT** require a USB drive.<br/> 
> Installing AutoOS using a USB drive is technically possible using Ventoy with the unattend.xml file but you will need a ventoy config file.<br/> 
> When installing via USB drive, you will lose out on stripping 8.3 file names and importing games, etc. from your old installation.

### Step 1: Join Discord Server
Join my [Discord Server](https://discord.gg/bZU4dMMWpg) to receive **installation support** and stay informed about **future updates or changes**.

### Step 2: Downloading the ISO
Download the latest Windows `25H2.iso` file from [here](https://drive.google.com/drive/folders/1BlAYofjlW1bU-WPG3jXygO1ezoJ4gPs7?usp=sharing) (Log into your Google Account if you get an error).<br/>
Other ISOs are not supported (will not work) to guarantee consistency and the latest features. 

### Step 3: Downloading Drivers
Open Device Manager (`devmgmt.msc`) and look for the brand of your Ethernet and Wi-Fi Adapter under the `Network adapters` section.<br/>
If you have one of following below, download them.<br/>

**INTEL:** [Ethernet](https://www.intel.com/content/www/us/en/download/727998/intel-network-adapter-driver-for-microsoft-windows-11.html) · [Wi-Fi](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/WiFi.zip) · [Bluetooth](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/Bluetooth.zip)

**Realtek:** [Ethernet](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Realtek/Install_PCIE_Win11_11029_20_DMAROFF_04212026.zip) · [Wi-Fi and Bluetooth](https://www.realtek.com/Download/Index?cate_id=203&menu_id=297)

**MediaTek:** [Wi-Fi](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/MediaTek/WiFi.zip) · [Bluetooth](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/MediaTek/Bluetooth.zip)

---

<details>
<summary>If you have a <b>Laptop</b>, click to expand</summary>
<br/>

Open System Information (`msinfo32`) and look for `System Model`, in your browser search for "{Your system model here} drivers".<br/>
Click on the support page, head to drivers and select Windows 11 (if available), then download the **Audio and Touchpad** drivers (if available).
<br/>
</details>

---

<details>
<summary>If you have an Intel <b>10th Generation and up Prebuilt PC</b> or <b>Laptop</b>, click to expand</summary>
<br/>

Open Device Manager (`devmgmt.msc`) and look for anything containing "**Intel**" under the `Storage controllers` section.<br/>
If there are none, continue with Step 4.<br/>

If there is an "**Intel**" entry, you have **2 options**:

**Option 1 (Preferred):**
- Disable `VMD Controller` in your BIOS. 
- On DELL/Alienware go to `Storage -> SATA/NVMe Operation` and change it from `Disabled` to `AHCI/NVMe`.

**Option 2:**
- Download the Intel® Rapid Storage Technology Driver for your CPU Generation from below:
  - [10th and 11th](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/RST/10th%20and%2011th/RST.zip)
  - [12th to 15th](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/RST/12th%20to%2015th/RST.zip)
  - [Intel® Core™ Ultra Series 3](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/RST/Intel%C2%AE%20Core%E2%84%A2%20Ultra%20Series%203/RST.zip)

**Notes:**
- **Option 1** will result in **better disk speeds**.
- If you get a **BSOD** on your **old Windows** after **disabling VMD Controller**, boot into **safe mode** once, then restart.
- If you keep `VMD Controller` **enabled** and **didn't download the RST driver**, you will get `Inaccessible boot device` **BSOD**.
</details>

---

After downloading your drivers, extract all `.zip` files.<br/>
For `.exe` files, run them and select the `Extract` (if available).<br/>
If they don't have that option, use `7-Zip, NanaZip, or WinRAR` to extract them.

Finally, create a `New Folder` and move all extracted folders into it.

### Step 4: Run the deployment script
Open PowerShell **as Administrator**.<br/>
Paste this into the PowerShell window to run the deployment script.<br/>

```ps1
$PSDefaultParameterValues['Invoke-WebRequest:UseBasicParsing'] = $true
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force 
irm https://raw.githubusercontent.com/tinodin/AutoOS/master/deploy/deploy.ps1 | iex
```
Then select the **ISO** file you downloaded in Step 2 and your **drivers folder** you created in Step 3.<br/>
If you get any errors during the script, please leave a message on my [Discord Server](https://discord.gg/bZU4dMMWpg).

### Step 5: Restarting into AutoOS
Once the script finished, restart your PC and boot into `AutoOS` by pressing `Enter` after every following restart.<br/>

> [!WARNING]  
> Make sure to `keep your ethernet cable connected` or `connect to your WiFi in the setup`.<br/>
> **DO NOT BYPASS THE NETWORK REQUIREMENT!**

### Step 6: AutoOS Installer
Once the OOBE has finished, wait for Windows to restart and AutoOS Installer to open up.<br/>
Carefully look through every tab and select your preferences and apps.<br/>
You can take inspiration from my selection in **[Screenshots](SCREENSHOTS.md)**.<br/>
Then click "Install AutoOS". This process will take around 15-45 minutes depending on your internet speed.

> [!NOTE]  
> You may experience a blank screen in the App after installing the Graphics Driver.<br/>
> To fix this, resize the window, click the navigation pane button a few times or just wait until it rerenders the UI.

After the installation is done, make sure to read the **[Post Install Guide](POSTINSTALL.md)**.