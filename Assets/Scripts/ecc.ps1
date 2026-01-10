# Credit: Peter
Get-WmiObject -Class Win32_VideoController | Where-Object { $_.PNPDeviceID -ne $null } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $providerName = (Get-ItemProperty -Path $classKey -Name "ProviderName" -ErrorAction SilentlyContinue).ProviderName
    if ($providerName -eq "NVIDIA") {
        # Ignore the ECC fuse
        New-ItemProperty -Path $classKey -Name "RMNoECCFuseCheck" -Value 1 -Type DWord -Force

        # Disable L1 ECC
        New-ItemProperty -Path $classKey -Name "RMEnableL1ECC" -Value 0 -Type DWord -Force

        # Disablee SM ECC
        New-ItemProperty -Path $classKey -Name "RMEnableSMECC" -Value 0 -Type DWord -Force

        # Disable SHM ECC
        New-ItemProperty -Path $classKey -Name "RMEnableSHMECC" -Value 0 -Type DWord -Force

        # Disable RM assert on ECC interrupts
        New-ItemProperty -Path $classKey -Name "RMAssertOnEccErrors" -Value 0 -Type DWord -Force

        # Disable ECC state in guest
        New-ItemProperty -Path $classKey -Name "RMGuestECCState" -Value 0 -Type DWord -Force
    }
}

# Disable ECC support
nvidia-smi.exe -e 0