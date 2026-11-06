<#
.SYNOPSIS
  Creates a room mailbox for trying out the Exchange Agent example, and prints its object id.

.DESCRIPTION
  Creates a room mailbox (needs no license) and sets the calendar processing so that the subject and the
  organizer of a booking stay visible in the room calendar (DeleteSubject and AddOrganizerToSubject are
  $false). Without this the agent reads events without a usable subject. Prints the object id of the
  mailbox: that id is the value of the property RoomId in the Designer project.
  With -Remove the room mailbox is deleted again.

  Requirements: PowerShell with the module ExchangeOnlineManagement
  (Install-Module ExchangeOnlineManagement -Scope CurrentUser) and an Exchange administrator account.

  Note: derived from the script used for the first tests of the agent; not run in exactly this form.
  Use it in a test directory, not for rooms that people really book.

.EXAMPLE
  .\New-RoomMailbox.ps1 -AdminUpn admin@contoso.onmicrosoft.com -RoomMail example-room@contoso.onmicrosoft.com

.EXAMPLE
  .\New-RoomMailbox.ps1 -AdminUpn admin@contoso.onmicrosoft.com -RoomMail example-room@contoso.onmicrosoft.com -Remove
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][string] $AdminUpn,
  [Parameter(Mandatory)][string] $RoomMail,
  [string] $RoomName = 'Example Room',
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

Import-Module ExchangeOnlineManagement
Connect-ExchangeOnline -UserPrincipalName $AdminUpn -ShowBanner:$false

try {
  $existing = Get-Mailbox -Identity $RoomMail -ErrorAction SilentlyContinue

  if ($Remove) {
    if (-not $existing) { Write-Host "No mailbox with the address $RoomMail found."; return }
    if ($existing.RecipientTypeDetails -ne 'RoomMailbox') { throw "$RoomMail is not a room mailbox ($($existing.RecipientTypeDetails)), nothing is deleted." }
    Write-Host "Will delete the room mailbox '$($existing.DisplayName)' <$($existing.PrimarySmtpAddress)>"
    if (-not (Confirm-Step 'Continue?')) { return }
    Remove-Mailbox -Identity $RoomMail -Confirm:$false
    Write-Host 'Deleted.'
    return
  }

  if ($existing) { throw "A mailbox with the address $RoomMail exists already. Nothing is changed." }

  Write-Host "Will create the room mailbox '$RoomName' <$RoomMail> with visible subject and organizer."
  if (-not (Confirm-Step 'Continue?')) { return }

  New-Mailbox -Room -Name $RoomName -DisplayName $RoomName -PrimarySmtpAddress $RoomMail | Out-Null
  Set-CalendarProcessing -Identity $RoomMail -DeleteSubject $false -AddOrganizerToSubject $false
  Get-CalendarProcessing -Identity $RoomMail | Format-List DeleteSubject, AddOrganizerToSubject, AutomateProcessing

  $id = (Get-Mailbox -Identity $RoomMail).ExternalDirectoryObjectId
  Write-Host "Object id of the room mailbox (property RoomId in the Designer project): $id"
  Write-Host 'Next: book the room in Outlook (invite the room as a resource) and restrict the app to this room (Set-AccessPolicy.ps1).'
}
finally {
  Disconnect-ExchangeOnline -Confirm:$false
}
