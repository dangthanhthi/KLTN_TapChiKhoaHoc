USE master;
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

-- 2. Bảng NguoiDung
CREATE TABLE NguoiDung (
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    MatKhau VARCHAR(255) NOT NULL,
    SoDienThoai VARCHAR(20) NULL,
    DonVi NVARCHAR(255) NULL,
    HocVi NVARCHAR(50) NULL,
    MaORCID VARCHAR(50) NULL,
    TrangThai BIT NOT NULL DEFAULT 1,
    NgayTao DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- Filtered Unique Index cho MaORCID (cho phép nhiều NULL, nhưng có giá trị thì phải duy nhất)
CREATE UNIQUE INDEX UQ_NguoiDung_ORCID ON NguoiDung(MaORCID) WHERE MaORCID IS NOT NULL;
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

-- 8. Bảng DongTacGia (Bổ sung MaNguoiDung tùy chọn liên kết tài khoản)
CREATE TABLE DongTacGia (
    MaDongTacGia INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(150) NULL,
    DonVi NVARCHAR(255) NULL,
    ThuTu INT NOT NULL DEFAULT 1,
    MaBaiBao INT NOT NULL,
    MaNguoiDung INT NULL,         -- Tùy chọn: liên kết tới NguoiDung nếu đồng tác giả đã có tài khoản
    CONSTRAINT FK_DongTacGia_BaiBao FOREIGN KEY (MaBaiBao) 
        REFERENCES BaiBao(MaBaiBao) ON DELETE CASCADE,
    CONSTRAINT FK_DongTacGia_NguoiDung FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDung(MaNguoiDung) ON DELETE SET NULL,
    CONSTRAINT UQ_DongTacGia_BaiBao_ThuTu UNIQUE (MaBaiBao, ThuTu),
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
        N'Phụ lục'
    ))
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
-- 4. TRIGGER TỰ ĐỘNG CẬP NHẬT NGÀY CẬP NHẬT
-- ==========================================
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

-- ==============================================================================
-- 5. DỮ LIỆU MẪU (SEED DATA CHUẨN ĐỒ ÁN TỐT NGHIỆP)
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

-- 5.2 Thêm Chuyên ngành học thuật
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

-- 5.4 Thêm Người dùng mẫu
SET IDENTITY_INSERT NguoiDung ON;
INSERT INTO NguoiDung (MaNguoiDung, HoTen, Email, MatKhau, SoDienThoai, DonVi, HocVi, MaORCID, TrangThai, NgayTao) VALUES
(1, N'Trần Xuân Hướng', 'editor@journal.edu.vn', '123456', '0901234567', N'Trường Đại học', N'PGS.TS', '0000-0002-1825-0091', 1, '2026-01-10'),
(2, N'Quản trị Tạp chí', 'admin@journal.edu.vn', '123456', '0907654321', N'Trường Đại học', N'ThS', NULL, 1, '2026-01-10'),
(3, N'Vũ Thị F', 'vuthif@journal.edu.vn', '123456', '0912345678', N'Khoa CNTT, Trường Đại học', N'TS', '0000-0002-1825-0097', 1, '2026-02-15'),
(4, N'Đặng Văn G', 'dangvang@vnuhcm.edu.vn', '123456', '0923456789', N'Viện Công nghệ Tiên tiến, ĐHQG-HCM', N'PGS.TS', '0000-0003-4567-8901', 1, '2026-02-18'),
(5, N'Trần Thị H', 'tranthih@ctub.edu.vn', '123456', '0934567890', N'Đại học Cần Thơ', N'TS', '0000-0001-2345-6789', 1, '2026-03-01'),
(6, N'Nguyễn Văn K', 'nguyenvank@hcmut.edu.vn', '123456', '0945678901', N'Đại học Bách Khoa TP.HCM', N'TS', NULL, 1, '2026-03-05'),
(7, N'Lý Thị M', 'lythim@journal.edu.vn', '123456', '0956789012', N'Khoa Cơ khí, Viện Nghiên cứu Khoa học & Công nghệ', N'ThS', NULL, 1, '2026-03-10'),
(8, N'Trần Văn N', 'tranvann@journal.edu.vn', '123456', '0967890123', N'Khoa CNTT, Viện Nghiên cứu Khoa học & Công nghệ', N'TS', '0000-0002-9876-5432', 1, '2026-03-12'),
(9, N'Hoàng Thị P', 'hoangthip@journal.edu.vn', '123456', '0978901234', N'Khoa Hóa học, Đại học Quốc gia', N'PGS.TS', '0000-0003-4567-8901', 1, '2026-03-15'),
(10, N'Đặng Thành Thi', 'dangthanhthi@journal.edu.vn', '123456', '0989012345', N'Khoa CNTT, Viện Nghiên cứu Khoa học & Công nghệ', N'TS', '0000-0001-8765-4321', 1, '2026-03-20');
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

