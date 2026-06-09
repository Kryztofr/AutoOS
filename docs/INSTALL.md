# 🚀 Installation Guide

> [!WARNING]  
> This guide does **NOT** require a USB drive.<br/> 
> Installing AutoOS using a USB drive is technically possible using Ventoy with the unattend.xml file but you will need a ventoy config file.<br/> 
> When installing via USB drive, you will lose out on stripping 8.3 file names and importing games, etc. from your old installation.

### Step 1: Join Discord Server
Join my [Discord Server](https://discord.gg/bZU4dMMWpg) to receive installation support and stay informed about future updates or changes.

### Step 2: Downloading the ISO
Download the latest Windows 25H2 ISO file from [here](https://drive.google.com/drive/folders/1BlAYofjlW1bU-WPG3jXygO1ezoJ4gPs7?usp=sharing) (Log into your Google Account if you get an error).<br/>
Other ISOs are not supported (will not work) to guarantee consistency and the latest features. 

### Step 3: Downloading Drivers
Open Device Manager (`devmgmt.msc`) and look for the brand of your Ethernet and Wi-Fi Adapter under the `Network Adapters` section.<br/>
If you have one of following below, download them.<br/>

**INTEL:** [Ethernet](https://www.intel.com/content/www/us/en/download/727998/intel-network-adapter-driver-for-microsoft-windows-11.html) · [Wi-Fi](https://www.dl.dropboxusercontent.com/scl/fi/9qjxlr4x59dv9ncusmu3h/INTEL-WiFi.zip?rlkey=v1mzzc37onjmcpundt48u8i83&st=pnj3c3ax&dl=0) · [Bluetooth](https://www.dl.dropboxusercontent.com/scl/fi/qoylgflunti1fhzpcjnip/INTEL-Bluetooth.zip?rlkey=j23dopqk2ek1r5ju00zemwsf2&st=wopu40cj&dl=0)

**Realtek:** [Ethernet](https://www.dl.dropboxusercontent.com/scl/fi/gr47u24zve7ll7lmel9ke/Install_Win11_Win10_10079_20_DMAROFF_01262026.zip?rlkey=pp7modxp8ht1zxcwlu5foam8l&st=vsxyeok0&dl=0)

Open System Information (`msinfo32`) and look for `System Model`, in your browser search for "{Your system model} drivers".<br/>
Click on the support page, head to drivers and select Windows 11, then download the rest of the drivers you may need (Realtek Wi-Fi / Bluetooth, Mediatek Wi-Fi / Bluetooth, Audio, Storage Controller, Touchpad). Do not download the graphics driver or any utilities.

If the page contains `Intel Rapid Storage Technology Driver` / `Intel RAID Driver`, either download it or disable `VMD Controller` in your BIOS.
If you keep it enabled and don't download the driver, you will get `Inaccessible boot device` BSOD.<br/>
On DELL/Alienware go to `Storage -> SATA/NVMe Operation` and change it from `Disabled` to `AHCI/NVMe`.

> [!NOTE]  
> Disabling it in BIOS is will result in better disk speeds over using the Intel RST driver.<br/>
> If you can't access your old Windows after disabling VMD Controller, boot into safe mode once, then restart.

After downloading all your drivers, extract all `.zip` files.<br/>
For `.exe` run them and select the `Extract` instead of `Install` if it exists.<br/>
If they don't have that option, use `7-Zip, NanaZip, or WinRAR` to extract them.

Finally, create a `New Folder` and move all extracted folders into it. The folder should contain each driver and their `.inf` files.

### Step 4: Run the deployment script
Open PowerShell **as Administrator**.<br/>
Paste this into the PowerShell window to run the deployment script.<br/>

```ps1
$PSDefaultParameterValues['Invoke-WebRequest:UseBasicParsing'] = $true
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force 
irm https://raw.githubusercontent.com/tinodin/AutoOS/master/deploy/deploy.ps1 | iex
```
Then select the **ISO** file you downloaded in Step 2 and your **drivers folder** you created in Step 3.<br/>
If you get any errors during the script leave a message in my [Discord Server](https://discord.gg/bZU4dMMWpg).

### Step 5: Restarting into AutoOS
Once the script finished, restart your PC and boot into the `default option` by pressing `Enter`.<br/>

> [!WARNING]  
> Make sure to `keep your ethernet cable connected` or `connect to your WiFi in the setup`.<br/>
> **DO NOT BYPASS THE NETWORK REQUIREMENT!**

### Step 6: AutoOS Installer
Once the OOBE has finished, wait for AutoOS Installer to open up.<br/>
Carefully look through every tab and select your preferences and apps.<br/>

> [!TIP]  
> Select the Discord app and do not use Discord in the Browser. It uses the same or less RAM and less CPU Usage.<br/>

Then click "Install AutoOS". This process will take around 15-30 minutes.

> [!NOTE]  
> You may experience a blank screen in the App after installing the Graphics Driver. To fix this, resize the window, click the navigation pane button a few times or just wait until it rerenders the UI.

### What to do after the installation is finished?
- For `Riot Games` titles to show up in the `Games` tab, install them through the `Epic Games Launcher` as well.
- For `EA` or `Ubisoft Connect` titles to show up in the `Games` tab, add them to your `Epic Games Launcher` library.
- To add custom games, add your game as a non-steam game in Steam (use the name that is on IGDB).
- `Disable` the toggle in `Services & Drivers` tab and restart whenever you are `Gaming` competitively.
- `Enable` it again and restart if you need functionality back for `Work` or installing applications / drivers.
- Go to the `Games` tab while `Services & Drivers` are disabled and press the `Play` button.
- Once you are in the `Game`, press the `Stop Processes` button. 
- Press the `Restart Processes` button to restore the taskbar etc.
- Cap your Game's `frame rate limit` to `a multiple` of your monitor's `refresh rate` (144hz, 72/144/288fps).
- Check the `BIOS Settings` tab for recommendations, click `Merge All` then `Import to NVRAM`.
- If you don't boot after merging all, reset CMOS and use `Merge Next` until you find the culprit.
- If you face worse performance, instability or crashes, use `Restore from Backup`.
- If you are `unstable` on `Intel`, lower your `Max Turbo Ratios`, disable `E-Cores` and enable `Hyper-Threading`.
- If your output supports a lower buffer size in the `Sound` tab, you may lower it in exchange for higher CPU usage.
- Leave a `review`, share `suggestions`, or report `issues` on the `Discord Server`.
- [Donate](https://www.paypal.com/donate/?hosted_button_id=GVEVUSHUWXEAG) if you appreciate the immense time and effort I have put into creating and providing this project for free.
- If you have experience with `C# and WinUI3` and want to become a part of the project, let me know.

### What **NOT** to do after the installation is finished?
- Run other `tweaks` or `optimizers` like `CTT` etc. for obvious reasons.
- Apply `timer resolution` because it does more harm than good.
- Use `external frame rate limiters` like `NVCP` or `RTSS` because they trade `better 1% lows` for `added latency`.
- Set `visual effects` to `Best Performance`, `disable animations / transparency / paging file`.
- `Uninstall` `MSI Afterburner, OBS, Everything, Windhawk, StartAllBack` or any of the `runtimes`.
- `Install` `7-Zip`, because `NanaZip` is already installed.
- `Uninstall` more AppX Packages like `Xbox Game Bar` or `Microsoft Edge` because it **breaks functionality**.

### Merging the old Windows partition
To delete your old Windows partition and merge the unallocated space with the AutoOS partition: 

- Move your Games to the AutoOS partition and replace the drive letters in the Game Launchers config files:
  - Epic Games 
    - `C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat"`
    - `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests"`
  - Steam 
    - `C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf`
- Open Command Prompt and paste:
```
bcdedit /enum
``` 

- Find the entry of your old Windows partition, copy its `identifier` value and then run:

```
bcdedit /delete {identifier}
```

- Install [Minitool Partition Wizard Free](https://cdn2.minitool.com/?p=pw&e=pw-free-offline). 
- Use the `Delete` function on the old Windows partition
- Use the `Extend` function on the AutoOS partition, select the old Windows partition and max out the slider. 
- Click `Apply` and then `Restart Now`. After its done, delete `Minitool Partition Wizard Free`.

If you are on ASUS Motherboard and get `GPT header corruption has been detected` message:
- Press `F1` to get into `BIOS`.
- Press `F7` to get into `advanced mode`.
- Go to `Boot` tab, then select `Boot Configuration`.
- Change `Next Boot Recovery Action` to `Recovery`.
- Change `Boot Sector (MBR/GPT) Recovery Policy` to `Auto Recovery` if it exists.
