param(
    [string]$OutputRoot
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$projectPath = Join-Path $repositoryRoot 'Backend\HuitJournal.Api\HuitJournal.Api.csproj'
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot 'dist\monsterasp'
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$publishPath = Join-Path $OutputRoot "api-$stamp"
$archivePath = Join-Path $OutputRoot "api-$stamp.zip"
New-Item -ItemType Directory -Path $publishPath -Force | Out-Null

& dotnet publish $projectPath --configuration Release --output $publishPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish thất bại (exit code $LASTEXITCODE)."
}

foreach ($privateFile in @('appsettings.Local.json', 'appsettings.Development.json', 'appsettings.Testing.json')) {
    if (Test-Path -LiteralPath (Join-Path $publishPath $privateFile)) {
        throw "Bản phát hành chứa cấu hình riêng tư: $privateFile"
    }
}
if (Test-Path -LiteralPath (Join-Path $publishPath 'Uploads')) {
    throw 'Bản phát hành chứa dữ liệu Uploads. Chuyển tệp này riêng, không đóng gói cùng mã nguồn.'
}
if (-not (Test-Path -LiteralPath (Join-Path $publishPath 'web.config'))) {
    throw 'Không tìm thấy web.config dành cho IIS trong bản phát hành.'
}
$publicSettings = Get-Content -LiteralPath (Join-Path $publishPath 'appsettings.json') -Raw | ConvertFrom-Json
if (-not [string]::IsNullOrWhiteSpace($publicSettings.Jwt.Key) -or
    -not [string]::IsNullOrWhiteSpace($publicSettings.EmailVerification.HmacSecretKey) -or
    -not [string]::IsNullOrWhiteSpace($publicSettings.EmailVerification.SmtpPassword)) {
    throw 'appsettings.json trong bản phát hành chứa khóa hoặc mật khẩu. Hãy chuyển chúng sang Environment Variables.'
}

Compress-Archive -Path (Join-Path $publishPath '*') -DestinationPath $archivePath -CompressionLevel Optimal
$checksum = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$archivePath.sha256.txt" -Value "$checksum  $(Split-Path -Leaf $archivePath)"
Write-Output "Thư mục phát hành: $publishPath"
Write-Output "Gói tải lên hosting: $archivePath"
Write-Output "SHA256: $checksum"
Write-Output 'Gói chỉ chứa ứng dụng. CSDL, Uploads và các khóa bí mật phải chuyển/cấu hình riêng.'