-- 5.6 Thêm Bài báo mẫu (Bao quát 6 giai đoạn quy trình JST và khớp với Web)
SET IDENTITY_INSERT BaiBao ON;
INSERT INTO BaiBao (MaBaiBao, TieuDe, TieuDeTiengAnh, TomTat, TomTatTiengAnh, TuKhoa, TrangThai, MaDOI, NgayGui, NgayCapNhat, MaNguoiDung, MaChuyenNganh, MaSoTapChi, TrangBatDau, TrangKetThuc) VALUES
-- Bài 1: Bài nổi bật trên Web & Đã xuất bản
(1, N'Ứng dụng mô hình Transformer trong phân loại văn bản tiếng Việt quy mô lớn', 
    N'Application of Transformer Models in Large-Scale Vietnamese Text Classification', 
    N'Nghiên cứu đề xuất giải pháp cải tiến dựa trên kiến trúc Transformer kết hợp cơ chế tự chú ý đa đầu để phân loại văn bản tiếng Việt quy mô lớn, đạt F1-score 94,8% trên 250.000 bài báo.', 
    N'This paper proposes an enhanced Transformer-based architecture with multi-head self-attention for Vietnamese text classification, achieving an F1-score of 94.8% on a benchmark of 250,000 articles.', 
    N'Transformer, Phân loại văn bản, NLP, Tiếng Việt', 
    N'Đã xuất bản', '10.59876/jst.jsc.2026.42.04', '2026-09-10', '2026-11-28', 3, 1, 1, 45, 58),

-- Bài 2: Đã xuất bản
(2, N'Đánh giá tác động của biến đổi khí hậu đến năng suất lúa Đồng bằng sông Cửu Long', 
    N'Assessing Climate Change Impacts on Rice Productivity in the Mekong Delta', 
    N'Phân tích chuỗi dữ liệu 20 năm nhằm lượng hóa mức độ ảnh hưởng của các yếu tố khí hậu cực đoan đến sản lượng lúa vùng đồng bằng.', 
    N'Analysis of 20-year climate dataset to quantify extreme weather impacts on rice yields in the delta region.', 
    N'Biến đổi khí hậu, Năng suất lúa, ĐBSCL, Môi trường', 
    N'Đã xuất bản', '10.59876/jst.jsc.2026.42.02', '2026-08-15', '2026-11-28', 5, 3, 1, 15, 28),

-- Bài 3: Đã xuất bản
(3, N'Tối ưu hóa thông số cắt gọt trong gia công hợp kim nhôm bằng phương pháp Taguchi', 
    N'Optimization of Cutting Parameters in Aluminum Alloy Machining Using Taguchi Method', 
    N'Xác định bộ thông số cắt tối ưu thông qua kết hợp phương pháp Taguchi và phân tích phương sai, giúp giảm 18% thời gian gia công.', 
    N'Determining optimal cutting parameters through Taguchi and ANOVA integration, reducing machining time by 18%.', 
    N'Gia công cơ khí, Hợp kim nhôm, Taguchi, Tối ưu hóa', 
    N'Đã xuất bản', '10.59876/jst.jsc.2026.42.03', '2026-08-20', '2026-11-28', 6, 2, 1, 29, 44),

