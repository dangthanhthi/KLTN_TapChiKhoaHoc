-- =========================================================================
-- KỊCH BẢN MIGRATION: HỆ THỐNG XÁC NHẬN EMAIL ĐĂNG KÝ TÀI KHOẢN HUIT
-- Ngày lập: 27/09/2026
-- Mô tả: Tạo các bảng DangKyChoXacNhan, MaXacNhanEmail, EmailOutbox
-- =========================================================================

-- 1. BẢNG HỒ SƠ ĐĂNG KÝ CHỜ XÁC NHẬN
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DangKyChoXacNhan')
BEGIN
    CREATE TABLE DangKyChoXacNhan (
        MaDangKy UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DangKyChoXacNhan PRIMARY KEY DEFAULT NEWID(),
        EmailGoc NVARCHAR(150) NOT NULL,
        EmailSoSanh VARCHAR(150) NOT NULL,
        TenDangNhapSoSanh VARCHAR(50) NOT NULL,
        MatKhauHash VARCHAR(100) NOT NULL,
        HoDem NVARCHAR(50) NULL,
        Ten NVARCHAR(30) NOT NULL,
        HoTen NVARCHAR(100) NOT NULL,
        HocVi NVARCHAR(30) NOT NULL CONSTRAINT DF_DangKyChoXacNhan_HocVi DEFAULT N'Không',
        HocHam NVARCHAR(30) NOT NULL CONSTRAINT DF_DangKyChoXacNhan_HocHam DEFAULT N'Không',
        GioiTinh NVARCHAR(10) NOT NULL CONSTRAINT DF_DangKyChoXacNhan_GioiTinh DEFAULT N'Nam',
        QuocGia NVARCHAR(50) NOT NULL CONSTRAINT DF_DangKyChoXacNhan_QuocGia DEFAULT 'Vietnam',
        NgonNgu NVARCHAR(30) NOT NULL CONSTRAINT DF_DangKyChoXacNhan_NgonNgu DEFAULT N'Tiếng Việt',
        SoDienThoai VARCHAR(20) NULL,
        DonVi NVARCHAR(255) NOT NULL,
        DiaChi NVARCHAR(255) NULL,
        SoTaiKhoan VARCHAR(30) NULL,
        ChuTaiKhoan NVARCHAR(100) NULL,
        NganHang NVARCHAR(100) NULL,
        MaORCID VARCHAR(30) NULL,
        ChuyenNganhId INT NULL,
        DangKyPhanBien BIT NOT NULL CONSTRAINT DF_DangKyChoXacNhan_DangKyPhanBien DEFAULT 0,
        TaoLucUtc DATETIME2 NOT NULL CONSTRAINT DF_DangKyChoXacNhan_TaoLucUtc DEFAULT SYSUTCDATETIME(),
        HetHanHoSoUtc DATETIME2 NOT NULL,
        TrangThai VARCHAR(20) NOT NULL CONSTRAINT DF_DangKyChoXacNhan_TrangThai DEFAULT 'Pending',
        RowVersion ROWVERSION NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_DangKyChoXacNhan_EmailSoSanh ON DangKyChoXacNhan(EmailSoSanh);
    CREATE NONCLUSTERED INDEX IX_DangKyChoXacNhan_TenDangNhapSoSanh ON DangKyChoXacNhan(TenDangNhapSoSanh);
    CREATE NONCLUSTERED INDEX IX_DangKyChoXacNhan_TrangThai_HetHan ON DangKyChoXacNhan(TrangThai, HetHanHoSoUtc);
    PRINT 'Da tao bang DangKyChoXacNhan thanh cong.';
END
GO

-- 2. BẢNG MÃ XÁC NHẬN EMAIL (OTP 6 SỐ BĂM HMAC-SHA256)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MaXacNhanEmail')
BEGIN
    CREATE TABLE MaXacNhanEmail (
        MaId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MaXacNhanEmail PRIMARY KEY DEFAULT NEWID(),
        MaDangKy UNIQUEIDENTIFIER NOT NULL,
        MaHash VARCHAR(64) NOT NULL,
        TaoLucUtc DATETIME2 NOT NULL CONSTRAINT DF_MaXacNhanEmail_TaoLucUtc DEFAULT SYSUTCDATETIME(),
        HetHanUtc DATETIME2 NOT NULL,
        SoLanNhapSai INT NOT NULL CONSTRAINT DF_MaXacNhanEmail_SoLanNhapSai DEFAULT 0,
        DaDungLucUtc DATETIME2 NULL,
        LanGuiCuoiUtc DATETIME2 NOT NULL CONSTRAINT DF_MaXacNhanEmail_LanGuiCuoiUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_MaXacNhanEmail_DangKyChoXacNhan FOREIGN KEY (MaDangKy) REFERENCES DangKyChoXacNhan(MaDangKy) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_MaXacNhanEmail_MaDangKy ON MaXacNhanEmail(MaDangKy);
    PRINT 'Da tao bang MaXacNhanEmail thanh cong.';
END
GO

-- 3. BẢNG HÀNG ĐỢI EMAIL OUTBOX (TRANSACTIONAL OUTBOX PATTERN)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailOutbox')
BEGIN
    CREATE TABLE EmailOutbox (
        MaOutbox UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EmailOutbox PRIMARY KEY DEFAULT NEWID(),
        LoaiThu VARCHAR(50) NOT NULL,
        NguoiNhan VARCHAR(150) NOT NULL,
        TieuDe NVARCHAR(255) NOT NULL,
        NoiDungHtml NVARCHAR(MAX) NULL,
        NoiDungText NVARCHAR(MAX) NULL,
        TrangThai VARCHAR(20) NOT NULL CONSTRAINT DF_EmailOutbox_TrangThai DEFAULT 'Pending',
        SoLanThuLai INT NOT NULL CONSTRAINT DF_EmailOutbox_SoLanThuLai DEFAULT 0,
        LoiKyThuat NVARCHAR(1000) NULL,
        TaoLucUtc DATETIME2 NOT NULL CONSTRAINT DF_EmailOutbox_TaoLucUtc DEFAULT SYSUTCDATETIME(),
        GuiLucUtc DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX IX_EmailOutbox_TrangThai_TaoLuc ON EmailOutbox(TrangThai, TaoLucUtc);
    PRINT 'Da tao bang EmailOutbox thanh cong.';
END
GO
