-- Tạo Cơ sở dữ liệu
CREATE DATABASE Journal_DB;
GO
USE Journal_DB;
GO

-- ==========================================
-- CỤM 1: QUẢN TRỊ NGƯỜI DÙNG & DANH MỤC
-- ==========================================

CREATE TABLE VaiTro (
    MaVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50) NOT NULL, -- (Quản trị hệ thống, Ban biên tập, Tác giả, Phản biện viên, Độc giả)
    MoTa NVARCHAR(255)
);

CREATE TABLE NguoiDung (
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    MatKhau NVARCHAR(255) NOT NULL,
    SoDienThoai NVARCHAR(15),
    DonVi NVARCHAR(200),
    HocVi NVARCHAR(50),
    MaORCID NVARCHAR(50),
    TrangThai BIT DEFAULT 1, -- 1: Hoạt động, 0: Khóa
    NgayTao DATETIME DEFAULT GETDATE()
);

CREATE TABLE NguoiDung_VaiTro (
    MaNguoiDung INT,
    MaVaiTro INT,
    PRIMARY KEY (MaNguoiDung, MaVaiTro),
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung) ON DELETE CASCADE,
    FOREIGN KEY (MaVaiTro) REFERENCES VaiTro(MaVaiTro) ON DELETE CASCADE
);

CREATE TABLE ChuyenNganh (
    MaChuyenNganh INT IDENTITY(1,1) PRIMARY KEY,
    TenChuyenNganh NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(500)
);

-- ==========================================
-- CỤM 2: BÀI BÁO & TÁC GIẢ
-- ==========================================

CREATE TABLE BaiBao (
    MaBaiBao INT IDENTITY(1,1) PRIMARY KEY,
    TieuDe NVARCHAR(500) NOT NULL,
    TieuDeTiengAnh NVARCHAR(500),
    TomTat NTEXT NOT NULL,
    TomTatTiengAnh NTEXT,
    TuKhoa NVARCHAR(300),
    MaChuyenNganh INT,
    MaTacGiaChinh INT NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Chờ sơ duyệt',
    MaDOI NVARCHAR(100),
    NgayGui DATETIME DEFAULT GETDATE(),
    NgayCapNhat DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MaChuyenNganh) REFERENCES ChuyenNganh(MaChuyenNganh) ON DELETE NO ACTION,
    FOREIGN KEY (MaTacGiaChinh) REFERENCES NguoiDung(MaNguoiDung) ON DELETE NO ACTION,
    CONSTRAINT CHK_TrangThaiBaiBao CHECK (TrangThai IN (N'Chờ sơ duyệt', N'Chờ sửa hình thức', N'Đang phản biện', N'Chờ chỉnh sửa', N'Đã chấp nhận', N'Từ chối', N'Đã xuất bản'))
);

CREATE TABLE DongTacGia (
    MaDongTacGia INT IDENTITY(1,1) PRIMARY KEY,
    MaBaiBao INT NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150),
    DonVi NVARCHAR(200),
    ThuTu INT DEFAULT 1, -- Thứ tự hiển thị tác giả
    FOREIGN KEY (MaBaiBao) REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE
);

CREATE TABLE ThuMucBaiBao (
    MaThuMuc INT IDENTITY(1,1) PRIMARY KEY,
    MaBaiBao INT NOT NULL,
    TenFileGoc NVARCHAR(255) NOT NULL,
    DuongDanLuu NVARCHAR(500) NOT NULL,
    LoaiFile NVARCHAR(50), -- (Bản thảo gốc, Dữ liệu thô, Bản giải trình, Galley PDF)
    LanNop INT DEFAULT 1,
    NgayTaiLen DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MaBaiBao) REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE
);

-- ==========================================
-- CỤM 3: PHẢN BIỆN & ĐÁNH GIÁ
-- ==========================================

CREATE TABLE PhanCongPhanBien (
    MaPhanCong INT IDENTITY(1,1) PRIMARY KEY,
    MaBaiBao INT NOT NULL,
    MaPhanBien INT NOT NULL,
    NgayPhanCong DATETIME DEFAULT GETDATE(),
    HanPhanHoi DATETIME NOT NULL,
    HanHoanThanh DATETIME NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Chờ phản hồi',
    FOREIGN KEY (MaBaiBao) REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE,
    FOREIGN KEY (MaPhanBien) REFERENCES NguoiDung(MaNguoiDung) ON DELETE NO ACTION,
    CONSTRAINT CHK_TrangThaiPhanCong CHECK (TrangThai IN (N'Chờ phản hồi', N'Đang đánh giá', N'Đã đánh giá', N'Từ chối'))
);

CREATE TABLE PhieuDanhGia (
    MaPhieu INT IDENTITY(1,1) PRIMARY KEY,
    MaPhanCong INT NOT NULL UNIQUE, -- Quan hệ 1-1 với Phân công
    DiemTongHop FLOAT,
    NhanXetTacGia NTEXT,
    NhanXetToaSoan NTEXT,
    KienNghiKetLuan NVARCHAR(100),
    NgayDanhGia DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MaPhanCong) REFERENCES PhanCongPhanBien(MaPhanCong) ON DELETE CASCADE,
    CONSTRAINT CHK_KienNghi CHECK (KienNghiKetLuan IN (N'Chấp nhận đăng', N'Sửa chữa nhỏ', N'Sửa chữa lớn', N'Từ chối đăng')),
    CONSTRAINT CHK_Diem CHECK (DiemTongHop >= 0 AND DiemTongHop <= 10)
);

-- ==========================================
-- CỤM 4: XUẤT BẢN & PHÁT HÀNH
-- ==========================================

CREATE TABLE SoTapChi (
    MaSoBao INT IDENTITY(1,1) PRIMARY KEY,
    TenSoBao NVARCHAR(200) NOT NULL,
    Tap INT NOT NULL,
    So INT NOT NULL,
    Nam INT NOT NULL,
    NgayXuatBan DATETIME,
    TrangThai NVARCHAR(50) DEFAULT N'Đang biên tập'
);

CREATE TABLE BaiBao_SoTapChi (
    MaSoBao INT,
    MaBaiBao INT,
    TuTrang INT,
    DenTrang INT,
    PRIMARY KEY (MaSoBao, MaBaiBao),
    FOREIGN KEY (MaSoBao) REFERENCES SoTapChi(MaSoBao) ON DELETE CASCADE,
    FOREIGN KEY (MaBaiBao) REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE
);