-- Bài 4: Đã xuất bản
(4, N'Kỹ thuật tăng cường dữ liệu cho mô hình học sâu xử lý ngôn ngữ tiếng Việt', 
    N'Data Augmentation Techniques for Vietnamese Natural Language Deep Learning Models', 
    N'Khảo sát các kỹ thuật tăng cường dữ liệu văn bản nhằm cải thiện hiệu suất mô hình trong điều kiện dữ liệu huấn luyện hạn chế.', 
    N'Investigating text data augmentation techniques to improve model performance under scarce training data constraints.', 
    N'Học sâu, Tăng cường dữ liệu, NLP, Tiếng Việt', 
    N'Đã xuất bản', '10.59876/jst.jsc.2026.41.01', '2026-06-12', '2026-08-28', 8, 1, 2, 1, 14),

-- Bài 5: Đã xuất bản
(5, N'Tác động của chuyển đổi số đến hiệu quả hoạt động doanh nghiệp vừa và nhỏ', 
    N'Impact of Digital Transformation on SMEs Performance', 
    N'Khảo sát 320 doanh nghiệp vừa và nhỏ cho thấy mối tương quan tích cực giữa mức độ chuyển đổi số và năng suất lao động.', 
    N'Empirical survey of 320 SMEs demonstrating a positive correlation between digital transformation maturity and labor productivity.', 
    N'Chuyển đổi số, Hiệu quả doanh nghiệp, Doanh nghiệp vừa và nhỏ, Kinh tế số', 
    N'Đã xuất bản', '10.59876/jst.jsc.2026.41.05', '2026-06-25', '2026-08-28', 9, 4, 2, 59, 72),

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
    N'Chờ quyết định', NULL, '2026-10-15', '2026-11-20', 7, 5, NULL, NULL, NULL),

-- Bài 8: Đang phản biện (Giai đoạn 2)
(8, N'Thiết kế bộ điều khiển thích nghi cho cánh tay robot 6 bậc tự do', 
    N'Adaptive Controller Design for 6-DOF Robotic Arm', 
    N'Áp dụng lý thuyết Lyapunov để thiết kế bộ điều khiển thích nghi bù trừ phi tuyến và nhiễu ngoài cho cánh tay robot công nghiệp.', 
    N'Applying Lyapunov stability theory to design an adaptive controller compensating nonlinear dynamics and disturbances for industrial robots.', 
    N'Cánh tay robot, Điều khiển thích nghi, Lyapunov, Tự động hóa', 
    N'Đang phản biện', NULL, '2026-11-01', '2026-11-15', 6, 2, NULL, NULL, NULL),

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

-- 5.7 Thêm Đồng tác giả
INSERT INTO DongTacGia (MaBaiBao, HoTen, Email, DonVi, ThuTu, MaNguoiDung) VALUES
(1, N'Đặng Văn G', 'dangvang@vnuhcm.edu.vn', N'Viện Công nghệ Tiên tiến, ĐHQG-HCM', 2, 4),
(3, N'Lý Thị M', 'lythim@journal.edu.vn', N'Khoa Cơ khí, Viện Nghiên cứu Khoa học & Công nghệ', 2, 7),
(5, N'Ngô Văn R', 'ngovanr@ueh.edu.vn', N'Đại học Kinh tế TP.HCM', 2, NULL);
GO

