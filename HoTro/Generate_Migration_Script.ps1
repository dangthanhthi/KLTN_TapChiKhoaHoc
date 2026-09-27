param(
    [string]$Server = ".",
    [string]$SourceDb = "QL_TapChiKhoaHoc_Inspect",
    [string]$OutputFile = "HoTro\Migration_20260927_ImportHistoricalPublishingData.sql"
)

$ErrorActionPreference = "Stop"
$conn = New-Object System.Data.SqlClient.SqlConnection("Server=$Server;Database=$SourceDb;Integrated Security=True;")
$conn.Open()

function Escape-Sql([object]$val) {
    if ($null -eq $val -or $val -is [System.DBNull]) { return "NULL" }
    $s = $val.ToString().Replace("'", "''")
    return "N'$s'"
}

function Format-SqlVal([object]$val, [string]$type) {
    if ($null -eq $val -or $val -is [System.DBNull]) { return "NULL" }
    switch -Regex ($type) {
        "Int32|Int64|Int16|Byte" { return $val.ToString() }
        "Boolean" { if ([bool]$val) { return "1" } else { return "0" } }
        "DateTime" { return "'" + ([DateTime]$val).ToString("yyyy-MM-dd HH:mm:ss") + "'" }
        default { return "N'" + $val.ToString().Replace("'", "''") + "'" }
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("-- =========================================================================")
[void]$sb.AppendLine("-- MIGRATION SCRIPT: NAP DU LIEU LICH SU XUAT BAN TU BACKUP 20260926")
[void]$sb.AppendLine("-- Sinh tu dong ngay: " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
[void]$sb.AppendLine("-- Nguon: " + $SourceDb + " (23 So tap chi, 230 Bai bao, 230 PDF, 185 Tac gia)")
[void]$sb.AppendLine("-- Tinh chat: Idempotent (chay nhieu lan an toan), bao toan 100% ID goc")
[void]$sb.AppendLine("-- =========================================================================")
[void]$sb.AppendLine("SET NOCOUNT ON;")
[void]$sb.AppendLine("SET XACT_ABORT ON;")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("PRINT N'=========================================================================';")
[void]$sb.AppendLine("PRINT N'BAT DAU MIGRATION DU LIEU LICH SU XUAT BAN KHOA HOC (2016 - 2026)';")
[void]$sb.AppendLine("PRINT N'Co so du lieu dich: ' + DB_NAME();")
[void]$sb.AppendLine("PRINT N'=========================================================================';")
[void]$sb.AppendLine("")

# 1. NguoiDung (MaNguoiDung >= 27)
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
[void]$sb.AppendLine("-- 1. NGUOI DUNG (TAC GIA BAI BAO LICH SU)")
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT * FROM NguoiDung WHERE MaNguoiDung >= 27 ORDER BY MaNguoiDung"
$da = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$dt = New-Object System.Data.DataTable
$da.Fill($dt) | Out-Null

$cols = @($dt.Columns | ForEach-Object { $_.ColumnName })
$colList = ($cols | ForEach-Object { "[$_]" }) -join ", "

[void]$sb.AppendLine("IF OBJECT_ID('tempdb..#TmpNguoiDung') IS NOT NULL DROP TABLE #TmpNguoiDung;")
[void]$sb.AppendLine("CREATE TABLE #TmpNguoiDung (")
$createCols = @()
foreach ($c in $dt.Columns) {
    $typeName = $c.DataType.Name
    $sqlType = switch ($typeName) {
        "Int32" { "INT" }
        "Int64" { "BIGINT" }
        "Boolean" { "BIT" }
        "DateTime" { "DATETIME" }
        default { "NVARCHAR(MAX)" }
    }
    $createCols += "  [$($c.ColumnName)] $sqlType"
}
[void]$sb.AppendLine(($createCols -join ",`n"))
[void]$sb.AppendLine(");")
[void]$sb.AppendLine("")

$batchSize = 40
for ($i = 0; $i -lt $dt.Rows.Count; $i += $batchSize) {
    $batch = @()
    for ($j = $i; $j -lt [Math]::Min($i + $batchSize, $dt.Rows.Count); $j++) {
        $row = $dt.Rows[$j]
        $vals = @()
        foreach ($c in $dt.Columns) {
            $vals += Format-SqlVal $row[$c.ColumnName] $c.DataType.Name
        }
        $batch += "(" + ($vals -join ", ") + ")"
    }
    [void]$sb.AppendLine("INSERT INTO #TmpNguoiDung ($colList) VALUES")
    [void]$sb.AppendLine(($batch -join ",`n") + ";")
}

[void]$sb.AppendLine("SET IDENTITY_INSERT NguoiDung ON;")
[void]$sb.AppendLine("INSERT INTO NguoiDung ($colList)")
[void]$sb.AppendLine("SELECT $colList FROM #TmpNguoiDung t")
[void]$sb.AppendLine("WHERE NOT EXISTS (SELECT 1 FROM NguoiDung n WHERE n.MaNguoiDung = t.MaNguoiDung);")
[void]$sb.AppendLine("SET IDENTITY_INSERT NguoiDung OFF;")
[void]$sb.AppendLine("DROP TABLE #TmpNguoiDung;")
[void]$sb.AppendLine("PRINT N'  [OK] Da dong bo ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' tai khoan nguoi dung lich su.';")
[void]$sb.AppendLine("")

# 2. NguoiDung_VaiTro
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
[void]$sb.AppendLine("-- 2. NGUOI DUNG - VAI TRO")
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
$cmd.CommandText = "SELECT MaNguoiDung, MaVaiTro FROM NguoiDung_VaiTro WHERE MaNguoiDung >= 27 ORDER BY MaNguoiDung, MaVaiTro"
$dtVT = New-Object System.Data.DataTable
(New-Object System.Data.SqlClient.SqlDataAdapter($cmd)).Fill($dtVT) | Out-Null
if ($dtVT.Rows.Count -gt 0) {
    [void]$sb.AppendLine("INSERT INTO NguoiDung_VaiTro (MaNguoiDung, MaVaiTro)")
    $rowsVT = @()
    foreach ($r in $dtVT.Rows) {
        $rowsVT += "SELECT $($r['MaNguoiDung']), $($r['MaVaiTro']) WHERE NOT EXISTS (SELECT 1 FROM NguoiDung_VaiTro WHERE MaNguoiDung = $($r['MaNguoiDung']) AND MaVaiTro = $($r['MaVaiTro']))"
    }
    [void]$sb.AppendLine(($rowsVT -join "`nUNION ALL`n") + ";")
    [void]$sb.AppendLine("PRINT N'  [OK] Da phan quyen vai tro cho cac tai khoan lich su.';")
}
[void]$sb.AppendLine("")

# 3. NguoiDung_ChuyenMon
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
[void]$sb.AppendLine("-- 3. NGUOI DUNG - CHUYEN MON")
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
$cmd.CommandText = "SELECT MaNguoiDung, MaChuyenNganh, LaChuyenMonChinh, GhiChu FROM NguoiDung_ChuyenMon WHERE MaNguoiDung >= 27 ORDER BY MaNguoiDung, MaChuyenNganh"
$dtCM = New-Object System.Data.DataTable
(New-Object System.Data.SqlClient.SqlDataAdapter($cmd)).Fill($dtCM) | Out-Null
if ($dtCM.Rows.Count -gt 0) {
    [void]$sb.AppendLine("INSERT INTO NguoiDung_ChuyenMon (MaNguoiDung, MaChuyenNganh, LaChuyenMonChinh, GhiChu)")
    $rowsCM = @()
    foreach ($r in $dtCM.Rows) {
        $lcm = if ([bool]$r['LaChuyenMonChinh']) { 1 } else { 0 }
        $gc = Escape-Sql $r['GhiChu']
        $rowsCM += "SELECT $($r['MaNguoiDung']), $($r['MaChuyenNganh']), $lcm, $gc WHERE NOT EXISTS (SELECT 1 FROM NguoiDung_ChuyenMon WHERE MaNguoiDung = $($r['MaNguoiDung']) AND MaChuyenNganh = $($r['MaChuyenNganh']))"
    }
    [void]$sb.AppendLine(($rowsCM -join "`nUNION ALL`n") + ";")
    [void]$sb.AppendLine("PRINT N'  [OK] Da dong bo ho so chuyen mon cho tai khoan lich su.';")
}
[void]$sb.AppendLine("")

# 4. SoTapChi (MaSoTapChi >= 4)
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
[void]$sb.AppendLine("-- 4. SO TAP CHI (23 SO PHAT HANH 2016 - 2026)")
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
$cmd.CommandText = "SELECT * FROM SoTapChi WHERE MaSoTapChi >= 4 ORDER BY MaSoTapChi"
$dtSTC = New-Object System.Data.DataTable
(New-Object System.Data.SqlClient.SqlDataAdapter($cmd)).Fill($dtSTC) | Out-Null
$colsSTC = @($dtSTC.Columns | ForEach-Object { $_.ColumnName })
$colListSTC = ($colsSTC | ForEach-Object { "[$_]" }) -join ", "

[void]$sb.AppendLine("IF OBJECT_ID('tempdb..#TmpSoTapChi') IS NOT NULL DROP TABLE #TmpSoTapChi;")
[void]$sb.AppendLine("CREATE TABLE #TmpSoTapChi (")
$createColsSTC = @()
foreach ($c in $dtSTC.Columns) {
    $typeName = $c.DataType.Name
    $sqlType = switch ($typeName) {
        "Int32" { "INT" }
        "DateTime" { "DATETIME" }
        default { "NVARCHAR(500)" }
    }
    $createColsSTC += "  [$($c.ColumnName)] $sqlType"
}
[void]$sb.AppendLine(($createColsSTC -join ",`n"))
[void]$sb.AppendLine(");")
[void]$sb.AppendLine("")

$batchSTC = @()
foreach ($row in $dtSTC.Rows) {
    $vals = @()
    foreach ($c in $dtSTC.Columns) {
        $vals += Format-SqlVal $row[$c.ColumnName] $c.DataType.Name
    }
    $batchSTC += "(" + ($vals -join ", ") + ")"
}
[void]$sb.AppendLine("INSERT INTO #TmpSoTapChi ($colListSTC) VALUES")
[void]$sb.AppendLine(($batchSTC -join ",`n") + ";")
[void]$sb.AppendLine("SET IDENTITY_INSERT SoTapChi ON;")
[void]$sb.AppendLine("INSERT INTO SoTapChi ($colListSTC)")
[void]$sb.AppendLine("SELECT $colListSTC FROM #TmpSoTapChi t")
[void]$sb.AppendLine("WHERE NOT EXISTS (SELECT 1 FROM SoTapChi s WHERE s.MaSoTapChi = t.MaSoTapChi);")
[void]$sb.AppendLine("SET IDENTITY_INSERT SoTapChi OFF;")
[void]$sb.AppendLine("DROP TABLE #TmpSoTapChi;")
[void]$sb.AppendLine("PRINT N'  [OK] Da dong bo ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' so tap chi xuat ban lich su.';")
[void]$sb.AppendLine("")

# 5. BaiBao (MaBaiBao >= 19)
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
[void]$sb.AppendLine("-- 5. BAI BAO KHOA HOC (230 BAI BAO DA XUAT BAN)")
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
$cmd.CommandText = "SELECT * FROM BaiBao WHERE MaBaiBao >= 19 ORDER BY MaBaiBao"
$dtBB = New-Object System.Data.DataTable
(New-Object System.Data.SqlClient.SqlDataAdapter($cmd)).Fill($dtBB) | Out-Null
$colsBB = @($dtBB.Columns | ForEach-Object { $_.ColumnName })
$colListBB = ($colsBB | ForEach-Object { "[$_]" }) -join ", "

[void]$sb.AppendLine("IF OBJECT_ID('tempdb..#TmpBaiBao') IS NOT NULL DROP TABLE #TmpBaiBao;")
[void]$sb.AppendLine("CREATE TABLE #TmpBaiBao (")
$createColsBB = @()
foreach ($c in $dtBB.Columns) {
    $typeName = $c.DataType.Name
    $sqlType = switch ($typeName) {
        "Int32" { "INT" }
        "DateTime" { "DATETIME" }
        default { "NVARCHAR(MAX)" }
    }
    $createColsBB += "  [$($c.ColumnName)] $sqlType"
}
[void]$sb.AppendLine(($createColsBB -join ",`n"))
[void]$sb.AppendLine(");")
[void]$sb.AppendLine("")

for ($i = 0; $i -lt $dtBB.Rows.Count; $i += $batchSize) {
    $batch = @()
    for ($j = $i; $j -lt [Math]::Min($i + $batchSize, $dtBB.Rows.Count); $j++) {
        $row = $dtBB.Rows[$j]
        $vals = @()
        foreach ($c in $dtBB.Columns) {
            $vals += Format-SqlVal $row[$c.ColumnName] $c.DataType.Name
        }
        $batch += "(" + ($vals -join ", ") + ")"
    }
    [void]$sb.AppendLine("INSERT INTO #TmpBaiBao ($colListBB) VALUES")
    [void]$sb.AppendLine(($batch -join ",`n") + ";")
}

[void]$sb.AppendLine("SET IDENTITY_INSERT BaiBao ON;")
[void]$sb.AppendLine("INSERT INTO BaiBao ($colListBB)")
[void]$sb.AppendLine("SELECT $colListBB FROM #TmpBaiBao t")
[void]$sb.AppendLine("WHERE NOT EXISTS (SELECT 1 FROM BaiBao b WHERE b.MaBaiBao = t.MaBaiBao);")
[void]$sb.AppendLine("SET IDENTITY_INSERT BaiBao OFF;")
[void]$sb.AppendLine("DROP TABLE #TmpBaiBao;")
[void]$sb.AppendLine("PRINT N'  [OK] Da dong bo ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bai bao khoa hoc da xuat ban.';")
[void]$sb.AppendLine("")

# 6. DongTacGia
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
[void]$sb.AppendLine("-- 6. DONG TAC GIA (215 DONG TAC GIA BAI BAO LICH SU)")
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
$cmd.CommandText = "SELECT * FROM DongTacGia WHERE MaBaiBao >= 19 ORDER BY MaDongTacGia"
$dtDTG = New-Object System.Data.DataTable
(New-Object System.Data.SqlClient.SqlDataAdapter($cmd)).Fill($dtDTG) | Out-Null
$colsDTG = @($dtDTG.Columns | ForEach-Object { $_.ColumnName })
$colListDTG = ($colsDTG | ForEach-Object { "[$_]" }) -join ", "

[void]$sb.AppendLine("IF OBJECT_ID('tempdb..#TmpDongTacGia') IS NOT NULL DROP TABLE #TmpDongTacGia;")
[void]$sb.AppendLine("CREATE TABLE #TmpDongTacGia (")
$createColsDTG = @()
foreach ($c in $dtDTG.Columns) {
    $typeName = $c.DataType.Name
    $sqlType = switch ($typeName) {
        "Int32" { "INT" }
        "Boolean" { "BIT" }
        default { "NVARCHAR(MAX)" }
    }
    $createColsDTG += "  [$($c.ColumnName)] $sqlType"
}
[void]$sb.AppendLine(($createColsDTG -join ",`n"))
[void]$sb.AppendLine(");")
[void]$sb.AppendLine("")

for ($i = 0; $i -lt $dtDTG.Rows.Count; $i += $batchSize) {
    $batch = @()
    for ($j = $i; $j -lt [Math]::Min($i + $batchSize, $dtDTG.Rows.Count); $j++) {
        $row = $dtDTG.Rows[$j]
        $vals = @()
        foreach ($c in $dtDTG.Columns) {
            $vals += Format-SqlVal $row[$c.ColumnName] $c.DataType.Name
        }
        $batch += "(" + ($vals -join ", ") + ")"
    }
    [void]$sb.AppendLine("INSERT INTO #TmpDongTacGia ($colListDTG) VALUES")
    [void]$sb.AppendLine(($batch -join ",`n") + ";")
}

[void]$sb.AppendLine("SET IDENTITY_INSERT DongTacGia ON;")
[void]$sb.AppendLine("INSERT INTO DongTacGia ($colListDTG)")
[void]$sb.AppendLine("SELECT $colListDTG FROM #TmpDongTacGia t")
[void]$sb.AppendLine("WHERE NOT EXISTS (SELECT 1 FROM DongTacGia d WHERE d.MaDongTacGia = t.MaDongTacGia);")
[void]$sb.AppendLine("SET IDENTITY_INSERT DongTacGia OFF;")
[void]$sb.AppendLine("DROP TABLE #TmpDongTacGia;")
[void]$sb.AppendLine("PRINT N'  [OK] Da dong bo ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' ban ghi dong tac gia.';")
[void]$sb.AppendLine("")

# 7. ThuMucBaiBao (MaThuMuc >= 26)
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
[void]$sb.AppendLine("-- 7. THU MUC BAI BAO (230 TAP TIN PDF THANH PHAM XUAT BAN)")
[void]$sb.AppendLine("-- -------------------------------------------------------------------------")
$cmd.CommandText = "SELECT * FROM ThuMucBaiBao WHERE MaThuMuc >= 26 ORDER BY MaThuMuc"
$dtTMBBTable = New-Object System.Data.DataTable
(New-Object System.Data.SqlClient.SqlDataAdapter($cmd)).Fill($dtTMBBTable) | Out-Null
$colsTMBB = @($dtTMBBTable.Columns | ForEach-Object { $_.ColumnName })
$colListTMBB = ($colsTMBB | ForEach-Object { "[$_]" }) -join ", "

[void]$sb.AppendLine("IF OBJECT_ID('tempdb..#TmpThuMucBaiBao') IS NOT NULL DROP TABLE #TmpThuMucBaiBao;")
[void]$sb.AppendLine("CREATE TABLE #TmpThuMucBaiBao (")
$createColsTMBB = @()
foreach ($c in $dtTMBBTable.Columns) {
    $typeName = $c.DataType.Name
    $sqlType = switch ($typeName) {
        "Int32" { "INT" }
        "Int64" { "BIGINT" }
        "DateTime" { "DATETIME" }
        default { "NVARCHAR(MAX)" }
    }
    $createColsTMBB += "  [$($c.ColumnName)] $sqlType"
}
[void]$sb.AppendLine(($createColsTMBB -join ",`n"))
[void]$sb.AppendLine(");")
[void]$sb.AppendLine("")

for ($i = 0; $i -lt $dtTMBBTable.Rows.Count; $i += $batchSize) {
    $batch = @()
    for ($j = $i; $j -lt [Math]::Min($i + $batchSize, $dtTMBBTable.Rows.Count); $j++) {
        $row = $dtTMBBTable.Rows[$j]
        $vals = @()
        foreach ($c in $dtTMBBTable.Columns) {
            $vals += Format-SqlVal $row[$c.ColumnName] $c.DataType.Name
        }
        $batch += "(" + ($vals -join ", ") + ")"
    }
    [void]$sb.AppendLine("INSERT INTO #TmpThuMucBaiBao ($colListTMBB) VALUES")
    [void]$sb.AppendLine(($batch -join ",`n") + ";")
}

[void]$sb.AppendLine("SET IDENTITY_INSERT ThuMucBaiBao ON;")
[void]$sb.AppendLine("INSERT INTO ThuMucBaiBao ($colListTMBB)")
[void]$sb.AppendLine("SELECT $colListTMBB FROM #TmpThuMucBaiBao t")
[void]$sb.AppendLine("WHERE NOT EXISTS (SELECT 1 FROM ThuMucBaiBao m WHERE m.MaThuMuc = t.MaThuMuc);")
[void]$sb.AppendLine("SET IDENTITY_INSERT ThuMucBaiBao OFF;")
[void]$sb.AppendLine("DROP TABLE #TmpThuMucBaiBao;")
[void]$sb.AppendLine("PRINT N'  [OK] Da dong bo ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' ban ghi tep PDF xuat ban thanh pham.';")
[void]$sb.AppendLine("")

[void]$sb.AppendLine("PRINT N'=========================================================================';")
[void]$sb.AppendLine("PRINT N'HOAN TAT MIGRATION DU LIEU LICH SU XUAT BAN THANH CONG!';")
[void]$sb.AppendLine("PRINT N'=========================================================================';")

$conn.Close()

[System.IO.File]::WriteAllText($OutputFile, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "Migration SQL successfully generated: $OutputFile ($([math]::Round($sb.Length / 1024, 2)) KB)"
