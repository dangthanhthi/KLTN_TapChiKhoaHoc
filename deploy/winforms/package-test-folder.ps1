param(
    [string]$BundleName = "HUIT-Journal-WinForms-Thu-Nghiem-$(Get-Date -Format yyyyMMdd-HHmmss)"
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$distRoot = Join-Path $projectRoot 'dist\winforms'
$appSource = Join-Path $distRoot 'win-x64-framework-dependent'
$sourceRoot = Join-Path $projectRoot 'Winform\QL_TapChi_WinForms'
$bundleRoot = Join-Path $distRoot $BundleName
$zipPath = "$bundleRoot.zip"

if ($BundleName -notmatch '^[A-Za-z0-9_-]+$') {
    throw 'BundleName chỉ được chứa chữ, số, dấu gạch ngang và gạch dưới.'
}
if (-not (Test-Path -LiteralPath (Join-Path $appSource 'QL_TapChi_WinForms.exe'))) {
    throw 'Chưa có bản WinForms đã build. Chạy .\deploy\winforms\publish.ps1 -FrameworkDependent trước.'
}
if ((Test-Path -LiteralPath $bundleRoot) -or (Test-Path -LiteralPath $zipPath)) {
    throw "Gói đã tồn tại: $bundleRoot. Hãy chọn BundleName khác để giữ nguyên gói cũ."
}

$appDestination = Join-Path $bundleRoot 'App'
$sourceDestination = Join-Path $bundleRoot 'Source'
New-Item -ItemType Directory -Path $appDestination, $sourceDestination -Force | Out-Null

Get-ChildItem -LiteralPath $appSource -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $appDestination -Recurse
}

$sourceFiles = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj|Database)[\\/]' -and
    $_.Extension -in '.cs', '.csproj', '.png'
}
foreach ($file in $sourceFiles) {
    $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([char]'\', [char]'/')
    $target = Join-Path $sourceDestination $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target
}

@'
HUIT JOURNAL - HƯỚNG DẪN THỬ WINFORMS TRÊN MÁY KHÁC
====================================================

1. CHẠY ỨNG DỤNG
   Giải nén TOÀN BỘ thư mục này trên máy Windows 64-bit.
   Cài .NET 8 Desktop Runtime nếu máy chưa có.
   Mở App\QL_TapChi_WinForms.exe.
   Đăng nhập bằng tài khoản Ban biên tập hoặc Quản trị hệ thống.
   Cần Internet và Backend API HTTPS đang hoạt động.

2. KẾT NỐI VỚI WEB
   App\desktop-settings.json chứa URL API công khai.
   WinForms và Web cùng đọc/ghi qua Backend API vào CSDL của Backend.
   WinForms KHÔNG chạy trên Vercel và KHÔNG cần mở Vercel để đăng nhập.
   Không thêm /api vào ApiBaseUrl. Không chứa mật khẩu trong tệp này.
   Biến môi trường HUIT_JOURNAL_API_URL trên máy ưu tiên hơn tệp cấu hình.
   Nếu lỗi kết nối, kiểm tra Internet, URL API, HTTPS và quyền tài khoản.

3. CHỈNH SỬA
   Source\ chứa mã nguồn WinForms và tệp .csproj. Bạn có thể sửa giao diện,
   màu sắc, văn bản và các màn hình bằng Visual Studio / .NET 8 SDK.
   Sửa xong phải build/publish lại; file .exe không tự đổi theo mã nguồn.
   Từ thư mục này, chạy:
   dotnet restore .\Source\QL_TapChi_WinForms.csproj
   dotnet publish .\Source\QL_TapChi_WinForms.csproj -c Release --self-contained false -o .\App
   Giữ App\desktop-settings.json khi cập nhật bản build.

   Bạn có thể sửa riêng WinForms mà không ảnh hưởng phần kết nối nếu giữ
   địa chỉ API, endpoint, kiểu JSON, JWT/vai trò và trạng thái nghiệp vụ.
   Nếu đổi hợp đồng API hoặc trạng thái nghiệp vụ, cần sửa Backend tương ứng
   và kiểm thử lại cả Web lẫn WinForms trước khi phát hành.

4. GIỚI HẠN
   Sao lưu/phục hồi cần quyền SQL trực tiếp trên máy quản trị. Không mở
   SQL Server ra Internet chỉ để dùng chức năng này từ máy thử.
   Gói thử cần .NET 8 Desktop Runtime; không chứa CSDL hay Backend.
   Các thay đổi Backend/Web mới trong mã nguồn chưa lên Production.
'@ | Set-Content -LiteralPath (Join-Path $bundleRoot 'HUONG_DAN_SU_DUNG_VA_CHINH_SUA.txt') -Encoding utf8

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $bundleRoot 'TAI_LIEU_KET_NOI_API.md')
Compress-Archive -Path $bundleRoot -DestinationPath $zipPath

$required = @(
    (Join-Path $appDestination 'QL_TapChi_WinForms.exe'),
    (Join-Path $appDestination 'desktop-settings.json'),
    (Join-Path $sourceDestination 'QL_TapChi_WinForms.csproj'),
    (Join-Path $bundleRoot 'HUONG_DAN_SU_DUNG_VA_CHINH_SUA.txt')
)
foreach ($file in $required) {
    if (-not (Test-Path -LiteralPath $file)) { throw "Gói thiếu file: $file" }
}

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
Write-Output "Thư mục: $bundleRoot"
Write-Output "ZIP: $zipPath"
Write-Output "Mã nguồn WinForms: $($sourceFiles.Count) file"
Write-Output "SHA256: $hash"
