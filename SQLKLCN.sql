USE master;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'QL_TapChiKhoaHoc')
BEGIN
    ALTER DATABASE QL_TapChiKhoaHoc SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QL_TapChiKhoaHoc;
END
GO

CREATE DATABASE QL_TapChiKhoaHoc;
GO

USE QL_TapChiKhoaHoc;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ==========================================
-- 1. BẢNG DANH MỤC & PHÂN QUYỀN HỆ THỐNG
-- ==========================================

-- 1. Bảng VaiTro
CREATE TABLE VaiTro (
    MaVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(100) NOT NULL UNIQUE,
    MoTa NVARCHAR(500) NULL
);
GO

-- 2. Bảng NguoiDung (Bổ sung đầy đủ các trường học thuật, cá nhân và tài khoản)
CREATE TABLE NguoiDung (
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap VARCHAR(100) NULL UNIQUE,
    HoDem NVARCHAR(100) NULL,
    Ten NVARCHAR(50) NULL,
    HoTen NVARCHAR(150) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    MatKhau VARCHAR(255) NOT NULL,
    HocVi NVARCHAR(50) NOT NULL DEFAULT N'Không',
    HocHam NVARCHAR(50) NOT NULL DEFAULT N'Không',
    GioiTinh NVARCHAR(10) NOT NULL DEFAULT N'Nam',
    NgonNgu NVARCHAR(50) NOT NULL DEFAULT N'Tiếng Việt',
    QuocGia NVARCHAR(100) NOT NULL DEFAULT N'Vietnam',
    SoDienThoai VARCHAR(20) NULL,
    DonVi NVARCHAR(255) NULL,
    DiaChi NVARCHAR(255) NULL,
    SoTaiKhoan VARCHAR(50) NULL,
    ChuTaiKhoan NVARCHAR(150) NULL,
    NganHang NVARCHAR(150) NULL,
    MaORCID VARCHAR(50) NULL,
    AnhDaiDien NVARCHAR(500) NULL,
    TrangThai BIT NOT NULL DEFAULT 1,
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CHK_NguoiDung_HocVi CHECK (HocVi IN (N'Không', N'Cử nhân', N'Thạc sĩ', N'Tiến sĩ', N'TSKH')),
    CONSTRAINT CHK_NguoiDung_HocHam CHECK (HocHam IN (N'Không', N'Phó giáo sư', N'Giáo sư')),
    CONSTRAINT CHK_NguoiDung_GioiTinh CHECK (GioiTinh IN (N'Nam', N'Nữ', N'Khác'))
);
GO

-- Filtered Unique Index cho MaORCID (cho phép nhiều NULL hoặc rỗng, nhưng có giá trị thì phải duy nhất)
CREATE UNIQUE INDEX UQ_NguoiDung_ORCID ON NguoiDung(MaORCID) WHERE MaORCID IS NOT NULL AND MaORCID <> '';
GO

-- 3. Bảng NguoiDung_VaiTro (Bảng trung gian n - n giữa Người dùng và Vai trò)
CREATE TABLE NguoiDung_VaiTro (
    MaNguoiDung INT NOT NULL,
    MaVaiTro INT NOT NULL,
    PRIMARY KEY (MaNguoiDung, MaVaiTro),
    CONSTRAINT FK_NguoiDung_VaiTro_NguoiDung FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDung(MaNguoiDung) ON DELETE CASCADE,
    CONSTRAINT FK_NguoiDung_VaiTro_VaiTro FOREIGN KEY (MaVaiTro) 
        REFERENCES VaiTro(MaVaiTro) ON DELETE CASCADE
);
GO

-- 4. Bảng ChuyenNganh
CREATE TABLE ChuyenNganh (
    MaChuyenNganh INT IDENTITY(1,1) PRIMARY KEY,
    TenChuyenNganh NVARCHAR(255) NOT NULL UNIQUE,
    MoTa NVARCHAR(500) NULL
);
GO

-- 4.1 Bảng NguoiDung_ChuyenMon (Bảng nối Người dùng / Tác giả / Phản biện với Chuyên ngành chuyên môn)
CREATE TABLE NguoiDung_ChuyenMon (
    MaNguoiDung INT NOT NULL,
    MaChuyenNganh INT NOT NULL,
    LaChuyenMonChinh BIT NOT NULL DEFAULT 1,
    GhiChu NVARCHAR(255) NULL,
    NgayDangKy DATETIME NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (MaNguoiDung, MaChuyenNganh),
    CONSTRAINT FK_NguoiDung_ChuyenMon_NguoiDung FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDung(MaNguoiDung) ON DELETE CASCADE,
    CONSTRAINT FK_NguoiDung_ChuyenMon_ChuyenNganh FOREIGN KEY (MaChuyenNganh) 
        REFERENCES ChuyenNganh(MaChuyenNganh) ON DELETE CASCADE
);
GO

-- 5. Bảng SoTapChi
CREATE TABLE SoTapChi (
    MaSoTapChi INT IDENTITY(1,1) PRIMARY KEY,
    TenSo NVARCHAR(255) NOT NULL,
    Tap INT NOT NULL,
    So INT NOT NULL,
    Nam INT NOT NULL,
    NgayPhatHanh DATETIME NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Đang biên tập',
    CONSTRAINT UQ_SoTapChi_Tap_So_Nam UNIQUE (Tap, So, Nam),
    CONSTRAINT CHK_SoTapChi_TrangThai CHECK (TrangThai IN (N'Đang biên tập', N'Đã xuất bản', N'Đã đóng'))
);
GO

-- ==========================================
-- 2. BẢNG BÀI BÁO & QUẢN LÝ XUẤT BẢN
-- ==========================================

