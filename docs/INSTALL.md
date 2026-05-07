# 🚀 Installation Guide

**Step 1:** Before installing, please join my [Discord Server](https://discord.gg/bZU4dMMWpg) to receive installation support and stay informed about future updates or changes.

**Step 2:** Download the latest Windows 25H2 ISO file from [here](https://drive.google.com/drive/folders/1BlAYofjlW1bU-WPG3jXygO1ezoJ4gPs7?usp=sharing) (Log into your Google Account if you get an error). Other ISOs are not supported to guarantee consistency and the latest features. 

**Step 3:** Open Device Manager (`devmgmt.msc`) and look for the brand of your Ethernet and Wi-Fi Adapter. If you have one of the below, download the corresponding drivers. For the rest of your drivers (Realtek Wi-Fi / Bluetooth, Mediatek Wi-Fi / Bluetooth, Audio and Storage Controller), download them from the Drivers / Support page of your Mainboard / PC manufacturer's website.

**INTEL:** [Ethernet](https://www.intel.com/content/www/us/en/download/727998/intel-network-adapter-driver-for-microsoft-windows-11.html) · [Wi-Fi](https://www.dl.dropboxusercontent.com/scl/fi/9qjxlr4x59dv9ncusmu3h/INTEL-WiFi.zip?rlkey=v1mzzc37onjmcpundt48u8i83&st=pnj3c3ax&dl=0) · [Bluetooth](https://www.dl.dropboxusercontent.com/scl/fi/qoylgflunti1fhzpcjnip/INTEL-Bluetooth.zip?rlkey=j23dopqk2ek1r5ju00zemwsf2&st=wopu40cj&dl=0)

**Realtek:** [Ethernet](https://www.dl.dropboxusercontent.com/scl/fi/gr47u24zve7ll7lmel9ke/Install_Win11_Win10_10079_20_DMAROFF_01262026.zip?rlkey=pp7modxp8ht1zxcwlu5foam8l&st=vsxyeok0&dl=0)

Extract all `.zip` files (for `.exe` files from HP, etc, run them and select `Extract` instead of `Install`, otherwise use `7-Zip, NanaZip, or WinRAR` to extract them) and move all extracted folders `into one new folder`. The folder should contain each driver and their `.inf` files.

On Prebuilts and Laptops you may need to disable `VMD Controller` in your BIOS, otherwise you may get `Inaccessible boot device` BSOD. It might be called differently in your BIOS. For DELL/Alienware go to `Storage -> SATA/NVMe Operation` and change it from `Disabled` to `AHCI/NVMe`.

> [!NOTE]  
> If you can't access your old Windows after disabling VMD Controller, boot into safe mode once, then restart.

**Step 4:** Open PowerShell **as Administrator**.

**Step 5:** Paste this into the PowerShell window to run the deployment script. Then select the **ISO** and your **drivers folder** or skip the driver installation if you are sure that you don't need them.

```ps1
$PSDefaultParameterValues['Invoke-WebRequest:UseBasicParsing'] = $true
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force 
irm https://raw.githubusercontent.com/tinodin/AutoOS/master/deploy/deploy.ps1 | iex
```

If you get any errors during the script leave a message in my discord server.

**Step 6:** Once the script finished, `restart` and boot into the `default option`. Make sure to `keep your ethernet cable connected` or `connect to your WiFi in the setup`. **DO NOT BYPASS THE NETWORK REQUIREMENT!** Then wait for Windows to finish installing.

**Step 7:** Once finished, wait for AutoOS to open up.

**Step 8:** Carefully look through every tab and select your preferences and apps. Please select the Discord app and don't use Discord in the Browser. Then click "Install AutoOS". This process will take around 15-30 minutes.

> [!NOTE]  
> You may experience a blank screen in the App after installing the Graphics Driver. To fix this, resize the window, click the navigation pane button a few times or just wait until it rerenders the UI.

### What to do after the installation is finished?
- For `Riot Games` titles to show up in the `Games` tab, install them through the `Epic Games Launcher` as well.
- For `EA` or `Ubisoft Connect` titles to show up in the `Games` tab, add them to your `Epic Games Launcher` library.
- `Disable` the toggle in `Services & Drivers` tab and restart whenever you are `Gaming`.
- `Enable` it again and restart if you need functionality back for `Work` or installing applications / drivers.
- Go to the `Games` tab while `Services & Drivers` are disabled and press the `Play` button.
- Once you are in the `Game`, press the `Stop Processes` button. 
- Press the `Restart Processes` button to restore the taskbar etc.
- Cap your Game's `frame rate limit` to `a multiple` of your monitor's `refresh rate` (144hz, 72/144/288fps).
- Use `NVIDIA Reflex Low Latency` set to `Off` in competetive games unless you are GPU bound with a bad GPU.
- Check the `BIOS Settings` tab for recommendations, click `Merge All` then `Import to NVRAM`.
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

- Install [Minitool Partition Wizard Free](https://cdn2.minitool.com/?p=pw&e=pw-free) (decline each offer in the installer). 
- Use the `Delete` function on the old Windows partition
- Use the `Extend` function on the AutoOS partition, select the old Windows partition and max out the slider. 
- Click `Apply` and then `Restart Now`. After its done, delete `Minitool Partition Wizard Free`.

If you are on ASUS Motherboard and get `GPT header corruption has been detected` message:
- Press `F1` to get into `BIOS`.
- Press `F7` to get into `advanced mode`.
- Go to `Boot` tab, then select `Boot Configuration`.
- Change `Next Boot Recovery Action` to `Recovery`.
- Change `Boot Sector (MBR/GPT) Recovery Policy` to `Auto Recovery` if it exists.
