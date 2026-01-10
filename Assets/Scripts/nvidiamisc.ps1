# Credit: Peter
Get-WmiObject -Class Win32_VideoController | Where-Object { $_.PNPDeviceID -ne $null } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $providerName = (Get-ItemProperty -Path $classKey -Name "ProviderName" -ErrorAction SilentlyContinue).ProviderName
    if ($providerName -eq "NVIDIA") {
        # Disable Runtime Power Management
        New-ItemProperty -Path $classKey -Name "EnableRuntimePowerManagement" -Value 0 -Type DWord -Force

        # Disables HW fault buffers on Pascal+ chips
        New-ItemProperty -Path $classKey -Name "RmDisableHwFaultBuffer" -Value 1 -Type DWord -Force

        # Disable all Engine Level Clock Gating settings
        New-ItemProperty -Path $classKey -Name "RMElcg" -Value 1431655765 -Type DWord -Force

        # Disable all Engine Level Power Gating settings
        New-ItemProperty -Path $classKey -Name "RMElpg" -Value 4095 -Type DWord -Force

        # Disable all Block Level Clock Gating settings
        New-ItemProperty -Path $classKey -Name "RMBlcg" -Value 286331153 -Type DWord -Force

        # Disable all Second Level Clock Gating settings
        New-ItemProperty -Path $classKey -Name "RMSlcg" -Value 262131 -Type DWord -Force

        # Disable all floorsweep power gating settings
        New-ItemProperty -Path $classKey -Name "RMFspg" -Value 15 -Type DWord -Force

        # Disable GC6
        New-ItemProperty -Path $classKey -Name "RMGC6Feature" -Value 699050 -Type DWord -Force

        # Disable all latency optimizations for GC6
        New-ItemProperty -Path $classKey -Name "RMGC6Parameters" -Value 85 -Type DWord -Force

        # Disable all GC5 Idle Features
        New-ItemProperty -Path $classKey -Name "RMDidleFeatureGC5" -Value 44731050 -Type DWord -Force
 
        # Disable Hot Plug Support
        New-ItemProperty -Path $classKey -Name "RMHotPlugSupportDisable" -Value 1 -Type DWord -Force

        # Enable the Paged DMA mode for FBSR
        New-ItemProperty -Path $classKey -Name "RmFbsrPagedDMA" -Value 1 -Type DWord -Force

        # Disable Post L2 Compression
        New-ItemProperty -Path $classKey -Name "RMDisablePostL2Compression" -Value 1 -Type DWord -Force

        # Disable RC Watchdog
        New-ItemProperty -Path $classKey -Name "RmRcWatchdog" -Value 0 -Type DWord -Force

        # Disable Event Logging on RC errors
        New-ItemProperty -Path $classKey -Name "RmLogonRC" -Value 0 -Type DWord -Force

        # Disable more detailed debug INTR logs
        New-ItemProperty -Path $classKey -Name "RMIntrDetailedLogs" -Value 0 -Type DWord -Force

        # Disable FECS context switch logging
        New-ItemProperty -Path $classKey -Name "RMCtxswLog" -Value 0 -Type DWord -Force

        # Disable Logging
        New-ItemProperty -Path $classKey -Name "RMNvLog" -Value 0 -Type DWord -Force

        # Disable logging of NVLINK fatal errors
        New-ItemProperty -Path $classKey -Name "RmDisableInforomNvlink" -Value 3 -Type DWord -Force

        # Set Head0 DCLK Mode
        New-ItemProperty -Path $classKey -Name "Head0DClkMode" -Value 4294967295 -Type DWord -Force
        
        # Set Head1 DCLK Mode
        New-ItemProperty -Path $classKey -Name "Head1DClkMode" -Value 4294967295 -Type DWord -Force
        
        # Set Head2 DCLK Mode
        New-ItemProperty -Path $classKey -Name "Head2DClkMode" -Value 4294967295 -Type DWord -Force
        
        # Set Head3 DCLK Mode
        New-ItemProperty -Path $classKey -Name "Head3DClkMode" -Value 4294967295 -Type DWord -Force

        # Set PCLK Mode
        New-ItemProperty -Path $classKey -Name "PClkMode" -Value 4294967295 -Type DWord -Force

        # Disable feature disablement
        New-ItemProperty -Path $classKey -Name "RMDisableFeatureDisablement" -Value 0 -Type DWord -Force

        # Disable Break
        New-ItemProperty -Path $classKey -Name "RmBreak" -Value 0 -Type DWord -Force

        # Disable breakpoint on DEBUG resource manager on RC errors
        New-ItemProperty -Path $classKey -Name "RmBreakonRC" -Value 0 -Type DWord -Force

        # Disable SMC on a specific GPU
        New-ItemProperty -Path $classKey -Name "RMDebugSetSMCMode" -Value 0 -Type DWord -Force

        # Disable LRC coalescing
        New-ItemProperty -Path $classKey -Name "RMDisableLRCCoalescing" -Value 1 -Type DWord -Force

        # Disable I2C Nanny
        New-ItemProperty -Path $classKey -Name "RmEnableI2CNanny" -Value 0 -Type DWord -Force

        # Latency Tolerance
        New-ItemProperty -Path $classKey -Name "RMPcieLtrOverride" -Value 1 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMDeepL1EntryLatencyUsec" -Value 1 -Type DWord -Force

        # Configure Bandwidth Feature
        New-ItemProperty -Path $classKey -Name "RMBandwidthFeature" -Value 1896072192 -Type DWord -Force
        
        # Disable Mempool Compression
        New-ItemProperty -Path $classKey -Name "RMBandwidthFeature2" -Value 1 -Type DWord -Force

        # Disable Pre OS Apps
        New-ItemProperty -Path $classKey -Name "RmDisablePreosapps" -Value 1 -Type DWord -Force

        # RmPerfLimitsOverride
        New-ItemProperty -Path $classKey -Name "RmPerfLimitsOverride" -Value 21 -Type DWord -Force

        # RMGCOffFeature
        New-ItemProperty -Path $classKey -Name "RMGCOffFeature" -Value 2 -Type DWord -Force

        # Disable ASPM
        New-ItemProperty -Path $classKey -Name "RmOverrideSupportChipsetAspm" -Value 1 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMEnableASPMDT" -Value 1 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMDisableGpuASPMFlags" -Value 3 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMEnableASPMAtLoad" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMEnableASPMPublicBits" -Value 0 -Type DWord -Force

        # Disable Event Tracer
        New-ItemProperty -Path $classKey -Name "RMEnableEventTracer" -Value 0 -Type DWord -Force

        # Skip Error Checks
        New-ItemProperty -Path $classKey -Name "SkipSwStateErrChecks" -Value 1 -Type DWord -Force

        # Disable Advanced Error Reporting
        New-ItemProperty -Path $classKey -Name "RMAERRForceDisable" -Value 1 -Type DWord -Force

        # Enable OPSB Feature
        New-ItemProperty -Path $classKey -Name "RM580312" -Value 1 -Type DWord -Force

        # Enable WAR
        New-ItemProperty -Path $classKey -Name "RMBug2519005War" -Value 1 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmCeElcgWar1895530" -Value 1 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmWar1760398" -Value 1 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RM2644249" -Value 1 -Type DWord -Force

        # Configure Low Power Features
        New-ItemProperty -Path $classKey -Name "RMLpwrArch" -Value 349525 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMLpwrEiClient" -Value 5 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmLpwrCtrlMsDifrCgParameters" -Value 1365 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmLpwrFgRppg" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmLpwrGrPgSwFilterFunction" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmLpwrCtrlMsLtcParameters" -Value 5 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmLpwrCtrlMsDifrSwAsrParameters" -Value 5461 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmLpwrCacheStatsOnD3" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmLpwrCtrlGrRgParameters" -Value 89478485 -Type DWord -Force

        # Configure Paging Features
        New-ItemProperty -Path $classKey -Name "RmPgCtrlParameters" -Value 1431655765 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmPgCtrlGrParameters" -Value 1431655765 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmPgCtrlDiParameters" -Value 21 -Type DWord -Force

        # Keep MSCG enabled from RM side
        New-ItemProperty -Path $classKey -Name "RmDwbMscg" -Value 1 -Type DWord -Force

        # Dont Use PMU SPI
        New-ItemProperty -Path $classKey -Name "RMUsePmuSpi" -Value 0 -Type DWord -Force

        # Disable BBX Inform
        New-ItemProperty -Path $classKey -Name "RmDisableInforomBBX" -Value 15 -Type DWord -Force

        # Prefer System Memory Contiguous
        New-ItemProperty -Path $classKey -Name "PreferSystemMemoryContiguous" -Value 1 -Type DWord -Force
        New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\nvlddmkm" -Name "PreferSystemMemoryContiguous" -Value 1 -Type DWord -Force

        # Configure SEC2 to not use profile with APM task enabled
        New-ItemProperty -Path $classKey -Name "RmSec2EnableApm" -Value 0 -Type DWord -Force

        # Default GPU Operation mode
        New-ItemProperty -Path $classKey -Name "RMGpuOperationMode" -Value 0 -Type DWord -Force

        # Disables SilentRunning performance levels
        New-ItemProperty -Path $classKey -Name "MaxPerfWithPerfMon" -Value 0 -Type DWord -Force

        # Disable lowering MCLK
        New-ItemProperty -Path $classKey -Name "RmOptp2LowerMclk" -Value 0 -Type DWord -Force

        # Disable Slowdowns
        New-ItemProperty -Path $classKey -Name "RmOverrideIdleSlowdownSettings" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMClkSlowDown" -Value 71303168 -Type DWord -Force

        # Disable D3 related features
        New-ItemProperty -Path $classKey -Name "RMD3Feature" -Value 2 -Type DWord -Force

        # Disable 10 types of ACPI calls from the Resource Manager to the SBIOS
        New-ItemProperty -Path $classKey -Name "RmDisableACPI" -Value 1023 -Type DWord -Force

        # Disable Native PCIE L1
        New-ItemProperty -Path $classKey -Name "RMNativePcieL1WarFlags" -Value 16 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RM303107" -Value 16 -Type DWord -Force

        # Force Disable Clear perfmon and reset level when entering D4 state
        New-ItemProperty -Path $classKey -Name "RMResetPerfMonD4" -Value 0 -Type DWord -Force

        # Not Allow MCLK Switching
        New-ItemProperty -Path $classKey -Name "RM592311" -Value 2 -Type DWord -Force

        # Disable EDC replay
        New-ItemProperty -Path $classKey -Name "RMDisableEDC" -Value 1 -Type DWord -Force

        # Disable LPWR FSMs On Init
        New-ItemProperty -Path $classKey -Name "RMElpgStateOnInit" -Value 3 -Type DWord -Force

        # Disable Thermal Policy and Thermal Slowdown
        New-ItemProperty -Path $classKey -Name "RmThermPolicyOverride" -Value 1 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmThermPolicySwSlowdownOverride" -Value 1 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "ThermalPolicySW1" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RmThermalCacheDisable" -Value 1 -Type DWord -Force

        # Disable OptimusBoost ACPI
        New-ItemProperty -Path $classKey -Name "RmGpsACPIType" -Value 0 -Type DWord -Force

        # Force Power Steering OFF
        New-ItemProperty -Path $classKey -Name "RmGpsPowerSteeringEnable" -Value 0 -Type DWord -Force

        # Disable CPU utilization controller
        New-ItemProperty -Path $classKey -Name "RmGpsCpuUtilPoll" -Value 0 -Type DWord -Force

        # Force never power off the MIOs
        New-ItemProperty -Path $classKey -Name "RmMIONoPowerOff" -Value 1 -Type DWord -Force

        # Force Highest NVLink Link Power States
        New-ItemProperty -Path $classKey -Name "RMNvLinkControlLinkPM" -Value 170 -Type DWord -Force

        # Disable Noise Aware Pll
        New-ItemProperty -Path $classKey -Name "RmEnableNoiseAwarePll" -Value 0 -Type DWord -Force

        # Disable Optimal Power For Padlink Pll
        New-ItemProperty -Path $classKey -Name "RMDisableOptimalPowerForPadlinkPll" -Value 1 -Type DWord -Force

        # Disable CLKREQ and DEEP L1
        New-ItemProperty -Path $classKey -Name "RM2779240" -Value 5 -Type DWord -Force

        # Disable the power-off-dram-pll-when-unused feature
        New-ItemProperty -Path $classKey -Name "RmClkPowerOffDramPllWhenUnused" -Value 0 -Type DWord -Force

        # Disable OPSB (Optional Power Saving Bundle)
        New-ItemProperty -Path $classKey -Name "RMOPSB" -Value 10914 -Type DWord -Force

        # Disable Slides MCLK
        New-ItemProperty -Path $classKey -Name "SlideMCLK" -Value 0 -Type DWord -Force

        # Disable RTD3 D3Hot
        New-ItemProperty -Path $classKey -Name "RMForceRtd3D3Hot" -Value 2 -Type DWord -Force

        # Disable UPHY Init sequence
        New-ItemProperty -Path $classKey -Name "RMNvlinkUPHYInitControl" -Value 16 -Type DWord -Force

        # Disable Genoa System Power Controller
        New-ItemProperty -Path $classKey -Name "RmGpsGenoa" -Value 0 -Type DWord -Force

        # Disable Telemetry Collection
        New-ItemProperty -Path $classKey -Name "RMNvTelemetryCollection" -Value 0 -Type DWord -Force

        # Disable Aggressive Vblank
        New-ItemProperty -Path $classKey -Name "RmDisableAggressiveVblank" -Value 1 -Type DWord -Force

        # Disable Glitch Free MClk
        New-ItemProperty -Path $classKey -Name "GlitchFreeMClk" -Value 0 -Type DWord -Force

        # Disable Registry Caching
        New-ItemProperty -Path $classKey -Name "RmDisableRegistryCaching" -Value 15 -Type DWord -Force

        # Disable Hulk Features
        New-ItemProperty -Path $classKey -Name "RmHulkDisableFeatures" -Value 7 -Type DWord -Force

        # Enable D3 PC Latency
        New-ItemProperty -Path $classKey -Name "D3PCLatency" -Value 1 -Type DWord -Force

        # Disable MS Hybrid
        New-ItemProperty -Path $classKey -Name "EnableMsHybrid" -Value 0 -Type DWord -Force

        # Ignore Hulk Errors
        New-ItemProperty -Path $classKey -Name "RmIgnoreHulkErrors" -Value 1 -Type DWord -Force

        # Disable Illegal Compstat Access
        New-ItemProperty -Path $classKey -Name "RMDisableIntrIllegalCompstatAccess" -Value 1 -Type DWord -Force

        # Disable Fan Diagnostics
        New-ItemProperty -Path $classKey -Name "RmDisableFanDiag" -Value 1 -Type DWord -Force

        # Set Panel Refresh Rate
        New-ItemProperty -Path $classKey -Name "SetPanelRefreshRate" -Value 0 -Type DWord -Force

        # Disable RC on BAR Fault
        New-ItemProperty -Path $classKey -Name "RMDisableRcOnBarFault" -Value 1 -Type DWord -Force

        # Enable PowerMizer
        New-ItemProperty -Path $classKey -Name "PowerMizerEnable" -Value 1 -Type DWord -Force

        # Set PowerMizer Level to Max Performance
        New-ItemProperty -Path $classKey -Name "PowerMizerLevel" -Value 1 -Type DWord -Force

        # Set PowerMizer Level AC to Max Performance
        New-ItemProperty -Path $classKey -Name "PowerMizerLevelAC" -Value 1 -Type DWord -Force

        # Set PowerMizer Hard Level to Max Performance
        New-ItemProperty -Path $classKey -Name "PowerMizerHardLevel" -Value 1 -Type DWord -Force

        # Set PowerMizer Hard Level AC to Max Performance
        New-ItemProperty -Path $classKey -Name "PowerMizerHardLevelAC" -Value 1 -Type DWord -Force

        # Set PowerMizer Default to Max Performance
        New-ItemProperty -Path $classKey -Name "PowerMizerDefault" -Value 1 -Type DWord -Force

        # Set PowerMizer Default AC to Max Performance
        New-ItemProperty -Path $classKey -Name "PowerMizerDefaultAC" -Value 1 -Type DWord -Force

        # Disable Non-Contiguous Allocation
        New-ItemProperty -Path $classKey -Name "RMDisableNoncontigAlloc" -Value 1 -Type DWord -Force
    }
}

nvidia-smi.exe -acp 0