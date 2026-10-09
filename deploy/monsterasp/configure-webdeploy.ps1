param(
    [Parameter(Mandatory = $true)][string]$PublishSettings,
    [string]$StateDirectory = (Join-Path $env:LOCALAPPDATA 'HuitJournal\MonsterASP')
)
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Cấu hình này sử dụng mã hóa DPAPI trên Windows.' }
[xml]$profileXml = Get-Content -LiteralPath $PublishSettings -Raw
$profiles = @($profileXml.publishData.publishProfile | Where-Object { $_.publishMethod -eq 'MSDeploy' })
if ($profiles.Count -ne 1) { throw 'Cần đúng một profile MSDeploy trong file publishSettings.' }
$profile = $profiles[0]
$site = [string]$profile.msdeploySite
if ($site -ne 'site94240') { throw 'Profile không thuộc website HUIT site94240.' }
$publishUrl = [string]$profile.publishUrl
if ($publishUrl -notmatch '^https://') { $publishUrl = 'https://' + $publishUrl }
$endpoint = [uri]$publishUrl
if ($endpoint.IsDefaultPort -and $endpoint.Host -eq 'site94240.siteasp.net') {
    $endpointBuilder = New-Object System.UriBuilder $endpoint
    $endpointBuilder.Port = 8172
    $publishUrl = $endpointBuilder.Uri.AbsoluteUri
    $endpoint = [uri]$publishUrl
}
if ($endpoint.Host -ne 'site94240.siteasp.net' -or $endpoint.Port -ne 8172) { throw 'Endpoint không thuộc WebDeploy của website HUIT.' }
if ($endpoint.AbsolutePath -eq '/') { $publishUrl = $publishUrl.TrimEnd('/') + '/msdeploy.axd?site=site94240' }
elseif ($endpoint.Query -eq '') { $publishUrl += '?site=site94240' }
if ([string]::IsNullOrWhiteSpace([string]$profile.userPWD)) { throw 'Profile không có mật khẩu WebDeploy.' }
$securePassword = ConvertTo-SecureString ([string]$profile.userPWD) -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential ([string]$profile.userName), $securePassword
New-Item -ItemType Directory -Path $StateDirectory -Force | Out-Null
# Only this Windows user and SYSTEM can read the local deployment state.
$acl = Get-Acl -LiteralPath $StateDirectory
$acl.SetAccessRuleProtection($true, $false)
$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$acl.SetAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($identity, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
$acl.SetAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule('SYSTEM', 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
Set-Acl -LiteralPath $StateDirectory -AclObject $acl
$credential | Export-Clixml -LiteralPath (Join-Path $StateDirectory 'credential.xml')
@{ Site = $site; Endpoint = $publishUrl; HealthUrl = 'https://huit-journal-api.runasp.net/api/chuyennganh' } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $StateDirectory 'settings.json') -Encoding UTF8
Write-Host 'Đã lưu cấu hình WebDeploy. Mật khẩu được Windows mã hóa cho tài khoản hiện tại, không nằm trong repository.'
