using Microsoft.EntityFrameworkCore;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using HuitJournal.Api.Infrastructure;
namespace HuitJournal.Api.Services;
public class IssuePublicationService(QLTapChiKhoaHocContext context, IWebHostEnvironment env, IConfiguration configuration)
{
    private readonly QLTapChiKhoaHocContext _context = context;
    private readonly IWebHostEnvironment _env = env;
    private readonly string _publicWebBaseUrl = configuration["EmailVerification:PublicWebBaseUrl"] ?? "https://kltn-tap-chi-khoa-hoc.vercel.app";
    public async Task<(int Status, object Result)> Publish(int id, int editorId, bool validateOnly = false)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await WorkflowTools.LockAsync(_context, "Journal:PublishIssue:" + id);
        var issue = await _context.SoTapChis.FirstOrDefaultAsync(s => s.MaSoTapChi == id);
        if (issue == null) return (404, new { message = "Không tìm thấy số tạp chí." });
        if (issue.TrangThai == "Đã xuất bản" || issue.TrangThai == "Đã phát hành")
            return (200, new { success = true, message = "Số tạp chí đã được phát hành trước đó." });

        if (issue.NgayPhatHanh.HasValue && issue.NgayPhatHanh.Value > WorkflowTools.VietnamNow && !validateOnly)
            return (400, new { message = "Chưa đến ngày phát hành đã đặt. Không thể công bố số tạp chí trước thời điểm này." });

        var articles = await _context.BaiBaos.Include(b => b.ThuMucBaiBaos)
            .Include(b => b.TacGia).Include(b => b.DongTacGias).ThenInclude(d => d.NguoiDung)
            .Where(b => b.MaSoTapChi == id).ToListAsync();
        if (articles.Count == 0)
            return (400, new { message = "Không thể phát hành số tạp chí chưa có bài báo." });

        foreach (var article in articles)
        {
            if (article.TrangThai != "Sẵn sàng xuất bản")
                return (400, new { message = $"Bài #{article.MaBaiBao} chưa được duyệt ở trạng thái Sẵn sàng xuất bản." });
            if (!article.TrangBatDau.HasValue || !article.TrangKetThuc.HasValue || article.TrangBatDau < 1 || article.TrangKetThuc < article.TrangBatDau)
                return (400, new { message = $"Bài #{article.MaBaiBao} chưa có khoảng trang hợp lệ." });
            if (!await JournalWorkflowService.ProofApproved(_context, article.MaBaiBao))
                return (400, new { message = $"Bài #{article.MaBaiBao} chưa được tác giả xác nhận PDF bản bông mới nhất." });
            var latestPdf = await JournalWorkflowService.LatestPdf(_context, article.MaBaiBao);
            if (latestPdf == null || !HasPhysicalPdf(latestPdf))
                return (400, new { message = $"Bài #{article.MaBaiBao} chưa có PDF thành phẩm thực tế trên máy chủ." });
        }

        var ordered = articles.OrderBy(b => b.TrangBatDau).ToList();
        for (var i = 1; i < ordered.Count; i++)
            if (ordered[i].TrangBatDau <= ordered[i - 1].TrangKetThuc)
                return (400, new { message = $"Khoảng trang bài #{ordered[i].MaBaiBao} trùng bài #{ordered[i - 1].MaBaiBao}." });
        if (validateOnly) return (200, new { success = true, message = "Hồ sơ số tạp chí đủ điều kiện phát hành." });
        var now = WorkflowTools.VietnamNow;
        issue.TrangThai = "Đã xuất bản";
        issue.NgayPhatHanh ??= now;
        foreach (var article in articles)
        {
            var oldStatus = article.TrangThai;
            article.TrangThai = "Đã xuất bản";
            article.NgayCapNhat = now;
            _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
            {
                MaBaiBao = article.MaBaiBao,
                TrangThaiCu = oldStatus,
                TrangThaiMoi = "Đã xuất bản",
                NgayChuyen = now,
                MaNguoiThucHien = editorId,
                GhiChu = "Phát hành số tạp chí.",
                ThongBaoChoTacGia = $"Phát hành số tạp chí {issue.TenSo}. Ngày phát hành: {issue.NgayPhatHanh:dd/MM/yyyy}."
            });
            var recipients = article.DongTacGias.Where(d => d.NguoiDung?.TrangThai == true)
                .Select(d => d.NguoiDung!)
                .Append(article.TacGia)
                .Where(u => u.TrangThai && !string.IsNullOrWhiteSpace(u.Email))
                .DistinctBy(u => u.Email.Trim().ToLowerInvariant());
            foreach (var recipient in recipients)
                _context.EmailOutboxes.Add(AuthorWorkflowNotifications.Publication(
                    recipient.Email, recipient.HoTen, article, issue, _publicWebBaseUrl));
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return (200, new { success = true, message = $"Đã phát hành {issue.TenSo} với {articles.Count} bài báo." });
    }

    private bool HasPhysicalPdf(ThuMucBaiBao file)
    {
        if ((file.LoaiThuMuc != "PDF thành phẩm" && file.LoaiThuMuc != "PDF Xuất bản") ||
            !file.TenThuMuc.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return false;
        return UploadStoragePaths.ResolveExistingFile(_env.ContentRootPath, file.DuongDan) != null;
    }
}
