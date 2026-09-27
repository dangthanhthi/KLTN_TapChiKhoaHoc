param(
    [string]$TenGoi = 'KLTN_HUIT_MANG_DEN_TRUONG_20260926.zip'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$thuMucDuAn = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$duongDanZip = [System.IO.Path]::GetFullPath((Join-Path $thuMucDuAn $TenGoi))
$duongDanDanhSach = Join-Path $PSScriptRoot 'GOI_MANG_DI_TRUONG_DANH_SACH_SHA256.txt'
if (-not $duongDanZip.StartsWith($thuMucDuAn + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Gói ZIP phải nằm trong thư mục dự án.'
}
if (Test-Path -LiteralPath $duongDanZip) {
    throw "Gói ZIP đã tồn tại: $duongDanZip. Hãy đặt tên mới nếu muốn tạo thêm bản."
}

$tepNguon = @(Get-ChildItem -LiteralPath $thuMucDuAn -Recurse -File -Force |
    Where-Object { $_.FullName -ne $duongDanZip -and $_.FullName -ne $duongDanDanhSach })
$bangBam = @{}
$dong = [System.Collections.Generic.List[string]]::new()
$dong.Add('# SHA256 | Bytes | DuongDanTuGocDuAn')
foreach ($tep in $tepNguon) {
    $tuongDoi = $tep.FullName.Substring($thuMucDuAn.Length + 1).Replace('\', '/')
    $bam = (Get-FileHash -LiteralPath $tep.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
    $bangBam[$tuongDoi] = $bam
    $dong.Add($bam + [char]9 + $tep.Length + [char]9 + $tuongDoi)
}
[System.IO.File]::WriteAllLines($duongDanDanhSach, $dong, [System.Text.UTF8Encoding]::new($false))
$tepDongGoi = @($tepNguon) + @(Get-Item -LiteralPath $duongDanDanhSach)

$dauRa = [System.IO.File]::Open($duongDanZip, [System.IO.FileMode]::CreateNew)
try {
    $zip = [System.IO.Compression.ZipArchive]::new($dauRa, [System.IO.Compression.ZipArchiveMode]::Create, $true, [System.Text.Encoding]::UTF8)
    try {
        foreach ($tep in $tepDongGoi) {
            $tuongDoi = $tep.FullName.Substring($thuMucDuAn.Length + 1).Replace('\', '/')
            $muc = $zip.CreateEntry($tuongDoi, [System.IO.Compression.CompressionLevel]::Optimal)
            $nguon = [System.IO.File]::OpenRead($tep.FullName)
            try {
                $dich = $muc.Open()
                try { $nguon.CopyTo($dich) }
                finally { $dich.Dispose() }
            }
            finally { $nguon.Dispose() }
        }
    }
    finally { $zip.Dispose() }
}
finally { $dauRa.Dispose() }

$kiemTra = [System.IO.Compression.ZipFile]::OpenRead($duongDanZip)
try {
    if ($kiemTra.Entries.Count -ne $tepDongGoi.Count) {
        throw "Số tệp trong ZIP không khớp: $($kiemTra.Entries.Count)/$($tepDongGoi.Count)."
    }
    $daThay = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $boBam = [System.Security.Cryptography.SHA256]::Create()
    try {
        foreach ($muc in $kiemTra.Entries) {
            if ($muc.FullName -eq 'HoTro/GOI_MANG_DI_TRUONG_DANH_SACH_SHA256.txt') { continue }
            if (-not $bangBam.ContainsKey($muc.FullName)) {
                throw "Tệp lạ trong ZIP: $($muc.FullName)"
            }
            if (-not $daThay.Add($muc.FullName)) {
                throw "Tệp trùng trong ZIP: $($muc.FullName)"
            }
            $luong = $muc.Open()
            try { $bam = [BitConverter]::ToString($boBam.ComputeHash($luong)).Replace('-', '') }
            finally { $luong.Dispose() }
            if ($bam -ne $bangBam[$muc.FullName]) {
                throw "Sai SHA256 sau giải nén: $($muc.FullName)"
            }
        }
        if ($daThay.Count -ne $bangBam.Count) {
            throw "ZIP thiếu tệp nguồn: $($daThay.Count)/$($bangBam.Count)."
        }
    }
    finally { $boBam.Dispose() }
}
finally { $kiemTra.Dispose() }

$ketQua = Get-Item -LiteralPath $duongDanZip
$bamZip = (Get-FileHash -LiteralPath $duongDanZip -Algorithm SHA256).Hash
[pscustomobject]@{
    Goi = $ketQua.FullName
    SoTep = $tepDongGoi.Count
    KichThuocMB = [math]::Round($ketQua.Length / 1MB, 2)
    SHA256 = $bamZip
    KiemTra = 'Đã đối chiếu SHA256 mọi tệp trong ZIP'
} | Format-List
