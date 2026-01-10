Get-WmiObject -Class Win32_VideoController | Where-Object { $_.PNPDeviceID -ne $null } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $providerName = (Get-ItemProperty -Path $classKey -Name "ProviderName" -ErrorAction SilentlyContinue).ProviderName
    if ($providerName -eq "NVIDIA") {
        # Disable dynamic P-State/adaptive clocking
        New-ItemProperty -Path $classKey -Name "DisableDynamicPstate" -Value 1 -Type DWord -Force

        # Disable asynchronous p-state changes
        New-ItemProperty -Path $classKey -Name "DisableAsyncPstates" -Value 1 -Type DWord -Force
    }
}