-- 6. Bảng BaiBao (Quan hệ 1 - n với SoTapChi theo phản biện 1 & 4)
CREATE TABLE BaiBao (
    MaBaiBao INT IDENTITY(1,1) PRIMARY KEY,
    TieuDe NVARCHAR(500) NOT NULL,
    TieuDeTiengAnh NVARCHAR(500) NULL,
    TomTat NVARCHAR(MAX) NULL,
    TomTatTiengAnh NVARCHAR(MAX) NULL,
    TuKhoa NVARCHAR(255) NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ sơ duyệt',
    MaDOI VARCHAR(100) NULL,
    NgayGui DATETIME NOT NULL DEFAULT GETDATE(),
    NgayCapNhat DATETIME NOT NULL DEFAULT GETDATE(),
    MaNguoiDung INT NOT NULL,     -- Tác giả gửi bài
    MaChuyenNganh INT NOT NULL,   -- Chuyên ngành
    MaSoTapChi INT NULL,          -- Gán trực tiếp vào Số tạp chí (NULL nếu chưa xuất bản)
    TrangBatDau INT NULL,
    TrangKetThuc INT NULL,
    CONSTRAINT FK_BaiBao_NguoiDung FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT FK_BaiBao_ChuyenNganh FOREIGN KEY (MaChuyenNganh) 
        REFERENCES ChuyenNganh(MaChuyenNganh),
    CONSTRAINT FK_BaiBao_SoTapChi FOREIGN KEY (MaSoTapChi) 
        REFERENCES SoTapChi(MaSoTapChi) ON DELETE SET NULL,
    CONSTRAINT CHK_BaiBao_Trang CHECK (
        (TrangBatDau IS NULL AND TrangKetThuc IS NULL) OR 
        (TrangBatDau IS NOT NULL AND TrangKetThuc IS NOT NULL AND TrangKetThuc >= TrangBatDau AND TrangBatDau > 0)
    ),
    CONSTRAINT CHK_BaiBao_TrangThai CHECK (TrangThai IN (
        N'Chờ sơ duyệt', 
        N'Chờ sửa hình thức', 
        N'Đang phản biện', 
        N'Chờ chỉnh sửa', 
        N'Chờ quyết định', 
        N'Đã chấp nhận', 
        N'Đang chế bản', 
        N'Sẵn sàng xuất bản', 
        N'Đã xuất bản', 
        N'Từ chối'
    ))
);
GO

-- 7. Filtered Unique Index cho MaDOI (Phản biện 5: cho phép nhiều NULL, nhưng có giá trị thì phải duy nhất)
CREATE UNIQUE INDEX UQ_BaiBao_DOI ON BaiBao(MaDOI) WHERE MaDOI IS NOT NULL;
GO

-- 8. Bảng DongTacGia (Lưu snapshot tác giả tại thời điểm nộp; hỗ trợ tác giả khách và liên kết tài khoản hệ thống)
CREATE TABLE DongTacGia (
    MaDongTacGia INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    DonVi NVARCHAR(255) NULL,
    MaORCID VARCHAR(50) NULL,
    LaTacGiaLienHe BIT NOT NULL DEFAULT 0,
    ThuTu INT NOT NULL DEFAULT 1,
    MaBaiBao INT NOT NULL,
    MaNguoiDung INT NULL,         -- Tùy chọn: NULL nếu đồng tác giả khách chưa có tài khoản
    CONSTRAINT FK_DongTacGia_BaiBao FOREIGN KEY (MaBaiBao) 
        REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE,
    CONSTRAINT FK_DongTacGia_NguoiDung FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDung(MaNguoiDung) ON DELETE SET NULL,
    CONSTRAINT UQ_DongTacGia_BaiBao_ThuTu UNIQUE (MaBaiBao, ThuTu),
    CONSTRAINT UQ_DongTacGia_BaiBao_Email UNIQUE (MaBaiBao, Email),
    CONSTRAINT CHK_DongTacGia_ThuTu CHECK (ThuTu > 0)
);
GO

-- 9. Bảng ThuMucBaiBao (Bổ sung SoVong và CHECK ràng buộc loại thư mục tài liệu)
CREATE TABLE ThuMucBaiBao (
    MaThuMuc INT IDENTITY(1,1) PRIMARY KEY,
    TenThuMuc NVARCHAR(255) NOT NULL,
    DuongDan NVARCHAR(500) NOT NULL,
    LoaiThuMuc NVARCHAR(50) NOT NULL,
    KichThuoc BIGINT NOT NULL,
    SoVong INT NOT NULL DEFAULT 1,    -- Vòng nộp bản thảo (Vòng 1, Vòng 2...)
    NgayTaiLen DATETIME NOT NULL DEFAULT GETDATE(),
    MaBaiBao INT NOT NULL,
    CONSTRAINT FK_ThuMucBaiBao_BaiBao FOREIGN KEY (MaBaiBao) 
        REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE,
    CONSTRAINT CHK_ThuMuc_LoaiThuMuc CHECK (LoaiThuMuc IN (
        N'Bản thảo gốc', 
        N'File ẩn danh', 
        N'Bản chỉnh sửa', 
        N'Phụ lục',
        N'Bản giải trình BM-03',
        N'Bản đánh dấu sửa đổi'
    ))
);
GO

-- 9.1 Bảng PhanBienDeXuat (Lưu danh sách chuyên gia phản biện do tác giả đề xuất lúc nộp bài)
CREATE TABLE PhanBienDeXuat (
    MaDeXuat INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(150) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    DonVi NVARCHAR(255) NULL,
    LinhVuc NVARCHAR(255) NULL,
    LaChuyenGiaHeThong BIT NOT NULL DEFAULT 0,
    MaBaiBao INT NOT NULL,
    MaNguoiDung INT NULL,
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_PhanBienDeXuat_BaiBao FOREIGN KEY (MaBaiBao) 
        REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE,
    CONSTRAINT FK_PhanBienDeXuat_NguoiDung FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDung(MaNguoiDung) ON DELETE SET NULL
);
GO

-- ==========================================
-- 3. BẢNG PHẢN BIỆN, ĐÁNH GIÁ & LỊCH SỬ TRẠNG THÁI
-- ==========================================

-- 10. Bảng PhanCongPhanBien (Khắc phục lỗi chặn giờ:phút:giây với kiểu DATE)
CREATE TABLE PhanCongPhanBien (
    MaPhanCong INT IDENTITY(1,1) PRIMARY KEY,
    SoVong INT NOT NULL DEFAULT 1,    -- Vòng phản biện tương ứng (Vòng 1, Vòng 2...)
    NgayPhanCong DATETIME NOT NULL DEFAULT GETDATE(),
    HanPhanHoi DATE NULL,             -- Hạn chót theo ngày
    HanHoanThanh DATE NULL,           -- Hạn chót theo ngày
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ phản hồi',
    MaBaiBao INT NOT NULL,
    MaNguoiDung INT NOT NULL,         -- Chuyên gia phản biện
    CONSTRAINT FK_PhanCongPhanBien_BaiBao FOREIGN KEY (MaBaiBao) 
        REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE,
    CONSTRAINT FK_PhanCongPhanBien_NguoiDung FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT UQ_PhanCong_BaiBao_NguoiDung_Vong UNIQUE (MaBaiBao, MaNguoiDung, SoVong),
    CONSTRAINT CHK_PhanCong_ThoiGian CHECK (
        (HanPhanHoi IS NULL OR HanPhanHoi >= CAST(NgayPhanCong AS DATE)) AND
        (HanHoanThanh IS NULL OR HanHoanThanh >= CAST(NgayPhanCong AS DATE)) AND
        (HanPhanHoi IS NULL OR HanHoanThanh IS NULL OR HanHoanThanh >= HanPhanHoi)
    ),
    CONSTRAINT CHK_PhanCong_TrangThai CHECK (TrangThai IN (
        N'Chờ phản hồi', 
        N'Đồng ý phản biện', 
        N'Đang đánh giá', 
        N'Đã đánh giá', 
        N'Từ chối phản biện', 
        N'Quá hạn'
    ))
);
GO