-- 5.8 Thêm Thư mục bài báo
INSERT INTO ThuMucBaiBao (TenThuMuc, DuongDan, LoaiThuMuc, KichThuoc, SoVong, NgayTaiLen, MaBaiBao) VALUES
('BanThao_Goctoanvan_v1.docx', '/uploads/2026/09/BanThao_Goctoanvan_v1.docx', N'Bản thảo gốc', 2450000, 1, '2026-09-10', 1),
('BanThao_AnDanh_PhanBien.docx', '/uploads/2026/09/BanThao_AnDanh_PhanBien.docx', N'File ẩn danh', 2300000, 1, '2026-09-12', 1),
('BanThao_Galley_InAn.pdf', '/uploads/2026/11/BanThao_Galley_InAn.pdf', N'Bản chỉnh sửa', 1450000, 1, '2026-11-25', 1),
('BanThao_IoT_Blockchain_v1.pdf', '/uploads/2026/10/BanThao_IoT_Blockchain_v1.pdf', N'Bản thảo gốc', 1890000, 1, '2026-10-01', 6),
('Robot_Arm_Adaptive_Review.docx', '/uploads/2026/11/Robot_Arm_Adaptive_Review.docx', N'File ẩn danh', 3120000, 1, '2026-11-01', 8);
GO

-- 5.9 Thêm Phân công phản biện
SET IDENTITY_INSERT PhanCongPhanBien ON;
INSERT INTO PhanCongPhanBien (MaPhanCong, SoVong, NgayPhanCong, HanPhanHoi, HanHoanThanh, TrangThai, MaBaiBao, MaNguoiDung) VALUES
-- Bài 1 (Đã phản biện xong)
(1, 1, '2026-09-15', '2026-09-22', '2026-10-15', N'Đã đánh giá', 1, 4),
(2, 1, '2026-09-15', '2026-09-22', '2026-10-15', N'Đã đánh giá', 1, 8),
-- Bài 7 (Đã đánh giá xong vòng 1)
(3, 1, '2026-10-18', '2026-10-25', '2026-11-15', N'Đã đánh giá', 7, 5),
-- Bài 8 (Đang đánh giá)
(4, 1, '2026-11-05', '2026-11-12', '2026-12-05', N'Đang đánh giá', 8, 6),
(5, 1, '2026-11-05', '2026-11-12', '2026-12-05', N'Đang đánh giá', 8, 4);
SET IDENTITY_INSERT PhanCongPhanBien OFF;
GO

-- 5.10 Thêm Phiếu đánh giá của phản biện
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

-- 5.11 Thêm Lịch sử trạng thái bài báo (Audit Trail)
INSERT INTO LichSuTrangThaiBaiBao (MaBaiBao, TrangThaiCu, TrangThaiMoi, NgayChuyen, MaNguoiThucHien, GhiChu) VALUES
-- Lịch sử Bài 1 (Khớp hoàn toàn với Timeline trên Web article-detail.html)
(1, NULL, N'Chờ sơ duyệt', '2026-09-10 08:30:00', 3, N'Tác giả Vũ Thị F nộp bản thảo mới lên hệ thống'),
(1, N'Chờ sơ duyệt', N'Đang phản biện', '2026-09-15 14:15:00', 1, N'Thư ký tòa soạn kiểm tra hình thức đạt yêu cầu và giao 02 chuyên gia phản biện'),
(1, N'Đang phản biện', N'Chờ chỉnh sửa', '2026-10-16 09:00:00', 1, N'Hội đồng gửi nhận xét phản biện yêu cầu tác giả bổ sung'),
(1, N'Chờ chỉnh sửa', N'Đã chấp nhận', '2026-11-05 16:45:00', 1, N'Ban biên tập chấp thuận bản thảo sau khi tác giả hoàn thiện'),
(1, N'Đã chấp nhận', N'Đang chế bản', '2026-11-10 10:00:00', 1, N'Chuyển bài sang bộ phận kỹ thuật đọc bông và chế bản'),
(1, N'Đang chế bản', N'Đã xuất bản', '2026-11-28 08:00:00', 1, N'Công bố chính thức trong Tập 15, Số 42 (2026)'),

-- Lịch sử Bài 8
(8, NULL, N'Chờ sơ duyệt', '2026-11-01 10:00:00', 6, N'Tác giả nộp bài'),
(8, N'Chờ sơ duyệt', N'Đang phản biện', '2026-11-05 15:30:00', 1, N'Phân công 2 chuyên gia phản biện');
GO
