### What to do after the installation is finished?
**Sound Tab:**
- If your audio output device supports a lower `buffer size`, you can lower it in exchange for `higher CPU usage`.

**Services & Drivers Tab:**
- `Enable` the `WiFi Support` checkbox if you are using `WiFi while Gaming`.
- `Enable` the `Bluetooth Support` checkbox if you are using `Bluetooth while Gaming`.
- `Disable` the `toggle` at the top and restart whenever you are `Gaming` competitively.
- `Enable` it again and restart if you need `functionality` for `Work` or installing applications / drivers.

**BIOS Settings Tab:**
- In the `Recommended Changes` field click `Merge` and `Import to NVRAM` on the top right, then restart your PC.

  **If no internet:**
  - Enable the `toggle` in the `Services & Drivers` tab and restart your PC.
  - Click `Optimize` in the `Per-CPU Scheduling` and in the `Network & Internet` tab.

  **If not booting:**
  - Reset CMOS using the `button` on your `motherboard` (if yours has one) or by `removing the CMOS battery` for 5 minutes.
  - After that, `lower the amount` to merge using the `numberbox` on the left until you `find the setting` that causes your PC to not boot.
  - Once you find the setting, please leave a message on the [Discord Server](https://discord.gg/bZU4dMMWpg)

  **If crashing, freezing or worse performance:**
  - Option 1 (Easier): Click `Restore from Backup` and select the oldest or previous `nvram.txt`.
  - Option 2 (Harder):
    - **Intel:**
      - Lower `Max Turbo Ratios`
      - Enable `Hyper-Threading`
      - Disable `E-Cores` (Active E-Cores, Active Efficient Cores, Active Efficient-cores, No. of CPU E-Cores Enabled)
    - **AMD:**
      - Lower `All Core Curve Optimizer Magnitude`

**Games Tab:**
- Press the `Play` button to launch any Game.

  **If Services & Drivers toggle disabled:**
  - Once you are in the `Game`, press the `Stop Processes` button.
  - Use `Alt+Tab` to switch between apps in this state.
  - Press the `Restart Processes` button to restore the taskbar etc.
  
  **Adding Games:**
  - For `Riot Games` titles to show up in the `Games` tab, install them through the `Epic Games Launcher` as well.
  - For `EA` or `Ubisoft Connect` titles to show up in the `Games` tab, add them to your `Epic Games Launcher` library.
  - To add custom games, add the game as a `non-steam game` in `Steam`.
  - The name has to be the same as found on [IGDB](https://www.igdb.com/).

  **Notes:**
  - Cap your Game's `frame rate limit` to `a multiple` of your monitor's `refresh rate` (eg. 144hz -> 72/144/288fps).

**Other:**
- Leave a `review`, share `suggestions`, or report `issues` on the `Discord Server`.
- [Donate](https://www.paypal.com/donate/?hosted_button_id=GVEVUSHUWXEAG) if you appreciate the immense time and effort I have put into creating and providing this project for free.
- If you want to become a part of the project, let me know.

### What **NOT** to do after the installation is finished?
- Run other `tweaks` or `optimizers` like `CTT` etc. for obvious reasons.
- Apply `timer resolution` because it does more harm than good.
- Use `external frame rate limiters` like `NVCP` or `RTSS` because they trade `better 1% lows` for `added latency` (unless non competitive).
- Set `visual effects` to `Best Performance`, `disable animations / transparency / paging file`.
- `Uninstall` `MSI Afterburner, OBS, Everything, Windhawk, StartAllBack` or any of the `runtimes`.
- `Install` `7-Zip` or `WinRAR` because `NanaZip` is already installed.
- `Uninstall` more AppX Packages like `Xbox Game Bar` or `Microsoft Edge` because it **breaks functionality**.

### Merging the old Windows partition
To delete your old Windows partition and merge the unallocated space with the AutoOS partition: 

- Move your Games to the AutoOS partition and replace the drive letters in the Game Launchers config files:
  - Epic Games 
    - `C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat`
    - `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests`
    - `C:\ProgramData\Epic\EpicOnlineServicesShared\InstallHelper\InstalledItems`
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