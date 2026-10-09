param(
    [switch]$Apply,
    [switch]$PrepareOnly,
    [string]$StateDirectory = (Join-Path $env:LOCALAPPDATA 'HuitJournal\MonsterASP')
)
$ErrorActionPreference = 'Stop'
$timer = [Diagnostics.Stopwatch]::StartNew()
$deployment = & (Join-Path $PSScriptRoot 'prepare.ps1') -SkipArchive -PassThru
if (-not $deployment.PublishPath) { throw 'Không nhận được thư mục publish đã kiểm tra.' }
Write-Host ('Chuẩn bị bản phát hành: {0:N1} giây.' -f $timer.Elapsed.TotalSeconds)
if ($PrepareOnly) { Write-Host 'Chỉ chuẩn bị local; chưa kết nối hoặc cập nhật MonsterASP.'; return }
$settingsPath = Join-Path $StateDirectory 'settings.json'
$credentialPath = Join-Path $StateDirectory 'credential.xml'
if (-not (Test-Path -LiteralPath $settingsPath) -or -not (Test-Path -LiteralPath $credentialPath)) {
    throw 'Chưa cấu hình WebDeploy. Chạy configure-webdeploy.ps1 với file publishSettings tải từ MonsterASP.'
}
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
if ($settings.Site -ne 'site94240' -or ([uri]$settings.Endpoint).Host -ne 'site94240.siteasp.net' -or ([uri]$settings.Endpoint).Scheme -ne 'https') {
    throw 'Cấu hình đích không thuộc website HUIT được phép triển khai.'
}
$credential = Import-Clixml -LiteralPath $credentialPath
$msdeploy = @('C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe', 'C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe') |
    Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $msdeploy) { throw 'Chưa cài Microsoft Web Deploy.' }
function Quote-ProviderValue([string]$Value) {
    if (-not $Value.Contains('"')) { return '"' + $Value + '"' }
    if (-not $Value.Contains("'")) { return "'" + $Value + "'" }
    throw 'Giá trị WebDeploy chứa đồng thời hai loại dấu nháy nên không thể truyền an toàn.'
}
$plainPassword = $credential.GetNetworkCredential().Password
try {
    $destination = '-dest:contentPath=' + (Quote-ProviderValue $settings.Site) + ',computerName=' + (Quote-ProviderValue $settings.Endpoint) +
        ',userName=' + (Quote-ProviderValue $credential.UserName) + ',password=' + (Quote-ProviderValue $plainPassword) + ',authType=Basic,includeAcls=False'
    $arguments = @('-verb:sync', ('-source:contentPath=' + (Quote-ProviderValue $deployment.PublishPath)), $destination,
        '-retryAttempts:3', '-enableRule:DoNotDeleteRule', '-disableLink:AppPoolExtension', '-disableLink:ContentExtension', '-disableLink:CertificateExtension',
        '-skip:objectName=dirPath,absolutePath=.+[\\/](Uploads|logs)([\\/].*)?$',
        '-skip:objectName=filePath,absolutePath=.+[\\/](Uploads|logs)[\\/].*',
        '-skip:objectName=filePath,absolutePath=.+[\\/]appsettings[^\\/]*\.json$')
    if ($Apply) { $arguments += '-enableRule:AppOffline'; Write-Host 'Đang cập nhật API MonsterASP; không xóa file đích, không thay đổi SQL hoặc cấu hình máy chủ.' }
    else { $arguments += '-whatif'; Write-Host 'Đang xem trước thay đổi; không ghi lên website.' }
    $transferTimer = [Diagnostics.Stopwatch]::StartNew()
    # Web Deploy's legacy parser expects Windows PowerShell argument quoting.
    $previousArgumentPassing = $PSNativeCommandArgumentPassing
    try {
        $PSNativeCommandArgumentPassing = 'Legacy'
        $result = & $msdeploy @arguments 2>&1
    } finally { $PSNativeCommandArgumentPassing = $previousArgumentPassing }
    $exitCode = $LASTEXITCODE
    foreach ($line in $result) { Write-Host ([string]$line).Replace($plainPassword, '[REDACTED]') }
    if ($exitCode -ne 0) { throw "WebDeploy thất bại (exit code $exitCode). Không tự thử lại hoặc bỏ qua lỗi chứng chỉ." }
    Write-Host ('Đồng bộ: {0:N1} giây.' -f $transferTimer.Elapsed.TotalSeconds)
} finally { $plainPassword = $null; $destination = $null; $arguments = $null }
if ($Apply) {
    $healthy = $false
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            $data = Invoke-RestMethod -Uri $settings.HealthUrl -TimeoutSec 15
            if (@($data).Count -lt 1) { throw 'API không trả danh sách chuyên ngành.' }
            $healthy = $true; break
        } catch { if ($attempt -lt 3) { Start-Sleep -Seconds 2 } }
    }
    if (-not $healthy) { throw 'Đã đồng bộ nhưng API chưa hoạt động. Kiểm tra log MonsterASP và app_offline.htm trước khi triển khai tiếp.' }
    Write-Host 'API đã trả dữ liệu JSON sau cập nhật.'
}
Write-Host ('Tổng thời gian: {0:N1} giây.' -f $timer.Elapsed.TotalSeconds)
