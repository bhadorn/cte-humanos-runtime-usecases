<#
.SYNOPSIS
  Adds another room to the device RoomCalendars: copies a room group with all its nodes and gives every
  copied node a new id.

.DESCRIPTION
  A room is one group of the device RoomCalendars: the property Role = Room, the property RoomId and the same
  child nodes (Status, LastUpdate, EventCount, Busy, Events, Event01 ... Event10). The command ReadCalendars
  finds the rooms by that property, so a copied group is a new room without any other change.

  The script changes the device file of the target (default\Devices\RoomCalendars.json) and the device
  template (DeviceTemplates\RoomCalendars.json). Both files are written again with the formatting of
  PowerShell (a larger diff than the one new group). Close the project in the Designer first, rebuild
  the target afterwards.

.EXAMPLE
  .\Add-Room.ps1 -Name Room3 -RoomId 00000000-0000-0000-0000-000000000000
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][string] $Name,
  [string] $RoomId = '',
  [string] $CopyFrom = 'Room1',
  [string[]] $DeviceFile = @(
    (Join-Path $PSScriptRoot '..\default\Devices\RoomCalendars.json'),
    (Join-Path $PSScriptRoot '..\DeviceTemplates\RoomCalendars.json'))
)

$ErrorActionPreference = 'Stop'

# every object with an Id (groups and data nodes) gets a new Id
function Set-NewIds($Item) {
  if ($Item -is [System.Array]) {
    foreach ($Entry in $Item) { Set-NewIds $Entry }
  }
  elseif ($Item -is [System.Management.Automation.PSCustomObject]) {
    if ($Item.PSObject.Properties['Id']) { $Item.Id = [guid]::NewGuid().ToString() }
    foreach ($Property in $Item.PSObject.Properties) {
      if ($Property.Value -is [System.Array] -or $Property.Value -is [System.Management.Automation.PSCustomObject]) { Set-NewIds $Property.Value }
    }
  }
}

foreach ($File in $DeviceFile) {
  $Path = (Resolve-Path $File).Path
  $Device = Get-Content -Raw -Encoding UTF8 $Path | ConvertFrom-Json

  if ($Device.Groups | Where-Object { $_.Name -eq $Name }) { throw "$Path already has a group '$Name'." }
  $Source = $Device.Groups | Where-Object { $_.Name -eq $CopyFrom }
  if (-not $Source) { throw "$Path has no group '$CopyFrom' to copy from." }

  $Copy = $Source | ConvertTo-Json -Depth 50 | ConvertFrom-Json
  Set-NewIds $Copy
  $Copy.Name = $Name
  foreach ($Property in $Copy.Properties) {
    if ($Property.Name -eq 'RoomId') { $Property.Value = $RoomId }
  }

  $Device.Groups = @($Device.Groups) + $Copy
  $Json = $Device | ConvertTo-Json -Depth 50
  [System.IO.File]::WriteAllText($Path, $Json, (New-Object System.Text.UTF8Encoding($false)))
  Write-Host "Added the group '$Name' to $Path"
}
