using System;
using System.Collections.Generic;

namespace QL_TapChi_WinForms.Services
{
    public sealed class RoleDistributionItem
    {
        public string TenVaiTro { get; set; } = "";
        public int SoLuong { get; set; }
    }

    public sealed class RecentHistoryItem
    {
        public DateTime NgayChuyen { get; set; }
        public string HoTen { get; set; } = "Hệ thống";
        public string TrangThaiMoi { get; set; } = "";
        public string? GhiChu { get; set; }
    }

    public static class AdminDashboardService
    {
        public static List<RoleDistributionItem> GetRoles() =>
            JournalApiClient.Read<List<RoleDistributionItem>>("/api/desktop-admin/dashboard/role-distribution") ?? new();

        public static List<RecentHistoryItem> GetRecentHistory() =>
            JournalApiClient.Read<List<RecentHistoryItem>>("/api/desktop-admin/dashboard/recent-history") ?? new();
    }
}
