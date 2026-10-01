using HuitJournal.Api.Models;

namespace HuitJournal.Api.Services;

internal static class AuthorReviewFeedback
{
    public static string ForDecision(BaiBao article, LichSuTrangThaiBaiBao decision)
    {
        if (decision.TrangThaiCu != "Đang phản biện" ||
            decision.TrangThaiMoi is not ("Chờ chỉnh sửa" or "Đã chấp nhận" or "Từ chối")) return "";
        var assignments = article.PhanCongPhanBiens.Where(p => p.NgayPhanCong <= decision.NgayChuyen).ToList();
        var round = assignments.Select(p => p.SoVong).DefaultIfEmpty(1).Max();
        return string.Join("\n\n", assignments.Where(p => p.SoVong == round && p.PhieuDanhGia != null &&
            p.PhieuDanhGia.NgayDanhGia <= decision.NgayChuyen).OrderBy(p => p.MaPhanCong)
            .Select((p, index) => $"Nhận xét phản biện {index + 1} · Vòng {round}:\n{p.PhieuDanhGia!.NhanXetChoTacGia}"));
    }
}
