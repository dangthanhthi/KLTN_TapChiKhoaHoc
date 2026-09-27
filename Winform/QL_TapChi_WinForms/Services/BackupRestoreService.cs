using System;
using System.IO;
using Microsoft.Data.SqlClient;

namespace QL_TapChi_WinForms.Services
{
    public class BackupRestoreService
    {
        public static (bool Success, string Message) BackupDatabase(string backupFolder)
        {
            if (AuthService.CurrentUser?.DanhSachVaiTro.Contains("Quản trị hệ thống") != true)
                return (false, "Chỉ quản trị hệ thống được sao lưu dữ liệu.");
            if (!DatabaseHelper.MatchBackendDatabase(out var databaseError))
                return (false, databaseError ?? "Cơ sở dữ liệu WinForms và API không trùng nhau.");
            try
            {
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                var builder = new SqlConnectionStringBuilder(AppConfig.CurrentConnectionString);
                string dbName = string.IsNullOrEmpty(builder.InitialCatalog) ? "QL_TapChiKhoaHoc" : builder.InitialCatalog;

                string fileName = $"{dbName}_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string fullPath = Path.Combine(backupFolder, fileName);

                if (!DatabaseHelper.TestConnection())
                {
                    return (false, "Không kết nối được SQL Server. Chưa tạo bản sao lưu.");
                }

                string safePath = fullPath.Replace("'", "''");
                string sql = $@"
                    BACKUP DATABASE [{dbName}] 
                    TO DISK = '{safePath}' 
                    WITH FORMAT, MEDIANAME = 'QL_TapChi_Backup', NAME = 'Full Backup of {dbName}';";

                DatabaseHelper.ExecuteNonQuery(sql);
                return (true, $"Sao lưu cơ sở dữ liệu thành công vào tệp:\n{fullPath}");
            }
            catch (SqlException sex) when (sex.Number == 3201 || sex.Message.Contains("Operating system error 5"))
            {
                return (false, $"Lỗi phân quyền SQL Server (Hệ điều hành từ chối quyền ghi - Error 5):\nDịch vụ SQL Server không có quyền ghi vào thư mục được chọn.\n\nGợi ý: Chọn thư mục dùng chung (ví dụ: C:\\SQLBackups) hoặc cấp quyền Write cho 'NT SERVICE\\MSSQLSERVER' / 'Everyone' trên thư mục này.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi sao lưu dữ liệu: {ex.Message}");
            }
        }

        public static (bool Success, string Message) RestoreDatabase(string bakFilePath)
        {
            if (AuthService.CurrentUser?.DanhSachVaiTro.Contains("Quản trị hệ thống") != true)
                return (false, "Chỉ quản trị hệ thống được phục hồi dữ liệu.");
            if (!DatabaseHelper.MatchBackendDatabase(out var databaseError))
                return (false, databaseError ?? "Cơ sở dữ liệu WinForms và API không trùng nhau.");
            string dbName = "QL_TapChiKhoaHoc";
            SqlConnectionStringBuilder? masterBuilder = null;

            try
            {
                if (!File.Exists(bakFilePath))
                {
                    return (false, "Không tìm thấy tệp bản sao lưu .bak đã chọn.");
                }

                var builder = new SqlConnectionStringBuilder(AppConfig.CurrentConnectionString);
                dbName = string.IsNullOrEmpty(builder.InitialCatalog) ? "QL_TapChiKhoaHoc" : builder.InitialCatalog;

                if (!DatabaseHelper.TestConnection())
                {
                    return (false, "Không kết nối được SQL Server. Chưa phục hồi dữ liệu.");
                }

                // Cần chuyển context sang master để ngắt mọi kết nối hiện tại tới CSDL cần restore
                masterBuilder = new SqlConnectionStringBuilder(AppConfig.CurrentConnectionString)
                {
                    InitialCatalog = "master"
                };

                using var conn = new SqlConnection(masterBuilder.ConnectionString);
                conn.Open();

                string safeBakPath = bakFilePath.Replace("'", "''");
                string sql = $@"
                    BEGIN TRY
                        ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        RESTORE DATABASE [{dbName}] FROM DISK = '{safeBakPath}' WITH REPLACE;
                        ALTER DATABASE [{dbName}] SET MULTI_USER;
                    END TRY
                    BEGIN CATCH
                        IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{dbName}')
                        BEGIN
                            ALTER DATABASE [{dbName}] SET MULTI_USER;
                        END
                        THROW;
                    END CATCH";

                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 120; // 2 phút timeout cho thao tác restore
                cmd.ExecuteNonQuery();

                return (true, $"Phục hồi cơ sở dữ liệu '{dbName}' thành công từ tệp sao lưu!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (masterBuilder != null)
                    {
                        using var rescueConn = new SqlConnection(masterBuilder.ConnectionString);
                        rescueConn.Open();
                        string rescueSql = $@"
                            IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{dbName}')
                            BEGIN
                                DECLARE @state NVARCHAR(60) = (SELECT state_desc FROM sys.databases WHERE name = '{dbName}');
                                IF @state = 'RESTORING'
                                BEGIN
                                    RESTORE DATABASE [{dbName}] WITH RECOVERY;
                                END
                                ALTER DATABASE [{dbName}] SET MULTI_USER WITH ROLLBACK IMMEDIATE;
                            END";
                        using var rescueCmd = new SqlCommand(rescueSql, rescueConn);
                        rescueCmd.ExecuteNonQuery();
                    }
                }
                catch { }
                finally
                {
                    SqlConnection.ClearAllPools();
                }
                return (false, $"Lỗi khi phục hồi dữ liệu: {ex.Message}");
            }
        }
    }
}
