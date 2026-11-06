<#
.SYNOPSIS
  Restricts the calendar access of the Entra app to the rooms of a mail-enabled security group.

.DESCRIPTION
  The permission Calendars.ReadBasic.All applies to ALL mailboxes of the directory. An application access
  policy of Exchange Online restricts one app (by its client id) to the members of a mail-enabled security
  group. This script
    1. creates the group (name and address from the parameters) with the room mailbox as its member,
    2. creates the policy with -AccessRight RestrictAccess for the app,
    3. runs Test-ApplicationAccessPolicy for the room (must say Granted) and, if given, for a mailbox
       that must stay out of reach (-OtherMailbox, must say Denied).
  A policy takes effect with a delay of up to about one hour. Wait until the test shows Denied for the other
  mailbox before the first run of the agent. With -Remove the policy and the group are deleted.

  Requirements: module ExchangeOnlineManagement and an Exchange administrator (role "Organization Management").
  A Graph administrator alone cannot create the policy.

  Note: derived from commands used for the first tests of the agent; not run in exactly this form. Microsoft is
  replacing application access policies step by step with "RBAC for Applications". Ask the administrator of the
  directory which of the two applies.

.EXAMPLE
  .\Set-AccessPolicy.ps1 -AdminUpn admin@contoso.onmicrosoft.com -AppId <CLIENT-ID> `
    -RoomMail example-room@contoso.onmicrosoft.com -GroupMail example-room-access@contoso.onmicrosoft.com `
    -OtherMailbox colleague@contoso.onmicrosoft.com

.EXAMPLE
  .\Set-AccessPolicy.ps1 -AdminUpn admin@contoso.onmicrosoft.com -AppId <CLIENT-ID> `
    -GroupMail example-room-access@contoso.onmicrosoft.com -Remove
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][string] $AdminUpn,
  [Parameter(Mandatory)][string] $AppId,
  [string] $RoomMail,
  [Parameter(Mandatory)][string] $GroupMail,
  [string] $GroupName = 'Exchange Agent Rooms',
  [string] $OtherMailbox,
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
  if ($Remove) {
    $policies = @(Get-ApplicationAccessPolicy | Where-Object { $_.AppId -eq $AppId })
    Write-Host "Will delete $($policies.Count) policy(ies) of the app $AppId and the group $GroupMail"
    if (-not (Confirm-Step 'Continue?')) { return }
    foreach ($policy in $policies) { Remove-ApplicationAccessPolicy -Identity $policy.Identity -Confirm:$false }
    Remove-DistributionGroup -Identity $GroupMail -Confirm:$false -ErrorAction SilentlyContinue
    Write-Host 'Deleted. Without a policy the app can read every mailbox again.'
    return
  }

  if (-not $RoomMail) { throw 'Parameter -RoomMail is required to create the policy.' }
  Write-Host "Will create the mail-enabled security group '$GroupName' <$GroupMail> with the member $RoomMail,"
  Write-Host "and the policy RestrictAccess for the app $AppId on that group."
  if (-not (Confirm-Step 'Continue?')) { return }

  New-DistributionGroup -Name $GroupName -Type Security -PrimarySmtpAddress $GroupMail -Members $RoomMail | Out-Null
  New-ApplicationAccessPolicy -AppId $AppId -PolicyScopeGroupId $GroupMail -AccessRight RestrictAccess `
    -Description 'HumanOS Exchange Agent example' | Out-Null

  Write-Host 'Check (the room must be Granted):'
  Test-ApplicationAccessPolicy -Identity $RoomMail -AppId $AppId | Format-List AccessCheckResult, Identity
  if ($OtherMailbox) {
    Write-Host 'Check (the other mailbox must become Denied, this can take up to about one hour):'
    Test-ApplicationAccessPolicy -Identity $OtherMailbox -AppId $AppId | Format-List AccessCheckResult, Identity
  }
}
finally {
  Disconnect-ExchangeOnline -Confirm:$false
}
