param(
    [Parameter(Mandatory)]
    [ValidateSet("UWA","Link")]
    [string]$Type,

    [Parameter(Mandatory)]
    [string]$Path
)

$xmlPath = "C:\Windows\Setup\Scripts\TaskbarLayoutModification.xml"

[xml]$xml = Get-Content $xmlPath

$nsMgr = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$nsMgr.AddNamespace("taskbar", "http://schemas.microsoft.com/Start/2014/TaskbarLayout")
$pinList = $xml.SelectSingleNode("//taskbar:TaskbarPinList", $nsMgr)
$nsUri = $nsMgr.LookupNamespace("taskbar")

switch ($Type) {
    "UWA" {
        $newNode = $xml.CreateElement("taskbar", "UWA", $nsUri)
        $newNode.SetAttribute("AppUserModelID", $Path)
    }
    "Link" {
        $newNode = $xml.CreateElement("taskbar", "DesktopApp", $nsUri)
        $newNode.SetAttribute("DesktopApplicationLinkPath", $Path)
    }
}

$pinList.AppendChild($newNode) | Out-Null
$xml.Save($xmlPath)

$regPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Explorer"

Set-ItemProperty -Path $regPath -Name "StartLayoutFile" -Value $xmlPath -Type ExpandString
Set-ItemProperty -Path $regPath -Name "LockedStartLayout" -Value 1 -Type DWord

Get-Process explorer | Stop-Process -Force
Start-Sleep 4