-- Chạy trên từng CSDL hiện có trước khi triển khai API mới.
-- Ví dụ: sqlcmd -S . -d QL_TapChiKhoaHoc -E -b -f 65001 -i HoTro/Migration_20260925_AuthorVisibleNote.sql
IF OBJECT_ID(N'dbo.LichSuTrangThaiBaiBao', N'U') IS NULL
    THROW 50001, N'Không tìm thấy bảng LichSuTrangThaiBaiBao.', 1;

IF COL_LENGTH(N'dbo.LichSuTrangThaiBaiBao', N'ThongBaoChoTacGia') IS NULL
    ALTER TABLE dbo.LichSuTrangThaiBaiBao
        ADD ThongBaoChoTacGia NVARCHAR(2000) NULL;
