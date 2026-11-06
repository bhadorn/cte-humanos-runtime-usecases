<#
.SYNOPSIS
  Registers the Entra app for the Exchange Agent example (read access to calendars only).

.DESCRIPTION
  Creates an app registration in the given directory (tenant) with the single application permission
  Calendars.ReadBasic.All, grants the admin consent and creates a client secret that expires after
  -SecretValidityDays. With -Remove the app (and with it every secret) is deleted again.

  Requirements: PowerShell with the module Microsoft.Graph.Applications
  (Install-Module Microsoft.Graph.Applications -Scope CurrentUser) and an account with the roles
  "Application Administrator" and "Privileged Role Administrator" (or Global Administrator).

  The secret is only printed on the console, never written to a file. It belongs into the secret
  database of the Designer project (README, step 5) and never into the repository.

  Note: this script was derived from the one used for the first tests of the agent against a real tenant.
  It has not been run in exactly this form. Read what it does before you run it in a production directory.

.EXAMPLE
  .\New-GraphApp.ps1 -TenantId contoso.onmicrosoft.com

.EXAMPLE
  .\New-GraphApp.ps1 -TenantId contoso.onmicrosoft.com -Remove
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][string] $TenantId,
  [string] $AppName = 'HumanOS Exchange Agent',
  [ValidateRange(1, 180)][int] $SecretValidityDays = 90,
  [switch] $Remove,
  [switch] $Force
)

$ErrorActionPreference = 'Stop'
$permissions = 'Calendars.ReadBasic.All'
$graphAppId = '00000003-0000-0000-c000-000000000000'   # Microsoft Graph

function Confirm-Step([string] $Question) {
  if ($Force) { return $true }
  return (Read-Host "$Question (y/n)") -eq 'y'
}

if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Applications)) {
  throw 'Module missing: Install-Module Microsoft.Graph.Applications -Scope CurrentUser'
}

Connect-MgGraph -TenantId $TenantId -Scopes 'Application.ReadWrite.All', 'AppRoleAssignment.ReadWrite.All' -NoWelcome
$context = Get-MgContext
Write-Host "Signed in as $($context.Account) in directory $($context.TenantId)"

$existing = @(Get-MgApplication -Filter "displayName eq '$AppName'")

if ($Remove) {
  if ($existing.Count -eq 0) { Write-Host "No app named '$AppName' found."; return }
  Write-Host "Will delete $($existing.Count) app(s) named '$AppName' (AppId $($existing.AppId -join ', '))"
  if (-not (Confirm-Step 'Continue?')) { return }
  foreach ($app in $existing) { Remove-MgApplication -ApplicationId $app.Id }
  Write-Host 'Deleted. The client secret is invalid now.'
  return
}

if ($existing.Count -gt 0) {
  throw "An app named '$AppName' exists already (AppId $($existing[0].AppId)). Delete it with -Remove or choose another -AppName."
}

$graphSp = Get-MgServicePrincipal -Filter "appId eq '$graphAppId'"
$roles = foreach ($permission in $permissions) {
  $role = $graphSp.AppRoles | Where-Object { $_.Value -eq $permission -and $_.AllowedMemberTypes -contains 'Application' }
  if (-not $role) { throw "Permission not found: $permission" }
  $role
}

Write-Host "Will create the app '$AppName' with the application permission(s): $($permissions -join ', ')."
Write-Host "The client secret is valid for $SecretValidityDays days. The permission is read only, but it applies to ALL mailboxes of the directory until you restrict it (README, step 4)."
if (-not (Confirm-Step 'Continue?')) { return }

$resourceAccess = @($roles | ForEach-Object { @{ Id = $_.Id; Type = 'Role' } })
$app = New-MgApplication -DisplayName $AppName -SignInAudience 'AzureADMyOrg' `
  -RequiredResourceAccess @(@{ ResourceAppId = $graphAppId; ResourceAccess = $resourceAccess })
$sp = New-MgServicePrincipal -AppId $app.AppId

# admin consent: assign every role of the app to its own service principal
foreach ($role in $roles) {
  New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $sp.Id -PrincipalId $sp.Id -ResourceId $graphSp.Id -AppRoleId $role.Id | Out-Null
}

$secret = Add-MgApplicationPassword -ApplicationId $app.Id `
  -PasswordCredential @{ DisplayName = 'Exchange Agent'; EndDateTime = (Get-Date).AddDays($SecretValidityDays) }

Write-Host ''
Write-Host 'Values for the Designer project (variables of the device GraphClient):'
Write-Host "  GRAPH_CLIENT_ID : $($app.AppId)"
Write-Host "  GRAPH_TENANT_ID : $($context.TenantId)"
Write-Host "  Client secret   : $($secret.SecretText)"
Write-Host "  valid until     : $($secret.EndDateTime)"
Write-Host 'The secret is shown only now. Do not write it to a file or into the repository: enter it in the secret database of the Designer.'
