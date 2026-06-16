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
- If your output supports a lower `buffer size` in the `Sound` tab, you may lower it in exchange for higher CPU usage.
- Leave a `review`, share `suggestions`, or report `issues` on the `Discord Server`.
- [Donate](https://www.paypal.com/donate/?hosted_button_id=GVEVUSHUWXEAG) if you appreciate the immense time and effort I have put into creating and providing this project for free.
- If you have experience with `C# and WinUI3` and want to become a part of the project, let me know.

### What **NOT** to do after the installation is finished?
- Run other `tweaks` or `optimizers` like `CTT` etc. for obvious reasons.
- Apply `timer resolution` because it does more harm than good.
- Use `external frame rate limiters` like `NVCP` or `RTSS` because they trade `better 1% lows` for `added latency` (unless non competitive).
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