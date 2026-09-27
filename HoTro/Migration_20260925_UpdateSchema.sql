-- =====================================================================
-- KỊCH BẢN MIGRATION ĐỒNG BỘ CSDL QL_TapChiKhoaHoc
-- Ngày: 25/09/2026
-- Nội dung:
--   1. Cập nhật CHECK constraint CHK_ThuMuc_LoaiThuMuc (thêm PDF thành phẩm, PDF Xuất bản, Bản thảo ẩn danh)
--   2. Bổ sung bảng DonDangKyPhanBien (Lưu trữ và quản lý đơn đăng ký phản biện chờ Ban biên tập phê duyệt)
-- =====================================================================

USE QL_TapChiKhoaHoc;
GO

PRINT N'-> Bắt đầu thực thi Migration 20260925_UpdateSchema...';
GO

-- 1. CẬP NHẬT RÀNG BUỘC CHK_ThuMuc_LoaiThuMuc
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_ThuMuc_LoaiThuMuc')
BEGIN
    PRINT N'  + Đang gỡ bỏ ràng buộc cũ CHK_ThuMuc_LoaiThuMuc...';
    ALTER TABLE ThuMucBaiBao DROP CONSTRAINT CHK_ThuMuc_LoaiThuMuc;
END
GO

PRINT N'  + Đang tái tạo ràng buộc CHK_ThuMuc_LoaiThuMuc đầy đủ các loại tệp...';
ALTER TABLE ThuMucBaiBao ADD CONSTRAINT CHK_ThuMuc_LoaiThuMuc CHECK (LoaiThuMuc IN (
    N'Bản thảo gốc', 
    N'File ẩn danh', 
    N'Bản thảo ẩn danh',
    N'Bản chỉnh sửa', 
    N'Phụ lục',
    N'Bản giải trình BM-03',
    N'Bản đánh dấu sửa đổi',
    N'PDF thành phẩm',
    N'PDF Xuất bản'
));
GO

-- 2. TẠO BẢNG DonDangKyPhanBien NẾU CHƯA CÓ
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DonDangKyPhanBien')
BEGIN
    PRINT N'  + Đang tạo bảng DonDangKyPhanBien...';
    CREATE TABLE DonDangKyPhanBien (
        MaDon INT IDENTITY(1,1) PRIMARY KEY,
        MaNguoiDung INT NOT NULL,
        NgayDangKy DATETIME NOT NULL DEFAULT GETDATE(),
        GhiChu NVARCHAR(500) NULL,
        TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
        MaNguoiDuyet INT NULL,
        NgayDuyet DATETIME NULL,
        LyDoTuChoi NVARCHAR(500) NULL,
        CONSTRAINT FK_DonDangKyPhanBien_NguoiDung FOREIGN KEY (MaNguoiDung) 
            REFERENCES NguoiDung(MaNguoiDung) ON DELETE CASCADE,
        CONSTRAINT FK_DonDangKyPhanBien_NguoiDuyet FOREIGN KEY (MaNguoiDuyet) 
            REFERENCES NguoiDung(MaNguoiDung),
        CONSTRAINT CHK_DonDangKy_TrangThai CHECK (TrangThai IN (N'Chờ duyệt', N'Đã duyệt', N'Từ chối'))
    );
    PRINT N'  [✓] Tạo bảng DonDangKyPhanBien thành công!';
END
ELSE
BEGIN
    PRINT N'  [*] Bảng DonDangKyPhanBien đã tồn tại.';
END
GO

PRINT N'[✓] HOÀN TẤT MIGRATION SCHEMA!';
GO
