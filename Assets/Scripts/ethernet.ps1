Get-NetAdapter | Where-Object { $_.PhysicalMediaType -eq "802.3" } | ForEach-Object {
    $adapterName = $_.Name
    $adapterProperties = Get-NetAdapterAdvancedProperty -Name $adapterName

    # Flow Control
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Flow Control" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Flow Control" -DisplayValue "Disabled"
    }

    # Idle power down restriction
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Idle power down restriction" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Idle power down restriction" -DisplayValue "Disabled"
    }

    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Interrupt Moderation Rate" }) {
        # Interrupt Moderation
        if ($adapterProperties | Where-Object { $_.DisplayName -eq "Interrupt Moderation" }) {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Interrupt Moderation" -DisplayValue "Enabled"
        }
        # Interrupt Moderation Rate
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Interrupt Moderation Rate" -DisplayValue "Medium"
    }
    else
    {
        # Interrupt Moderation
        if ($adapterProperties | Where-Object { $_.DisplayName -eq "Interrupt Moderation" }) {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Interrupt Moderation" -DisplayValue "Disabled"
        }
    }

    # IPv4 Checksum Offload
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "IPv4 Checksum Offload" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "IPv4 Checksum Offload" -DisplayValue "Rx & Tx Enabled"
    }

    # Jumbo Packet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Jumbo Packet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Jumbo Packet" -DisplayValue "Disabled"
    }

    # JumboPacket
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "JumboPacket" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "JumboPacket" -DisplayValue "Disabled"
    }

    # Large Send Offload Version 1
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Large Send Offload Version 1" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Large Send Offload Version 1" -DisplayValue "Enabled"
    }

    # Large Send Offload V2 (IPv4)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Large Send Offload V2 (IPv4)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Large Send Offload V2 (IPv4)" -DisplayValue "Enabled"
    }

    # Large Send Offload V2 (IPv6)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Large Send Offload V2 (IPv6)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Large Send Offload V2 (IPv6)" -DisplayValue "Enabled"
    }

    # Wake from S0ix on Magic Packet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake from S0ix on Magic Packet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake from S0ix on Magic Packet" -DisplayValue "Disabled"
    }

    # Wake On Magic Packet From S5
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake On Magic Packet From S5" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake On Magic Packet From S5" -DisplayValue "Disabled"
    }

    # Wake on magic packet when system is in the S0ix power state
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on magic packet when system is in the S0ix power state" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on magic packet when system is in the S0ix power state" -DisplayValue "Disabled"
    }

    # Maximum Number of RSS Queues
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Maximum Number of RSS Queues" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Maximum Number of RSS Queues" -DisplayValue "4 Queues"
    }

    # ARP Offload
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "ARP Offload" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "ARP Offload" -DisplayValue "Enabled"
    }

    # NS Offload
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "NS Offload" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "NS Offload" -DisplayValue "Enabled"
    }

    # Packet Priority & VLAN
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Packet Priority & VLAN" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Packet Priority & VLAN" -DisplayValue "Packet Priority & VLAN Enabled"
    }

    # Receive Segment Coalescing (IPv4)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Recv Segment Coalescing (IPv4)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Recv Segment Coalescing (IPv4)" -DisplayValue "Enabled"
    }

    # Receive Segment Coalescing (IPv6)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Recv Segment Coalescing (IPv6)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Recv Segment Coalescing (IPv6)" -DisplayValue "Enabled"
    }

    # Receive Buffers
    # if ($adapterProperties | Where-Object { $_.DisplayName -eq "Receive Buffers" }) {
    #     Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Receive Buffers" -DisplayValue "512"
    #     Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Receive Buffers" -DisplayValue "1024"
    #     Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Receive Buffers" -DisplayValue "2048"
    #     Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Receive Buffers" -DisplayValue "4096"
    # }

    # Receive Side Scaling
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Receive Side Scaling" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Receive Side Scaling" -DisplayValue "Disabled"
    }

    # Selective Suspend
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Selective Suspend" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Selective Suspend" -DisplayValue "Disabled"
    }

    # SelectiveSuspend
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "SelectiveSuspend" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "SelectiveSuspend" -DisplayValue "Disabled"
    }

    # Speed & Duplex
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Speed & Duplex" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Speed & Duplex" -DisplayValue "Auto Negotiation"
    }

    # Selective Suspend Idle Timeout
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Selective Suspend Idle Timeout" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Selective Suspend Idle Timeout" -DisplayValue "60"
    }

    # TCP Checksum Offload (IPv4)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "TCP Checksum Offload (IPv4)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "TCP Checksum Offload (IPv4)" -DisplayValue "Rx & Tx Enabled"
    }

    # TCP Checksum Offload (IPv6)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "TCP Checksum Offload (IPv6)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "TCP Checksum Offload (IPv6)" -DisplayValue "Rx & Tx Enabled"
    }

    # Transmit Buffers
    # if ($adapterProperties | Where-Object { $_.DisplayName -eq "Transmit Buffers" }) {
    #     Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Transmit Buffers" -DisplayValue "512"
    #     Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Transmit Buffers" -DisplayValue "1024"
    #     Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Transmit Buffers" -DisplayValue "2048"
    #     Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Transmit Buffers" -DisplayValue "4096"
    # }

    # UDP Checksum Offload (IPv4)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "UDP Checksum Offload (IPv4)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "UDP Checksum Offload (IPv4)" -DisplayValue "Rx & Tx Enabled"
    }

    # UDP Checksum Offload (IPv6)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "UDP Checksum Offload (IPv6)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "UDP Checksum Offload (IPv6)" -DisplayValue "Rx & Tx Enabled"
    }

    # TCP/UDP Checksum Offload (IPv4)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "TCP/UDP Checksum Offload (IPv4)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "TCP/UDP Checksum Offload (IPv4)" -DisplayValue "Rx & Tx Enabled"
    }

    # TCP/UDP Checksum Offload (IPv6)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "TCP/UDP Checksum Offload (IPv6)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "TCP/UDP Checksum Offload (IPv6)" -DisplayValue "Rx & Tx Enabled"
    }

    # Wake on Magic Packet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Magic Packet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Magic Packet" -DisplayValue "Disabled"
    }

    # Wake on Pattern Match
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Pattern Match" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Pattern Match" -DisplayValue "Disabled"
    }

    # Wake on pattern match
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on pattern match" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on pattern match" -DisplayValue "Disabled"
    }

    # DMA Coalescing
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "DMA Coalescing" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "DMA Coalescing" -DisplayValue "Disabled"
    }

    # Energy Efficient Ethernet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Energy Efficient Ethernet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Energy Efficient Ethernet" -DisplayValue "Disabled"
    }

    # Energy Efficient Ethernet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Energy Efficient Ethernet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Energy Efficient Ethernet" -DisplayValue "Off"
    }

    # Energy-Efficient Ethernet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Energy-Efficient Ethernet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Energy-Efficient Ethernet" -DisplayValue "Disabled"
    }

    # Enable PME
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Enable PME" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Enable PME" -DisplayValue "Disabled"
    }

    # Advanced EEE
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Advanced EEE" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Advanced EEE" -DisplayValue "Disabled"
    }

    # Auto Disable Gigabit
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Auto Disable Gigabit" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Auto Disable Gigabit" -DisplayValue "Disabled"
    }

    # Green Ethernet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Green Ethernet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Green Ethernet" -DisplayValue "Disabled"
    }

    # Gigabit Lite
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Gigabit Lite" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Gigabit Lite" -DisplayValue "Disabled"
    }

    # Power Saving Mode
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Power Saving Mode" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Power Saving Mode" -DisplayValue "Disabled"
    }

    # Enable PME
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Enable PME" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Enable PME" -DisplayValue "Disabled"
    }

    # Log Link State Event
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Log Link State Event" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Log Link State Event" -DisplayValue "Disabled"
    }

    # Gigabit Master Slave Mode
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Gigabit Master Slave Mode" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Gigabit Master Slave Mode" -DisplayValue "Auto Detect"
    }

    # Locally Administered Address
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Locally Administered Address" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Locally Administered Address" -DisplayValue "--"
    }

    # Wait for Link
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wait for Link" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wait for Link" -DisplayValue "Off"
    }

    # Wake on Link
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Link" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Link" -DisplayValue "Disabled"
    }

    # Wake on Link Settings
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Link Settings" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Link Settings" -DisplayValue "Disabled"
    }

    # Wake On Link Up
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake On Link Up" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake On Link Up" -DisplayValue "Disabled"
    }

    # Wake on Ping
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Ping" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Ping" -DisplayValue "Disabled"
    }

    # Shutdown Wake-On-Lan
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Shutdown Wake-On-Lan" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Shutdown Wake-On-Lan" -DisplayValue "Disabled"
    }

    # WOL & Shutdown Link Speed
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "WOL & Shutdown Link Speed" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "WOL & Shutdown Link Speed" -DisplayValue "Not Speed Down"
    }

    # Reduce Speed On Power Down
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Reduce Speed On Power Down" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Reduce Speed On Power Down" -DisplayValue "Disabled"
    }

    # Multi-Channel Concurrent
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Multi-Channel Concurrent" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Multi-Channel Concurrent" -DisplayValue "Disabled"
    }

    # Preamble Mode
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Preamble Mode" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Preamble Mode" -DisplayValue "Short"
    }

    # Downshift retries
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Downshift retries" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Downshift retries" -DisplayValue "0"
    }

    # Wake from power off state
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake from power off state" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake from power off state" -DisplayValue "Disabled"
    }

    # WOL Link Power Saving
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "WOL Link Power Saving" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "WOL Link Power Saving" -DisplayValue "Disabled"
    }
    
    # Ultra Low Power Mode
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Ultra Low Power Mode" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Ultra Low Power Mode" -DisplayValue "Disabled"
    }

    # PCI Express Link Power Saving
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "PCI Express Link Power Saving" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "PCI Express Link Power Saving" -DisplayValue "Disabled"
    }
}