-- 11. Bảng PhieuDanhGia (Dùng DECIMAL(3,1) và CHECK điểm 0 - 10)
CREATE TABLE PhieuDanhGia (
    MaPhieu INT IDENTITY(1,1) PRIMARY KEY,
    DiemTinhMoi DECIMAL(3,1) NULL,
    DiemPhuongPhap DECIMAL(3,1) NULL,
    DiemKetQua DECIMAL(3,1) NULL,
    DiemTrinhBay DECIMAL(3,1) NULL,
    DiemTongKet DECIMAL(3,1) NULL,
    NhanXetChoTacGia NVARCHAR(MAX) NULL,
    NhanXetBaoMat NVARCHAR(MAX) NULL,
    KienNghi NVARCHAR(255) NULL,
    NgayDanhGia DATETIME NOT NULL DEFAULT GETDATE(),
    MaPhanCong INT NOT NULL UNIQUE, 
    CONSTRAINT FK_PhieuDanhGia_PhanCongPhanBien FOREIGN KEY (MaPhanCong) 
        REFERENCES PhanCongPhanBien(MaPhanCong) ON DELETE CASCADE,
    CONSTRAINT CHK_PhieuDanhGia_KienNghi CHECK (KienNghi IN (
        N'Chấp nhận đăng', 
        N'Chỉnh sửa nhỏ', 
        N'Chỉnh sửa lớn và phản biện lại', 
        N'Từ chối đăng'
    )),
    CONSTRAINT CHK_PhieuDanhGia_Diem CHECK (
        (DiemTinhMoi IS NULL OR (DiemTinhMoi >= 0 AND DiemTinhMoi <= 10)) AND
        (DiemPhuongPhap IS NULL OR (DiemPhuongPhap >= 0 AND DiemPhuongPhap <= 10)) AND
        (DiemKetQua IS NULL OR (DiemKetQua >= 0 AND DiemKetQua <= 10)) AND
        (DiemTrinhBay IS NULL OR (DiemTrinhBay >= 0 AND DiemTrinhBay <= 10)) AND
        (DiemTongKet IS NULL OR (DiemTongKet >= 0 AND DiemTongKet <= 10))
    )
);
GO

-- 12. Bảng LichSuTrangThaiBaiBao (Audit Trail theo phản biện 2)
CREATE TABLE LichSuTrangThaiBaiBao (
    MaLichSu INT IDENTITY(1,1) PRIMARY KEY,
    MaBaiBao INT NOT NULL,
    TrangThaiCu NVARCHAR(50) NULL,
    TrangThaiMoi NVARCHAR(50) NOT NULL,
    NgayChuyen DATETIME NOT NULL DEFAULT GETDATE(),
    MaNguoiThucHien INT NULL,         -- Người thực hiện thao tác chuyển (Editor/Admin)
    GhiChu NVARCHAR(500) NULL,
    CONSTRAINT FK_LichSu_BaiBao FOREIGN KEY (MaBaiBao) 
        REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE,
    CONSTRAINT FK_LichSu_NguoiDung FOREIGN KEY (MaNguoiThucHien) 
        REFERENCES NguoiDung(MaNguoiDung) ON DELETE SET NULL
);
GO

-- ==========================================
-- 4. TRIGGER RÀNG BUỘC NGHIỆP VỤ XUẤT BẢN
-- ==========================================

-- 4.1 Trigger tự động cập nhật ngày cập nhật bài báo
CREATE TRIGGER TRG_BaiBao_NgayCapNhat
ON BaiBao
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE BaiBao
    SET NgayCapNhat = GETDATE()
    FROM BaiBao b
    INNER JOIN inserted i ON b.MaBaiBao = i.MaBaiBao;
END;
GO

