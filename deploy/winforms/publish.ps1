param(
    [string]$ApiOrigin = 'https://huit-journal-api.runasp.net',
    [ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64',
    [switch]$FrameworkDependent
)

$ErrorActionPreference = 'Stop'
if ($FrameworkDependent -and $Runtime -ne 'win-x64') {
    throw 'Chế độ FrameworkDependent hiện chỉ được kiểm chứng trên win-x64.'
}
$origin = $null
if (-not [Uri]::TryCreate($ApiOrigin, [UriKind]::Absolute, [ref]$origin) -or
    $origin.Scheme -ne 'https' -or $origin.AbsolutePath -ne '/' -or
    $origin.Query -or $origin.Fragment -or $origin.UserInfo) {
    throw 'ApiOrigin phải là origin HTTPS, ví dụ https://huit-journal-api.runasp.net (không có /api).'
}

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $root 'Winform\QL_TapChi_WinForms\QL_TapChi_WinForms.csproj'
$packageName = if ($FrameworkDependent) { "$Runtime-framework-dependent" } else { $Runtime }
$output = Join-Path $root "dist\winforms\$packageName"
New-Item -ItemType Directory -Path $output -Force | Out-Null

if ($FrameworkDependent) {
    dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore thất bại: $LASTEXITCODE" }
    dotnet publish $project --configuration Release --no-restore --self-contained false `
        -p:DebugType=None --output $output
} else {
    dotnet restore $project --ignore-failed-sources --runtime $Runtime -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore thất bại: $LASTEXITCODE" }
    dotnet publish $project --configuration Release --runtime $Runtime --no-restore --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None `
        --output $output
}
if ($LASTEXITCODE -ne 0) { throw "dotnet publish thất bại: $LASTEXITCODE" }

@{ ApiBaseUrl = $origin.GetLeftPart([UriPartial]::Authority) } |
    ConvertTo-Json -Compress |
    Set-Content -LiteralPath (Join-Path $output 'desktop-settings.json') -Encoding utf8

$zip = Join-Path $root "dist\winforms\HUIT-Journal-Desktop-$packageName.zip"
Compress-Archive -Path (Join-Path $output '*') -DestinationPath $zip -Force
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
Write-Output "Gói WinForms: $zip"
Write-Output "SHA256: $hash"
Write-Output "API: $($origin.GetLeftPart([UriPartial]::Authority))"
