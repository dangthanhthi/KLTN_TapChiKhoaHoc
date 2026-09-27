-- Chạy trên từng CSDL mục tiêu sau khi sao lưu. Không đổi dữ liệu bài báo hiện có.
-- Cho phép trạng thái rút hồ sơ trước phản biện; giữ toàn bộ trạng thái cũ.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

IF DB_NAME() NOT IN (N'QL_TapChiKhoaHoc', N'QL_TapChiKhoaHoc_Test')
    THROW 50000, N'Sai cơ sở dữ liệu mục tiêu.', 1;
IF OBJECT_ID(N'dbo.BaiBao', N'U') IS NULL
    THROW 50001, N'Không tìm thấy bảng dbo.BaiBao.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID(N'dbo.BaiBao') AND name = N'CHK_BaiBao_TrangThai')
    THROW 50002, N'Không tìm thấy ràng buộc trạng thái dự kiến; cần kiểm tra schema.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID(N'dbo.BaiBao') AND name = N'CHK_BaiBao_TrangThai'
                 AND definition LIKE N'%Đã rút%')
BEGIN
    BEGIN TRANSACTION;
    ALTER TABLE dbo.BaiBao DROP CONSTRAINT CHK_BaiBao_TrangThai;
    ALTER TABLE dbo.BaiBao WITH CHECK ADD CONSTRAINT CHK_BaiBao_TrangThai CHECK (TrangThai IN (
        N'Chờ sơ duyệt', N'Chờ sửa hình thức', N'Đang phản biện', N'Chờ chỉnh sửa',
        N'Chờ quyết định', N'Đã chấp nhận', N'Đang chế bản', N'Sẵn sàng xuất bản',
        N'Đã xuất bản', N'Đã rút', N'Từ chối'
    ));
    COMMIT TRANSACTION;
END;

SELECT DB_NAME() AS DatabaseName, name AS ConstraintName, definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.BaiBao') AND name = N'CHK_BaiBao_TrangThai';
