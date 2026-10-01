using System.Net;
using HuitJournal.Api.Models;

namespace HuitJournal.Api.Services;

internal static class AuthorWorkflowNotifications
{
    public static EmailOutbox Publication(string email, string name, BaiBao article, SoTapChi issue, string webBaseUrl)
    {
        var notice = $"Bài báo đã được phát hành trong {issue.TenSo}. Ngày phát hành: {issue.NgayPhatHanh:dd/MM/yyyy}.";
        var text = $"Xin chào {name},\n\nBài #{article.MaBaiBao}: {article.TieuDe}\n{notice}\n\nĐọc bài báo tại: {webBaseUrl.TrimEnd('/')}/article-detail.html?id={article.MaBaiBao}";
        return new EmailOutbox
        {
            LoaiThu = "ThongBaoPhatHanh",
            NguoiNhan = email.Trim().ToLowerInvariant(),
            TieuDe = $"[HUIT Journal] Bài #{article.MaBaiBao} đã phát hành",
            NoiDungText = text,
            NoiDungHtml = $"<p style=\"white-space:pre-wrap\">{WebUtility.HtmlEncode(text)}</p>",
            TrangThai = "Pending",
            TaoLucUtc = DateTime.UtcNow
        };
    }

    // Added to the same EF unit of work as the decision, so a failed save cannot send a decision that was not committed.
    public static EmailOutbox Decision(string email, string name, BaiBao article, string notice, string webBaseUrl)
    {
        var link = webBaseUrl.TrimEnd('/') + "/profile.html";
        var text = $"Xin chào {name},\n\nBài #{article.MaBaiBao}: {article.TieuDe}\nTrạng thái: {article.TrangThai}\n\n{notice}\n\nXem hồ sơ, nhận xét và nộp bản sửa tại: {link}";
        return new EmailOutbox
        {
            LoaiThu = "QuyetDinhBienTap",
            NguoiNhan = email.Trim().ToLowerInvariant(),
            TieuDe = $"[HUIT Journal] Bài #{article.MaBaiBao}: {article.TrangThai}",
            NoiDungText = text,
            NoiDungHtml = $"<p style=\"white-space:pre-wrap\">{WebUtility.HtmlEncode(text)}</p>",
            TrangThai = "Pending",
            TaoLucUtc = DateTime.UtcNow
        };
    }
}
