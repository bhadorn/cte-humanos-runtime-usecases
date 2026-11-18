<#
.SYNOPSIS
  Creates an equipment mailbox that stands for a bookable machine, and prints its object id.

.DESCRIPTION
  Creates an equipment mailbox (needs no license) and sets the calendar processing so that
    - a booking is accepted automatically when the machine is free (AutomateProcessing AutoAccept),
    - the subject and the organizer of a booking stay visible in the calendar (DeleteSubject and
      AddOrganizerToSubject are $false). Without this the example reads bookings without a usable subject.
  Prints the object id of the mailbox: that id is the value of the property ResourceId of the device
  MachineBooking in the Designer project.
  With -Remove the equipment mailbox is deleted again.

  A room mailbox works the same way: use -Room to create a room instead of equipment.

  Requirements: PowerShell with the module ExchangeOnlineManagement
  (Install-Module ExchangeOnlineManagement -Scope CurrentUser) and an Exchange administrator account.

  Note: derived from the room script of the Exchange Agent example; not run in exactly this form.
  Use it in a test directory, not for a mailbox that people really book.

.EXAMPLE
  .\New-EquipmentMailbox.ps1 -AdminUpn admin@contoso.onmicrosoft.com -MachineMail test-machine@contoso.onmicrosoft.com -MachineName "FANUC Demo"

.EXAMPLE
  .\New-EquipmentMailbox.ps1 -AdminUpn admin@contoso.onmicrosoft.com -MachineMail test-machine@contoso.onmicrosoft.com -Remove
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][string] $AdminUpn,
  [Parameter(Mandatory)][string] $MachineMail,
  [string] $MachineName = 'Demo Machine',
  [switch] $Room,
  [switch] $Remove,
  [switch] $Force
)

$ErrorActionPreference = 'Stop'

function Confirm-Step([string] $Question) {
  if ($Force) { return $true }
  return (Read-Host "$Question (y/n)") -eq 'y'
}

if (-not (Get-Module -ListAvailable -Name ExchangeOnlineManagement)) {
  throw 'Module missing: Install-Module ExchangeOnlineManagement -Scope CurrentUser'
}

$expectedType = if ($Room) { 'RoomMailbox' } else { 'EquipmentMailbox' }

Import-Module ExchangeOnlineManagement
Connect-ExchangeOnline -UserPrincipalName $AdminUpn -ShowBanner:$false

try {
  $existing = Get-Mailbox -Identity $MachineMail -ErrorAction SilentlyContinue

  if ($Remove) {
    if (-not $existing) { Write-Host "No mailbox with the address $MachineMail found."; return }
    if ($existing.RecipientTypeDetails -ne $expectedType) { throw "$MachineMail is not an $expectedType ($($existing.RecipientTypeDetails)), nothing is deleted." }
    Write-Host "Will delete the $expectedType '$($existing.DisplayName)' <$($existing.PrimarySmtpAddress)>"
    if (-not (Confirm-Step 'Continue?')) { return }
    Remove-Mailbox -Identity $MachineMail -Confirm:$false
    Write-Host 'Deleted.'
    return
  }

  if ($existing) { throw "A mailbox with the address $MachineMail exists already. Nothing is changed." }

  Write-Host "Will create the $expectedType '$MachineName' <$MachineMail> with automatic accept and visible subject and organizer."
  if (-not (Confirm-Step 'Continue?')) { return }

  if ($Room) {
    New-Mailbox -Room -Name $MachineName -DisplayName $MachineName -PrimarySmtpAddress $MachineMail | Out-Null
  } else {
    New-Mailbox -Equipment -Name $MachineName -DisplayName $MachineName -PrimarySmtpAddress $MachineMail | Out-Null
  }
  Set-CalendarProcessing -Identity $MachineMail -AutomateProcessing AutoAccept -DeleteSubject $false -AddOrganizerToSubject $false
  Get-CalendarProcessing -Identity $MachineMail | Format-List DeleteSubject, AddOrganizerToSubject, AutomateProcessing

  $id = (Get-Mailbox -Identity $MachineMail).ExternalDirectoryObjectId
  Write-Host "Object id of the mailbox (property ResourceId of the device MachineBooking in the Designer project): $id"
  Write-Host 'Next: book the machine in Outlook (invite it as a resource) and restrict the app to this mailbox (Set-AccessPolicy.ps1 of the Exchange Agent example).'
}
finally {
  Disconnect-ExchangeOnline -Confirm:$false
}
