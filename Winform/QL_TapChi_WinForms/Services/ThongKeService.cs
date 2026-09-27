using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace QL_TapChi_WinForms.Services
{
    public class MetricSummary
    {
        public int TongBaiBao { get; set; }
        public int ChoSoDuyet { get; set; }
        public int DangPhanBien { get; set; }
        public int CanChinhSua { get; set; }
        public int ChoQuyetDinh { get; set; }
        public int DaXuatBan { get; set; }
        public int TongNguoiDung { get; set; }
        public int TongPhanBien { get; set; }
        public int TongSoTapChi { get; set; }
    }

    public class ThongKeService
    {
        public static MetricSummary GetDashboardMetrics() =>
            JournalApiClient.Read<MetricSummary>("/api/desktop-editorial/metrics") ?? new();

        public static Dictionary<string, int> GetCountByTrangThai() =>
            JournalApiClient.Read<Dictionary<string, int>>("/api/desktop-editorial/metrics/by-status") ?? new();

        public static Dictionary<string, int> GetCountByChuyenNganh() =>
            JournalApiClient.Read<Dictionary<string, int>>("/api/desktop-editorial/metrics/by-category") ?? new();

        public static bool ExportToCsv(string filePath, DataTable dt, string tieuDeBaoCao)
        {
            try
            {
                using var sw = new StreamWriter(filePath, false, new UTF8Encoding(true));
                sw.WriteLine($"# {tieuDeBaoCao.ToUpperInvariant()}");
                sw.WriteLine($"# Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                sw.WriteLine("# Đơn vị: Tạp chí Khoa học Đại học Công Thương TP.HCM");
                sw.WriteLine();
                var headers = new List<string>();
                foreach (DataColumn col in dt.Columns) headers.Add(Csv(col.ColumnName));
                sw.WriteLine(string.Join(",", headers));
                foreach (DataRow row in dt.Rows)
                {
                    var fields = new List<string>();
                    foreach (DataColumn col in dt.Columns) fields.Add(Csv(row[col]?.ToString() ?? ""));
                    sw.WriteLine(string.Join(",", fields));
                }
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