-- 4.2 Trigger kiểm tra bài báo người đó viết phải liên quan đến chuyên môn của họ
CREATE TRIGGER TRG_BaiBao_KiemTraChuyenMonTacGia
ON BaiBao
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra nếu tác giả gửi bài báo có chuyên ngành chưa đăng ký trong danh mục chuyên môn
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN NguoiDung_ChuyenMon cm 
            ON i.MaNguoiDung = cm.MaNguoiDung 
            AND i.MaChuyenNganh = cm.MaChuyenNganh
        WHERE cm.MaChuyenNganh IS NULL
    )
    BEGIN
        RAISERROR (N'LỖI NGHIỆP VỤ: Bài báo người đó viết phải liên quan đến chuyên môn của họ! Tác giả chưa đăng ký chuyên ngành này trong danh mục chuyên môn.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;
END;
GO

-- 4.3 Trigger kiểm tra chuyên gia phản biện phải phù hợp chuyên môn & chống xung đột lợi ích
CREATE TRIGGER TRG_PhanCongPhanBien_KiemTraChuyenMon
ON PhanCongPhanBien
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Chống xung đột lợi ích: Tác giả chính không được tự phản biện bài của mình
    IF EXISTS (
        SELECT 1
        FROM inserted pc
        INNER JOIN BaiBao bb ON pc.MaBaiBao = bb.MaBaiBao
        WHERE pc.MaNguoiDung = bb.MaNguoiDung
    )
    BEGIN
        RAISERROR (N'LỖI XUNG ĐỘT LỢI ÍCH: Tác giả chính của bài báo không được phép làm chuyên gia phản biện cho chính bài báo của mình!', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    -- 2. Chống xung đột lợi ích: Đồng tác giả không được làm chuyên gia phản biện
    -- (Kiểm tra cả liên kết tài khoản MaNguoiDung lẫn đối chiếu Email để chặn triệt để)
    IF EXISTS (
        SELECT 1
        FROM inserted pc
        INNER JOIN DongTacGia dtg ON pc.MaBaiBao = dtg.MaBaiBao 
        INNER JOIN NguoiDung nd ON pc.MaNguoiDung = nd.MaNguoiDung
        WHERE (dtg.MaNguoiDung IS NOT NULL AND pc.MaNguoiDung = dtg.MaNguoiDung)
           OR (dtg.Email IS NOT NULL AND LOWER(LTRIM(RTRIM(dtg.Email))) = LOWER(LTRIM(RTRIM(nd.Email))))
    )
    BEGIN
        RAISERROR (N'LỖI XUNG ĐỘT LỢI ÍCH: Đồng tác giả của bài báo không được phép làm chuyên gia phản biện cho bài báo này (trùng tài khoản hoặc trùng email)!', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    -- 3. Ràng buộc chuyên môn: Gửi mời chuyên gia phản biện phải phù hợp với chuyên môn của họ
    IF EXISTS (
        SELECT 1
        FROM inserted pc
        INNER JOIN BaiBao bb ON pc.MaBaiBao = bb.MaBaiBao
        LEFT JOIN NguoiDung_ChuyenMon cm 
            ON pc.MaNguoiDung = cm.MaNguoiDung 
            AND bb.MaChuyenNganh = cm.MaChuyenNganh
        WHERE cm.MaChuyenNganh IS NULL
    )
    BEGIN
        RAISERROR (N'LỖI NGHIỆP VỤ: Gửi mời chuyên gia phản biện phải phù hợp với chuyên môn của họ! Chuyên gia này chưa đăng ký chuyên môn thuộc chuyên ngành của bài báo.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;
END;
GO

-- ==============================================================================
-- 4.4 STORED PROCEDURES NGHIỆP VỤ (CHUẨN OJS & SCOPUS AUTHOR CLAIMING)
-- ==============================================================================

-- Thủ tục 1: Đồng bộ liên kết đồng tác giả theo Email khi tác giả chủ động xác nhận tác quyền (Author Claiming)
CREATE OR ALTER PROCEDURE sp_DongBoDongTacGia_TheoEmail
    @MaNguoiDung INT,
    @SoBaiDaLienKet INT = 0 OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EmailUser VARCHAR(150);

    -- Lấy thông tin email của tài khoản
    SELECT @EmailUser = Email FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung;

    IF @EmailUser IS NULL
    BEGIN
        RAISERROR (N'LỖI NGHIỆP VỤ: Không tìm thấy tài khoản người dùng với mã cung cấp.', 16, 1);
        RETURN;
    END;

    -- Cập nhật MaNguoiDung cho các bài báo có email trùng khớp mà trước đó đang là NULL (Tác giả khách)
    UPDATE DongTacGia
    SET MaNguoiDung = @MaNguoiDung
    WHERE LOWER(LTRIM(RTRIM(Email))) = LOWER(LTRIM(RTRIM(@EmailUser)))
      AND MaNguoiDung IS NULL;

    SET @SoBaiDaLienKet = @@ROWCOUNT;
END;
GO

-- Thủ tục 2: Truy vấn danh sách bài báo đồng tác giả chưa liên kết theo Email của tài khoản
CREATE OR ALTER PROCEDURE sp_KiemTraDongTacGiaChuaLienKet
    @MaNguoiDung INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EmailUser VARCHAR(150);

    SELECT @EmailUser = Email FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung;

    SELECT 
        dtg.MaDongTacGia,
        dtg.MaBaiBao,
        bb.TieuDe AS TieuDeBaiBao,
        cn.TenChuyenNganh,
        dtg.HoTen AS HoTenSnapshot,
        dtg.DonVi AS DonViSnapshot,
        dtg.LaTacGiaLienHe,
        dtg.ThuTu,
        bb.NgayGui,
        bb.TrangThai AS TrangThaiBaiBao
    FROM DongTacGia dtg
    INNER JOIN BaiBao bb ON dtg.MaBaiBao = bb.MaBaiBao
    INNER JOIN ChuyenNganh cn ON bb.MaChuyenNganh = cn.MaChuyenNganh
    WHERE LOWER(LTRIM(RTRIM(dtg.Email))) = LOWER(LTRIM(RTRIM(@EmailUser)))
      AND dtg.MaNguoiDung IS NULL;
END;
GO

-- ==============================================================================
-- 5. DỮ LIỆU MẪU (SEED DATA CHUẨN ĐỒ ÁN JST)
-- ==============================================================================

-- 5.1 Thêm 5 vai trò chuẩn
SET IDENTITY_INSERT VaiTro ON;
INSERT INTO VaiTro (MaVaiTro, TenVaiTro, MoTa) VALUES
(1, N'Quản trị hệ thống', N'Quản trị người dùng, phân quyền và cấu hình toàn bộ hệ thống'),
(2, N'Ban biên tập', N'Tổng biên tập, Phó tổng biên tập và Thư ký tòa soạn quản lý luồng bài'),
(3, N'Tác giả', N'Tác giả nộp bài, chỉnh sửa bản thảo và theo dõi tiến độ thẩm định'),
(4, N'Chuyên gia phản biện', N'Thực hiện phản biện kín hai chiều và đánh giá bản thảo'),
(5, N'Độc giả', N'Độc giả tra cứu, đọc bài báo và tải toàn văn PDF');
SET IDENTITY_INSERT VaiTro OFF;
GO

-- 5.2 Thêm Chuyên ngành học thuật JST
SET IDENTITY_INSERT ChuyenNganh ON;
INSERT INTO ChuyenNganh (MaChuyenNganh, TenChuyenNganh, MoTa) VALUES
(1, N'Công nghệ thông tin & Trí tuệ nhân tạo', N'Khoa học máy tính, Học máy, Xử lý ngôn ngữ tự nhiên, An toàn thông tin'),
(2, N'Cơ khí – Chế tạo máy – Tự động hóa', N'Kỹ thuật cơ khí, Robot, Cơ điện tử, Hệ thống điều khiển tự động'),
(3, N'Khoa học Môi trường & Nông nghiệp', N'Kỹ thuật môi trường, Xử lý nước thải, Nông nghiệp công nghệ cao, Biến đổi khí hậu'),
(4, N'Kinh tế – Quản trị kinh doanh & Tài chính', N'Kinh tế số, Chuỗi cung ứng, Quản trị doanh nghiệp, Hành vi người tiêu dùng'),
(5, N'Hóa học & Công nghệ thực phẩm', N'Công nghệ thực phẩm, Lên men sinh học, Chiết xuất hợp chất tự nhiên');
SET IDENTITY_INSERT ChuyenNganh OFF;
GO

-- 5.3 Thêm Số tạp chí
SET IDENTITY_INSERT SoTapChi ON;
INSERT INTO SoTapChi (MaSoTapChi, TenSo, Tap, So, Nam, NgayPhatHanh, TrangThai) VALUES
(1, N'Tạp chí Khoa học và Công nghệ - Số 42 (Chuyên đề AI & Chuyển đổi số)', 15, 42, 2026, '2026-11-28', N'Đã xuất bản'),
(2, N'Tạp chí Khoa học và Công nghệ - Số 41', 15, 41, 2026, '2026-08-28', N'Đã xuất bản'),
(3, N'Tạp chí Khoa học và Công nghệ - Số 43', 15, 43, 2026, NULL, N'Đang biên tập');
SET IDENTITY_INSERT SoTapChi OFF;
GO

-- 5.4 Thêm Người dùng mẫu (Đầy đủ Học vị, Học hàm, Giới tính, Quốc gia, Ngân hàng, Địa chỉ)
SET IDENTITY_INSERT NguoiDung ON;
INSERT INTO NguoiDung (MaNguoiDung, TenDangNhap, HoDem, Ten, HoTen, Email, MatKhau, HocVi, HocHam, GioiTinh, NgonNgu, QuocGia, SoDienThoai, DonVi, DiaChi, SoTaiKhoan, ChuTaiKhoan, NganHang, MaORCID, TrangThai, NgayTao) VALUES
(1, 'txhuong', N'Trần Xuân', N'Hướng', N'Trần Xuân Hướng', 'editor@huit.edu.vn', '123456', N'Tiến sĩ', N'Phó giáo sư', N'Nam', N'Tiếng Việt', N'Vietnam', '0901234567', N'Trường Đại học Công Thương TP.HCM', N'140 Lê Trọng Tấn, Tây Thạnh, Tân Phú, TP.HCM', '10123456789', N'TRAN XUAN HUONG', N'Vietcombank', '0000-0002-1825-0091', 1, '2026-01-10'),
(2, 'admin', N'Quản trị', N'Tạp chí', N'Quản trị Tạp chí', 'admin@huit.edu.vn', '123456', N'Thạc sĩ', N'Không', N'Nam', N'Tiếng Việt', N'Vietnam', '0907654321', N'Trường Đại học Công Thương TP.HCM', N'140 Lê Trọng Tấn, Tây Thạnh, Tân Phú, TP.HCM', '10987654321', N'QUAN TRI HE THONG', N'BIDV', NULL, 1, '2026-01-10'),
(3, 'vuthif', N'Vũ Thị', N'F', N'Vũ Thị F', 'vuthif@huit.edu.vn', '123456', N'Tiến sĩ', N'Không', N'Nữ', N'Tiếng Việt', N'Vietnam', '0912345678', N'Khoa CNTT, Trường Đại học Công Thương TP.HCM', N'140 Lê Trọng Tấn, Tây Thạnh, Tân Phú, TP.HCM', '1903456789012', N'VU THI F', N'Vietcombank', '0000-0002-1825-0097', 1, '2026-02-15'),
(4, 'dangvang', N'Đặng Văn', N'G', N'Đặng Văn G', 'dangvang@vnuhcm.edu.vn', '123456', N'Tiến sĩ', N'Phó giáo sư', N'Nam', N'Tiếng Việt', N'Vietnam', '0923456789', N'Viện Công nghệ Tiên tiến, ĐHQG-HCM', N'Khu phố 6, Linh Trung, TP. Thủ Đức, TP.HCM', '0071001234567', N'DANG VAN G', N'Vietcombank', '0000-0003-4567-8901', 1, '2026-02-18'),
(5, 'tranthih', N'Trần Thị', N'H', N'Trần Thị H', 'tranthih@ctub.edu.vn', '123456', N'Tiến sĩ', N'Không', N'Nữ', N'Tiếng Việt', N'Vietnam', '0934567890', N'Khoa Môi trường & Tài nguyên Thiên nhiên, Đại học Cần Thơ', N'Khu II, đường 3/2, Q. Ninh Kiều, TP. Cần Thơ', '0111000234567', N'TRAN THI H', N'VietinBank', '0000-0001-2345-6789', 1, '2026-03-01'),
(6, 'nguyenvank', N'Nguyễn Văn', N'K', N'Nguyễn Văn K', 'nguyenvank@hcmut.edu.vn', '123456', N'Tiến sĩ', N'Không', N'Nam', N'Tiếng Việt', N'Vietnam', '0945678901', N'Khoa Cơ khí, Đại học Bách Khoa TP.HCM', N'268 Lý Thường Kiệt, Quận 10, TP.HCM', '0251009876543', N'NGUYEN VAN K', N'Vietcombank', NULL, 1, '2026-03-05'),
(7, 'lythim', N'Lý Thị', N'M', N'Lý Thị M', 'lythim@huit.edu.vn', '123456', N'Thạc sĩ', N'Không', N'Nữ', N'Tiếng Việt', N'Vietnam', '0956789012', N'Khoa Cơ khí, Trường ĐH Công Thương TP.HCM', N'140 Lê Trọng Tấn, Tây Thạnh, Tân Phú, TP.HCM', '1902888999111', N'LY THI M', N'Techcombank', NULL, 1, '2026-03-10'),
(8, 'tranvann', N'Trần Văn', N'N', N'Trần Văn N', 'tranvann@huit.edu.vn', '123456', N'Tiến sĩ', N'Không', N'Nam', N'Tiếng Việt', N'Vietnam', '0967890123', N'Khoa CNTT, Trường ĐH Công Thương TP.HCM', N'140 Lê Trọng Tấn, Tây Thạnh, Tân Phú, TP.HCM', '0441003456789', N'TRAN VAN N', N'Vietcombank', '0000-0002-9876-5432', 1, '2026-03-12'),
(9, 'phamthiq', N'Phạm Thị', N'Q', N'Phạm Thị Q', 'phamthiq@ueh.edu.vn', '123456', N'Tiến sĩ', N'Không', N'Nữ', N'Tiếng Việt', N'Vietnam', '0978901234', N'Đại học Kinh tế TP.HCM', N'59C Nguyễn Đình Chiểu, Quận 3, TP.HCM', '119000123888', N'PHAM THI Q', N'VietinBank', NULL, 1, '2026-03-15'),
(10, 'dangthanhthi', N'Đặng Thành', N'Thi', N'Đặng Thành Thi', 'dangthanhthi@huit.edu.vn', '123456', N'Tiến sĩ', N'Không', N'Nam', N'Tiếng Việt', N'Vietnam', '0989012345', N'Khoa CNTT, Trường ĐH Công Thương TP.HCM', N'140 Lê Trọng Tấn, Tây Thạnh, Tân Phú, TP.HCM', '0531002345678', N'DANG THANH THI', N'Vietcombank', '0000-0001-8765-4321', 1, '2026-03-20');
SET IDENTITY_INSERT NguoiDung OFF;
GO

-- 5.5 Phân quyền Người dùng - Vai trò
INSERT INTO NguoiDung_VaiTro (MaNguoiDung, MaVaiTro) VALUES
(1, 2), -- Trần Xuân Hướng: Ban biên tập
(2, 1), -- Quản trị Tạp chí: Quản trị hệ thống
(3, 3), -- Vũ Thị F: Tác giả
(4, 4), -- Đặng Văn G: Phản biện viên
(5, 3), -- Trần Thị H: Tác giả
(5, 4), -- Trần Thị H kiêm Phản biện viên
(6, 4), -- Nguyễn Văn K: Phản biện viên
(7, 3), -- Lý Thị M: Tác giả
(8, 3), -- Trần Văn N: Tác giả
(8, 4), -- Trần Văn N kiêm Phản biện viên
(9, 3), -- Phạm Thị Q: Tác giả
(10, 3), -- Đặng Thành Thi: Tác giả
(10, 4); -- Đặng Thành Thi kiêm Phản biện viên
GO

-- 5.6 Thêm Chuyên môn của Người dùng (Bảng nối NguoiDung_ChuyenMon)
-- Cần nạp trước BaiBao để thỏa mãn trigger kiểm tra chuyên môn
INSERT INTO NguoiDung_ChuyenMon (MaNguoiDung, MaChuyenNganh, LaChuyenMonChinh, GhiChu) VALUES
-- 1. Trần Xuân Hướng: Ban biên tập
(1, 1, 1, N'Trí tuệ nhân tạo & Khai phá dữ liệu'),
(1, 2, 0, N'Hệ thống điều khiển tự động'),

-- 2. Quản trị Tạp chí
(2, 1, 1, N'Khoa học máy tính & Mạng máy tính'),

-- 3. Vũ Thị F: Tác giả (CNTT & AI)
(3, 1, 1, N'Xử lý ngôn ngữ tự nhiên & Học sâu'),

-- 4. Đặng Văn G: Phản biện viên (CNTT & Cơ khí)
(4, 1, 1, N'Thị giác máy tính & Robot tự hành'),
(4, 2, 0, N'Hệ thống Cơ điện tử thông minh'),

-- 5. Trần Thị H: Tác giả & Phản biện viên (Môi trường & Thực phẩm)
(5, 3, 1, N'Khoa học môi trường & Xử lý chất thải'),
(5, 5, 0, N'Hợp chất sinh học & Công nghệ sau thu hoạch'),

-- 6. Nguyễn Văn K: Tác giả & Phản biện viên (Cơ khí & Chế tạo máy)
(6, 2, 1, N'Gia công chính xác & Tối ưu hóa cơ khí'),

-- 7. Lý Thị M: Tác giả (Cơ khí & Robot)
(7, 2, 1, N'Cánh tay robot công nghiệp & Điều khiển thích nghi'),

-- 8. Trần Văn N: Tác giả & Phản biện viên (CNTT & AI)
(8, 1, 1, N'Học sâu & Tăng cường dữ liệu văn bản'),

-- 9. Phạm Thị Q: Tác giả (Kinh tế & Quản trị)
(9, 4, 1, N'Kinh tế số & Quản trị chuỗi cung ứng'),

-- 10. Đặng Thành Thi: Tác giả & Phản biện viên (CNTT & Thực phẩm)
(10, 1, 1, N'Bảo mật Blockchain & Mạng cảm biến IoT'),
(10, 5, 0, N'Bảo quản nông sản & Màng bao sinh học');
GO

-- 5.7 Thêm Bài báo mẫu (Bao quát 6 giai đoạn quy trình JST và khớp với Web)
SET IDENTITY_INSERT BaiBao ON;
INSERT INTO BaiBao (MaBaiBao, TieuDe, TieuDeTiengAnh, TomTat, TomTatTiengAnh, TuKhoa, TrangThai, MaDOI, NgayGui, NgayCapNhat, MaNguoiDung, MaChuyenNganh, MaSoTapChi, TrangBatDau, TrangKetThuc) VALUES
-- Bài 1: Bài nổi bật trên Web & Đã xuất bản
(1, N'Ứng dụng mô hình Transformer trong phân loại văn bản tiếng Việt quy mô lớn', 
    N'Application of Transformer Models in Large-Scale Vietnamese Text Classification', 
    N'Nghiên cứu đề xuất giải pháp cải tiến dựa trên kiến trúc Transformer kết hợp cơ chế tự chú ý đa đầu để phân loại văn bản tiếng Việt quy mô lớn, đạt F1-score 94,8% trên 250.000 bài báo.', 
    N'This paper proposes an enhanced Transformer-based architecture with multi-head self-attention for Vietnamese text classification, achieving an F1-score of 94.8% on a benchmark of 250,000 articles.', 
    N'Transformer, Phân loại văn bản, NLP, Tiếng Việt', 
    N'Đã xuất bản', '10.59876/huit.jsc.2026.42.04', '2026-09-10', '2026-11-28', 3, 1, 1, 45, 58),

-- Bài 2: Đã xuất bản
(2, N'Đánh giá tác động của biến đổi khí hậu đến năng suất lúa Đồng bằng sông Cửu Long', 
    N'Assessing Climate Change Impacts on Rice Productivity in the Mekong Delta', 
    N'Phân tích chuỗi dữ liệu 20 năm nhằm lượng hóa mức độ ảnh hưởng của các yếu tố khí hậu cực đoan đến sản lượng lúa vùng đồng bằng.', 
    N'Analysis of 20-year climate dataset to quantify extreme weather impacts on rice yields in the delta region.', 
    N'Biến đổi khí hậu, Năng suất lúa, ĐBSCL, Môi trường', 
    N'Đã xuất bản', '10.59876/huit.jsc.2026.42.02', '2026-08-15', '2026-11-28', 5, 3, 1, 15, 28),

-- Bài 3: Đã xuất bản
(3, N'Tối ưu hóa thông số cắt gọt trong gia công hợp kim nhôm bằng phương pháp Taguchi', 
    N'Optimization of Cutting Parameters in Aluminum Alloy Machining Using Taguchi Method', 
    N'Xác định bộ thông số cắt tối ưu thông qua kết hợp phương pháp Taguchi và phân tích phương sai, giúp giảm 18% thời gian gia công.', 
    N'Determining optimal cutting parameters through Taguchi and ANOVA integration, reducing machining time by 18%.', 
    N'Gia công cơ khí, Hợp kim nhôm, Taguchi, Tối ưu hóa', 
    N'Đã xuất bản', '10.59876/huit.jsc.2026.42.03', '2026-08-20', '2026-11-28', 6, 2, 1, 29, 44),

-- Bài 4: Đã xuất bản
(4, N'Kỹ thuật tăng cường dữ liệu cho mô hình học sâu xử lý ngôn ngữ tiếng Việt', 
    N'Data Augmentation Techniques for Vietnamese Natural Language Deep Learning Models', 
    N'Khảo sát các kỹ thuật tăng cường dữ liệu văn bản nhằm cải thiện hiệu suất mô hình trong điều kiện dữ liệu huấn luyện hạn chế.', 
    N'Investigating text data augmentation techniques to improve model performance under scarce training data constraints.', 
    N'Học sâu, Tăng cường dữ liệu, NLP, Tiếng Việt', 
    N'Đã xuất bản', '10.59876/huit.jsc.2026.41.01', '2026-06-12', '2026-08-28', 8, 1, 2, 1, 14),

-- Bài 5: Đã xuất bản
(5, N'Tác động của chuyển đổi số đến hiệu quả hoạt động doanh nghiệp vừa và nhỏ', 
    N'Impact of Digital Transformation on SMEs Performance', 
    N'Khảo sát 320 doanh nghiệp vừa và nhỏ cho thấy mối tương quan tích cực giữa mức độ chuyển đổi số và năng suất lao động.', 
    N'Empirical survey of 320 SMEs demonstrating a positive correlation between digital transformation maturity and labor productivity.', 
    N'Chuyển đổi số, Hiệu quả doanh nghiệp, Doanh nghiệp vừa và nhỏ, Kinh tế số', 
    N'Đã xuất bản', '10.59876/huit.jsc.2026.41.05', '2026-06-25', '2026-08-28', 9, 4, 2, 59, 72),

-- Bài 6: Đang chế bản (Giai đoạn 5)
(6, N'Giải pháp bảo mật dữ liệu IoT dựa trên công nghệ blockchain', 
    N'IoT Data Security Solution Based on Blockchain Technology', 
    N'Đề xuất giao thức đồng thuận nhẹ kết hợp mã hóa bất đối xứng tối ưu cho các thiết bị IoT giới hạn tài nguyên tính toán.', 
    N'Proposing a lightweight consensus protocol paired with asymmetric cryptography optimized for resource-constrained IoT devices.', 
    N'Bảo mật IoT, Blockchain, Mã hóa nhẹ, An toàn mạng', 
    N'Đang chế bản', NULL, '2026-10-01', '2026-11-25', 10, 1, 3, NULL, NULL),

-- Bài 7: Chờ quyết định (Giai đoạn 3)
(7, N'Nghiên cứu bảo quản thực phẩm bằng màng bao sinh học chiết xuất từ rong biển', 
    N'Food Preservation Study Using Seaweed-Derived Biodegradable Films', 
    N'Đánh giá hiệu quả kháng khuẩn và kéo dài thời gian bảo quản trái cây tươi của màng bao sinh học polysaccharide.', 
    N'Evaluating antimicrobial efficacy and shelf-life extension of fresh fruits using polysaccharide edible films.', 
    N'Màng sinh học, Bảo quản thực phẩm, Rong biển, Kháng khuẩn', 
    N'Chờ quyết định', NULL, '2026-10-15', '2026-11-20', 10, 5, NULL, NULL, NULL),

-- Bài 8: Đang phản biện (Giai đoạn 2)
(8, N'Thiết kế bộ điều khiển thích nghi cho cánh tay robot 6 bậc tự do', 
    N'Adaptive Controller Design for 6-DOF Robotic Arm', 
    N'Áp dụng lý thuyết Lyapunov để thiết kế bộ điều khiển thích nghi bù trừ phi tuyến và nhiễu ngoài cho cánh tay robot công nghiệp.', 
    N'Applying Lyapunov stability theory to design an adaptive controller compensating nonlinear dynamics and disturbances for industrial robots.', 
    N'Cánh tay robot, Điều khiển thích nghi, Lyapunov, Tự động hóa', 
    N'Đang phản biện', NULL, '2026-11-01', '2026-11-15', 7, 2, NULL, NULL, NULL),

-- Bài 9: Chờ chỉnh sửa (Giai đoạn 4)
(9, N'Thu hồi kim loại nặng từ nguồn nước thải công nghiệp bằng vật liệu nano từ tính', 
    N'Heavy Metal Recovery from Industrial Wastewater Using Magnetic Nanomaterials', 
    N'Tổng hợp hạt nano Fe3O4 biến tính bề mặt nhằm hấp phụ chọn lọc ion Pb2+ và Cd2+ trong nước thải dệt nhuộm.', 
    N'Synthesizing surface-modified Fe3O4 nanoparticles for selective adsorption of Pb2+ and Cd2+ ions in textile wastewater.', 
    N'Vật liệu nano, Xử lý nước thải, Hấp phụ kim loại nặng, Môi trường', 
    N'Chờ chỉnh sửa', NULL, '2026-10-20', '2026-11-18', 5, 3, NULL, NULL, NULL),

-- Bài 10: Chờ sơ duyệt (Giai đoạn 1)
(10, N'Ứng dụng học sâu trong nhận diện sớm bệnh vàng lá trên cây có múi', 
     N'Deep Learning Application for Early Detection of Citrus Greening Disease', 
     N'Xây dựng mô hình Convolutional Neural Network tối ưu hóa kiến trúc MobileNetV3 để nhận diện bệnh vàng lá qua hình ảnh chụp từ drone.', 
     N'Developing a Convolutional Neural Network optimizing MobileNetV3 architecture to diagnose citrus disease from drone imagery.', 
     N'Học sâu, Nhận diện hình ảnh, Nông nghiệp thông minh, CNN', 
     N'Chờ sơ duyệt', NULL, '2026-11-28', '2026-11-28', 3, 1, NULL, NULL, NULL);
SET IDENTITY_INSERT BaiBao OFF;
GO

-- 5.8 Thêm Đồng tác giả (Gồm cả tác giả nội bộ liên kết tài khoản và tác giả khách ngoài hệ thống)
INSERT INTO DongTacGia (MaBaiBao, HoTen, Email, DonVi, MaORCID, LaTacGiaLienHe, ThuTu, MaNguoiDung) VALUES
(1, N'Đặng Văn G', 'dangvang@vnuhcm.edu.vn', N'Viện Công nghệ Tiên tiến, ĐHQG-HCM', '0000-0002-4512-8890', 0, 2, 4),
(3, N'Lý Thị M', 'lythim@huit.edu.vn', N'Khoa Cơ khí, Trường ĐH Công Thương TP.HCM', '0000-0002-3344-5566', 0, 2, 7),
(5, N'Ngô Văn R', 'ngovanr@ueh.edu.vn', N'Đại học Kinh tế TP.HCM', '0000-0003-7788-9900', 0, 2, NULL),
(10, N'Lê Hoàng Long', 'longlh@vast.ac.vn', N'Viện Công nghệ Thông tin, Viện Hàn lâm KH&CN VN', '0000-0001-9988-7766', 1, 2, NULL);
GO

-- 5.9 Thêm Thư mục bài báo
INSERT INTO ThuMucBaiBao (TenThuMuc, DuongDan, LoaiThuMuc, KichThuoc, SoVong, NgayTaiLen, MaBaiBao) VALUES
('BanThao_Goctoanvan_v1.docx', '/uploads/2026/09/BanThao_Goctoanvan_v1.docx', N'Bản thảo gốc', 2450000, 1, '2026-09-10', 1),
('BanThao_AnDanh_PhanBien.docx', '/uploads/2026/09/BanThao_AnDanh_PhanBien.docx', N'File ẩn danh', 2300000, 1, '2026-09-12', 1),
('BanThao_Galley_InAn.pdf', '/uploads/2026/11/BanThao_Galley_InAn.pdf', N'Bản chỉnh sửa', 1450000, 1, '2026-11-25', 1),
('BanThao_IoT_Blockchain_v1.pdf', '/uploads/2026/10/BanThao_IoT_Blockchain_v1.pdf', N'Bản thảo gốc', 1890000, 1, '2026-10-01', 6),
('Robot_Arm_Adaptive_Review.docx', '/uploads/2026/11/Robot_Arm_Adaptive_Review.docx', N'File ẩn danh', 3120000, 1, '2026-11-01', 8);
GO

-- 5.10 Thêm Phân công phản biện (Thỏa mãn ràng buộc đúng chuyên môn và chống xung đột lợi ích)
SET IDENTITY_INSERT PhanCongPhanBien ON;
INSERT INTO PhanCongPhanBien (MaPhanCong, SoVong, NgayPhanCong, HanPhanHoi, HanHoanThanh, TrangThai, MaBaiBao, MaNguoiDung) VALUES
-- Bài 1 (CNTT & AI): Phản biện Đặng Thành Thi (10) & Trần Văn N (8) - Cả 2 đều có chuyên môn CNTT, không xung đột tác giả
(1, 1, '2026-09-15', '2026-09-22', '2026-10-15', N'Đã đánh giá', 1, 10),
(2, 1, '2026-09-15', '2026-09-22', '2026-10-15', N'Đã đánh giá', 1, 8),
-- Bài 7 (Thực phẩm): Phản biện Trần Thị H (5) - Có chuyên môn Thực phẩm, tác giả là Đặng Thành Thi (10)
(3, 1, '2026-10-18', '2026-10-25', '2026-11-15', N'Đã đánh giá', 7, 5),
-- Bài 8 (Cơ khí & Robot): Phản biện Nguyễn Văn K (6) & Đặng Văn G (4) - Cả 2 đều có chuyên môn Cơ khí, tác giả là Lý Thị M (7)
(4, 1, '2026-11-05', '2026-11-12', '2026-12-05', N'Đang đánh giá', 8, 6),
(5, 1, '2026-11-05', '2026-11-12', '2026-12-05', N'Đang đánh giá', 8, 4);
SET IDENTITY_INSERT PhanCongPhanBien OFF;
GO

-- 5.11 Thêm Phiếu đánh giá của phản biện
INSERT INTO PhieuDanhGia (DiemTinhMoi, DiemPhuongPhap, DiemKetQua, DiemTrinhBay, DiemTongKet, NhanXetChoTacGia, NhanXetBaoMat, KienNghi, NgayDanhGia, MaPhanCong) VALUES
(9.0, 9.5, 9.5, 9.0, 9.3, 
 N'Bài viết có chất lượng học thuật rất tốt, mô hình đề xuất cải tiến rõ rệt so với các baseline hiện nay. Cần bổ sung bảng so sánh độ phức tạp tính toán.', 
 N'Đề xuất chấp nhận đăng ngay vào số chuyên đề AI.', 
 N'Chấp nhận đăng', '2026-10-12', 1),

(8.5, 9.0, 9.0, 8.5, 8.8, 
 N'Thực nghiệm kỹ lưỡng trên tập dữ liệu lớn tiếng Việt. Tác giả nên giải thích rõ hơn về bước tiền xử lý từ ghép.', 
 N'Đồng ý cho đăng sau khi tác giả giải trình phần tiền xử lý.', 
 N'Chỉnh sửa nhỏ', '2026-10-14', 2),

(8.0, 8.5, 8.0, 8.5, 8.2, 
 N'Ý tưởng bảo quản thực phẩm có tính ứng dụng thực tế cao. Đề nghị bổ sung phân tích chỉ số vi sinh vật ở tuần bảo quản thứ 3.', 
 N'Có thể xem xét đăng nếu tác giả bổ sung đủ số liệu vi sinh.', 
 N'Chỉnh sửa nhỏ', '2026-11-14', 3);
GO

-- 5.12 Thêm Lịch sử trạng thái bài báo (Audit Trail)
INSERT INTO LichSuTrangThaiBaiBao (MaBaiBao, TrangThaiCu, TrangThaiMoi, NgayChuyen, MaNguoiThucHien, GhiChu) VALUES
-- Lịch sử Bài 1 (Khớp hoàn toàn với Timeline trên Web article-detail.html)
(1, NULL, N'Chờ sơ duyệt', '2026-09-10 08:30:00', 3, N'Tác giả Vũ Thị F nộp bản thảo mới lên hệ thống'),
(1, N'Chờ sơ duyệt', N'Đang phản biện', '2026-09-15 14:15:00', 1, N'Thư ký tòa soạn kiểm tra hình thức đạt yêu cầu và giao 02 chuyên gia phản biện'),
(1, N'Đang phản biện', N'Chờ chỉnh sửa', '2026-10-16 09:00:00', 1, N'Hội đồng gửi nhận xét phản biện yêu cầu tác giả bổ sung'),
(1, N'Chờ chỉnh sửa', N'Đã chấp nhận', '2026-11-05 16:45:00', 1, N'Ban biên tập chấp thuận bản thảo sau khi tác giả hoàn thiện'),
(1, N'Đã chấp nhận', N'Đang chế bản', '2026-11-10 10:00:00', 1, N'Chuyển bài sang bộ phận kỹ thuật đọc bông và chế bản'),
(1, N'Đang chế bản', N'Đã xuất bản', '2026-11-28 08:00:00', 1, N'Công bố chính thức trong Tập 15, Số 42 (2026)'),

-- Lịch sử Bài 8
(8, NULL, N'Chờ sơ duyệt', '2026-11-01 10:00:00', 7, N'Tác giả Lý Thị M nộp bài'),
(8, N'Chờ sơ duyệt', N'Đang phản biện', '2026-11-05 15:30:00', 1, N'Phân công 2 chuyên gia phản biện');
GO
