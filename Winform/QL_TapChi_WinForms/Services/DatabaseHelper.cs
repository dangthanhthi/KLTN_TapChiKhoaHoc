using System;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace QL_TapChi_WinForms.Services
{
    public static class DatabaseHelper
    {
        private static bool? _isConnected = null;

        // WinForms còn một số màn hình đọc SQL trực tiếp. Chỉ cho vào bàn làm việc
        // khi kết nối đó trỏ tới đúng SQL Server/CSDL mà Backend API đang dùng.
        public static bool MatchBackendDatabase(out string error)
        {
            error = "";
            _isConnected = false;
            var response = JournalApiClient.Get("/api/system/environment");
            if (response is not JsonElement json || json.ValueKind != JsonValueKind.Object ||
                !json.TryGetProperty("serverName", out var serverValue) ||
                !json.TryGetProperty("databaseName", out var databaseValue) ||
                serverValue.ValueKind != JsonValueKind.String || databaseValue.ValueKind != JsonValueKind.String)
            {
                error = JournalApiClient.LastError ?? "Không xác nhận được CSDL của Backend API. Hãy chạy đúng phiên bản Backend trước khi mở WinForms.";
                return false;
            }

            var backendServer = serverValue.GetString();
            var backendDatabase = databaseValue.GetString();
            foreach (var connectionString in AppConfig.ConnectionStrings)
            {
                try
                {
                    var probe = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 5 };
                    using var connection = new SqlConnection(probe.ConnectionString);
                    connection.Open();
                    using var command = new SqlCommand(
                        "SELECT CONVERT(nvarchar(256), SERVERPROPERTY('ServerName')), DB_NAME()", connection);
                    using var reader = command.ExecuteReader();
                    if (!reader.Read()) continue;
                    var desktopServer = reader.GetString(0);
                    var desktopDatabase = reader.GetString(1);
                    if (!string.Equals(desktopServer, backendServer, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(desktopDatabase, backendDatabase, StringComparison.OrdinalIgnoreCase))
                        continue;

                    AppConfig.CurrentConnectionString = connectionString;
                    _isConnected = true;
                    return true;
                }
                catch (Exception ex) when (ex is SqlException or ArgumentException or InvalidOperationException)
                {
                    // Thử instance SQL tiếp theo; không chấp nhận một DB khác chỉ vì cùng tên.
                }
            }

            error = $"WinForms không kết nối được đúng CSDL của Backend ({backendServer}/{backendDatabase}). " +
                    "Hãy đặt HUIT_JOURNAL_DB_CONNECTION trỏ tới cùng SQL Server và CSDL rồi đăng nhập lại.";
            return false;
        }

        public static bool TestConnection()
        {
            if (_isConnected.HasValue && _isConnected.Value) return true;

            foreach (var connStr in AppConfig.ConnectionStrings)
            {
                try
                {
                    using var conn = new SqlConnection(connStr);
                    conn.Open();
                    AppConfig.CurrentConnectionString = connStr;
                    _isConnected = true;
                    return true;
                }
                catch
                {
                    // Thử chuỗi tiếp theo
                }
            }

            _isConnected = false;
            return false;
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(AppConfig.CurrentConnectionString);
        }

        public static DataTable ExecuteQuery(string sql, params SqlParameter[] parameters)
        {
            var dt = new DataTable();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }
            return cmd.ExecuteScalar();
        }
    }
}
