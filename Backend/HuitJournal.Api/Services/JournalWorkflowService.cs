using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HuitJournal.Api.Configuration;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Infrastructure;
using HuitJournal.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HuitJournal.Api.Services;

public class JournalWorkflowService(QLTapChiKhoaHocContext db, IWebHostEnvironment env,
    IOptions<EmailVerificationSettings> settings, IEmailVerificationService verification, IConfiguration config)
{
    public static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw new ArgumentException(string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private async Task<BaiBao> Owned(int articleId, int userId)
    {
        var article = await db.BaiBaos.FindAsync(articleId) ?? throw new KeyNotFoundException("Không tìm thấy bài.");
        if (article.MaNguoiDung != userId) throw new UnauthorizedAccessException("Chỉ tài khoản gửi bài được thực hiện thao tác này.");
        return article;
    }
    public async Task<bool> CanRead(int articleId, int userId, bool staff) => staff ||
        await db.BaiBaos.AnyAsync(b => b.MaBaiBao == articleId && (b.MaNguoiDung == userId || b.DongTacGias.Any(d => d.MaNguoiDung == userId)));

    public async Task<object> Article(int articleId, int userId, bool staff)
    {
        if (!await CanRead(articleId, userId, staff)) throw new UnauthorizedAccessException();
        var article = await db.BaiBaos.FindAsync(articleId) ?? throw new KeyNotFoundException();
        var records = await db.WorkflowRecords.AsNoTracking().Where(r => r.ArticleId == articleId &&
            (staff || r.Kind == "Submission" || r.Kind == "Proof" || r.Kind == "Withdrawal" || r.Kind == "PublicationNotice"))
            .OrderByDescending(r => r.CreatedUtc).ToListAsync();
        var files = await db.WorkflowFiles.AsNoTracking().Where(f => f.ArticleId == articleId && (staff || f.Kind != "SimilarityReport"))
            .Select(f => new { f.Id, f.Kind, f.Name, f.Size, f.CreatedUtc }).ToListAsync();
        return new { article.MaBaiBao, article.TrangThai, readOnly = article.MaNguoiDung != userId,
            records = records.Select(r => new { r.Id, r.Kind, r.State, r.CreatedUtc, r.UpdatedUtc, data = JsonSerializer.Deserialize<JsonElement>(r.Payload) }), files };
    }

    public async Task<object?> Draft(int userId)
    {
        var record = await db.WorkflowRecords.AsNoTracking().Where(r => r.UserId == userId && r.Kind == "SubmissionDraft" && r.State == "Draft")
            .OrderByDescending(r => r.UpdatedUtc).FirstOrDefaultAsync();
        if (record == null) return null;
        var files = await db.WorkflowFiles.AsNoTracking().Where(f => f.RecordId == record.Id).Select(f => new { f.Id, f.Kind, f.Name, f.Size }).ToListAsync();
        return new { record.Id, record.Payload, version = Convert.ToBase64String(record.RowVersion), record.UpdatedUtc, files };
    }
    public async Task<object> SaveDraft(int userId, SubmissionDraftRequest dto)
    {
        Validate(dto);
        using var json = JsonDocument.Parse(dto.Payload);
        if (json.RootElement.ValueKind != JsonValueKind.Object || dto.Id == Guid.Empty) throw new ArgumentException("Bản nháp không hợp lệ.");
        var files = new List<(string Kind,IFormFile File)>();
        if (dto.Manuscript != null) files.Add(("Manuscript", dto.Manuscript));
        if (dto.Bm02 != null) files.Add(("BM02", dto.Bm02));
        if (dto.Supplements.Count > 0 && !dto.ReplaceSupplements) throw new ArgumentException("Cần xác nhận thay danh sách tệp bổ sung.");
        files.AddRange(dto.Supplements.Select(f => ("Supplement", f)));
        if (files.Count > 7 || files.Sum(f => f.File.Length) > 120L*1024*1024) throw new ArgumentException("Hồ sơ vượt giới hạn dung lượng/số tệp.");
        foreach (var f in files) await WorkflowFileStorage.ValidateAsync(f.File, f.Kind == "Supplement");
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:Draft:" + dto.Id);
        var record = await db.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == dto.Id);
        if (record != null && (record.UserId != userId || record.Kind != "SubmissionDraft" || record.State != "Draft")) throw new UnauthorizedAccessException();
        if (record != null && dto.ExpectedVersion != Convert.ToBase64String(record.RowVersion))
            throw new InvalidOperationException("Bản nháp đã đổi ở máy khác. Tải lại trước khi ghi tiếp.");
        if (record == null) { record = new WorkflowRecord { Id = dto.Id, UserId = userId, Kind = "SubmissionDraft", State = "Draft" }; db.WorkflowRecords.Add(record); }
        record.Payload = dto.Payload; record.UpdatedUtc = DateTime.UtcNow;
        var replacedKinds = files.Select(f => f.Kind).Where(k => k != "Supplement").ToList();
        if (dto.ReplaceSupplements) replacedKinds.Add("Supplement");
        if (dto.ReplaceManuscript) replacedKinds.Add("Manuscript");
        if (dto.ReplaceBm02) replacedKinds.Add("BM02");
        var retainedSize = await db.WorkflowFiles.Where(f => f.RecordId == record.Id && !replacedKinds.Contains(f.Kind)).SumAsync(f => (long?)f.Size) ?? 0;
        if (retainedSize + files.Sum(f => f.File.Length) > 120L * 1024 * 1024) throw new ArgumentException("Tổng tệp bản nháp vượt 120 MB.");
        var old = await db.WorkflowFiles.Where(f => f.RecordId == record.Id && replacedKinds.Contains(f.Kind)).ToListAsync();
        var written = new List<WorkflowFile>();
        try
        {
            db.WorkflowFiles.RemoveRange(old);
            foreach (var f in files) { var saved = await WorkflowFileStorage.SaveAsync(env.ContentRootPath, record, userId, f.Kind, f.File); written.Add(saved); db.WorkflowFiles.Add(saved); }
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        catch { foreach (var f in written) WorkflowFileStorage.Delete(env.ContentRootPath, f); throw; }
        foreach (var f in old) { try { WorkflowFileStorage.Delete(env.ContentRootPath, f); } catch (IOException) { /* Saved draft remains valid; orphan cleanup can retry. */ } }
        return new { success = true, record.Id, version = Convert.ToBase64String(record.RowVersion), record.UpdatedUtc };
    }
    public async Task DeleteDraft(Guid id, int userId)
    {
        var record = await db.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == id && r.UserId == userId && r.Kind == "SubmissionDraft" && r.State == "Draft")
            ?? throw new KeyNotFoundException();
        record.State = "Deleted"; record.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<Guid> Contact(ContactRequest request)
    {
        Validate(request);
        var record = new WorkflowRecord { Kind = "Contact", Payload = JsonSerializer.Serialize(request) };
        db.WorkflowRecords.Add(record);
        var inbox = config["Workflow:EditorialInbox"] ?? settings.Value.SenderEmail;
        db.EmailOutboxes.Add(WorkflowTools.Mail("LienHeToaSoan", inbox, "[HUIT Journal] Yêu cầu liên hệ " + record.Id.ToString("N")[..8],
            $"Người gửi: {request.Name} ({request.Email})\nMã bài: {request.ArticleCode}\nChủ đề: {request.Subject}\n\n{request.Message}"));
        await db.SaveChangesAsync(); return record.Id;
    }

    private sealed record ResetData(string Hash, DateTime ExpiresUtc, int Attempts);
    public async Task<Guid> RequestReset(ResetRequest request)
    {
        Validate(request);
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.NguoiDungs.SingleOrDefaultAsync(u => u.Email.ToLower() == email && u.TrangThai);
        var id = Guid.NewGuid();
        if (user == null) return id;
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:PasswordReset:" + user.MaNguoiDung);
        var recent = await db.WorkflowRecords.FirstOrDefaultAsync(r => r.UserId == user.MaNguoiDung && r.Kind == "PasswordReset" && r.State == "Pending" && r.CreatedUtc > DateTime.UtcNow.AddMinutes(-1));
        if (recent != null) return recent.Id;
        var pending = await db.WorkflowRecords.Where(r => r.UserId == user.MaNguoiDung && r.Kind == "PasswordReset" && r.State == "Pending").ToListAsync();
        foreach (var p in pending) p.State = "Superseded";
        var (code, hash) = verification.GenerateOtp(id);
        db.WorkflowRecords.Add(new WorkflowRecord { Id = id, UserId = user.MaNguoiDung, Kind = "PasswordReset",
            Payload = JsonSerializer.Serialize(new ResetData(hash, DateTime.UtcNow.AddMinutes(10), 0)) });
        db.EmailOutboxes.Add(WorkflowTools.Mail("KhoiPhucMatKhau", email, "[HUIT Journal] Mã khôi phục mật khẩu",
            $"Mã khôi phục: {code}. Mã có hiệu lực 10 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này."));
        await db.SaveChangesAsync(); await tx.CommitAsync(); return id;
    }
    public async Task<bool> ConfirmReset(ResetConfirm dto)
    {
        Validate(dto);
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:ResetCode:" + dto.Id);
        var record = await db.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == dto.Id && r.Kind == "PasswordReset" && r.State == "Pending");
        var data = WorkflowTools.Read<ResetData>(record);
        if (record == null || data == null || data.ExpiresUtc < DateTime.UtcNow || data.Attempts >= 5) return false;
        if (!verification.VerifyHash(dto.Id, dto.Code, data.Hash))
        {
            record.Payload = JsonSerializer.Serialize(data with { Attempts = data.Attempts + 1 });
            await db.SaveChangesAsync(); await tx.CommitAsync(); return false;
        }
        var user = await db.NguoiDungs.SingleAsync(u => u.MaNguoiDung == record.UserId && u.TrangThai);
        user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.Password, 11);
        record.State = "Consumed"; record.UpdatedUtc = DateTime.UtcNow;
        record.Payload = "{}";
        db.WorkflowRecords.Add(new WorkflowRecord { UserId = user.MaNguoiDung, Kind = "AuthVersion", State = "Active" });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return true;
    }

    public async Task<Guid> RequestWithdrawal(int articleId, int userId, ReasonRequest dto)
    {
        Validate(dto);
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:Article:" + articleId);
        var article = await Owned(articleId, userId);
        if (article.TrangThai is not ("Chờ sơ duyệt" or "Chờ sửa hình thức") || await db.PhanCongPhanBiens.AnyAsync(p => p.MaBaiBao == articleId))
            throw new ArgumentException("Chỉ xin rút hồ sơ trước khi phân công phản biện.");
        var previous = await db.WorkflowRecords.FirstOrDefaultAsync(r => r.ArticleId == articleId && r.Kind == "Withdrawal" && r.State == "Pending");
        if (previous != null) return previous.Id;
        var record = new WorkflowRecord { UserId = userId, ArticleId = articleId, Kind = "Withdrawal", Payload = JsonSerializer.Serialize(new { dto.Reason }) };
        db.WorkflowRecords.Add(record);
        db.EmailOutboxes.Add(WorkflowTools.Mail("YeuCauRutBai", config["Workflow:EditorialInbox"] ?? settings.Value.SenderEmail,
            "[HUIT Journal] Yêu cầu rút bài #" + articleId, dto.Reason));
        await db.SaveChangesAsync(); await tx.CommitAsync(); return record.Id;
    }
    public async Task ReviewWithdrawal(Guid id, int editorId, ActionReviewRequest dto)
    {
        Validate(dto);
        await using var tx = await db.Database.BeginTransactionAsync();
        var record = await db.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == id && r.Kind == "Withdrawal") ?? throw new KeyNotFoundException();
        await WorkflowTools.LockAsync(db, "Journal:Article:" + record.ArticleId);
        await db.Entry(record).ReloadAsync();
        if (record.State != "Pending") throw new ArgumentException("Yêu cầu đã được xử lý.");
        var article = await db.BaiBaos.Include(b => b.TacGia).SingleAsync(b => b.MaBaiBao == record.ArticleId);
        if (dto.Approve && (article.TrangThai is not ("Chờ sơ duyệt" or "Chờ sửa hình thức") || await db.PhanCongPhanBiens.AnyAsync(p => p.MaBaiBao == article.MaBaiBao)))
            throw new ArgumentException("Bài đã chuyển sang phản biện; không thể duyệt yêu cầu rút cũ.");
        var reason = JsonDocument.Parse(record.Payload).RootElement.GetProperty("Reason").GetString();
        record.State = dto.Approve ? "Approved" : "Rejected"; record.UpdatedUtc = DateTime.UtcNow;
        record.Payload = JsonSerializer.Serialize(new { Reason = reason, dto.Note, EditorId = editorId });
        if (dto.Approve)
        {
            var old = article.TrangThai; article.TrangThai = "Đã rút"; article.NgayCapNhat = WorkflowTools.VietnamNow;
            db.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao { MaBaiBao = article.MaBaiBao, TrangThaiCu = old, TrangThaiMoi = article.TrangThai,
                NgayChuyen = article.NgayCapNhat, MaNguoiThucHien = editorId, GhiChu = "Duyệt yêu cầu rút hồ sơ của tác giả.", ThongBaoChoTacGia = "Tòa soạn đã duyệt yêu cầu rút hồ sơ. " + dto.Note });
        }
        db.EmailOutboxes.Add(WorkflowTools.Mail("KetQuaRutBai", article.TacGia.Email, "[HUIT Journal] Kết quả yêu cầu rút bài #" + article.MaBaiBao,
            (dto.Approve ? "Yêu cầu đã được duyệt." : "Yêu cầu chưa được duyệt.") + " " + dto.Note));
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task<Guid> SendProof(int articleId, int editorId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:Article:" + articleId);
        var article = await db.BaiBaos.Include(b => b.TacGia).SingleOrDefaultAsync(b => b.MaBaiBao == articleId) ?? throw new KeyNotFoundException();
        if (article.TrangThai is not ("Đã chấp nhận" or "Đang chế bản" or "Sẵn sàng xuất bản")) throw new ArgumentException("Bài chưa đến bước duyệt bản bông.");
        var pdf = await LatestPdf(db, articleId) ?? throw new ArgumentException("Chưa có PDF bản bông.");
        if (UploadStoragePaths.ResolveExistingFile(env.ContentRootPath, pdf.DuongDan) == null) throw new ArgumentException("Không tìm thấy tệp PDF.");
        var previous = await db.WorkflowRecords.Where(r => r.ArticleId == articleId && r.Kind == "Proof" && r.State == "Pending").ToListAsync();
        foreach (var p in previous) p.State = "Superseded";
        var record = new WorkflowRecord { ArticleId = articleId, UserId = article.MaNguoiDung, Kind = "Proof",
            Payload = JsonSerializer.Serialize(new { FileId = pdf.MaThuMuc, EditorId = editorId, Note = "" }) };
        db.WorkflowRecords.Add(record);
        db.EmailOutboxes.Add(WorkflowTools.Mail("DuyetBanBong", article.TacGia.Email, "[HUIT Journal] Duyệt bản bông bài #" + articleId,
            $"Vui lòng đọc PDF và xác nhận hoặc ghi các lỗi cần sửa tại {settings.Value.PublicWebBaseUrl.TrimEnd('/')}/article-workflow.html?id={articleId}"));
        await db.SaveChangesAsync(); await tx.CommitAsync(); return record.Id;
    }
    public async Task ReviewProof(Guid id, int userId, ActionReviewRequest dto)
    {
        Validate(dto);
        if (!dto.Approve && string.IsNullOrWhiteSpace(dto.Note)) throw new ArgumentException("Vui lòng nêu lỗi cần sửa.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var record = await db.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == id && r.Kind == "Proof") ?? throw new KeyNotFoundException();
        await WorkflowTools.LockAsync(db, "Journal:Article:" + record.ArticleId);
        await db.Entry(record).ReloadAsync();
        var article = await Owned(record.ArticleId!.Value, userId);
        if (record.UserId != userId || record.State != "Pending" || article.TrangThai is not ("Đã chấp nhận" or "Đang chế bản" or "Sẵn sàng xuất bản")) throw new ArgumentException("Bản bông không còn chờ xác nhận.");
        var fileId = JsonDocument.Parse(record.Payload).RootElement.GetProperty("FileId").GetInt32();
        var latest = await LatestPdf(db, article.MaBaiBao);
        if (latest?.MaThuMuc != fileId) throw new ArgumentException("PDF đã được thay; tòa soạn cần gửi bản bông mới.");
        record.State = dto.Approve ? "Approved" : "ChangesRequested"; record.UpdatedUtc = DateTime.UtcNow;
        record.Payload = JsonSerializer.Serialize(new { FileId = fileId, dto.Note, ApprovedBy = userId });
        var oldStatus = article.TrangThai;
        if (!dto.Approve && article.TrangThai == "Sẵn sàng xuất bản") article.TrangThai = "Đang chế bản";
        article.NgayCapNhat = WorkflowTools.VietnamNow;
        db.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao { MaBaiBao = article.MaBaiBao, TrangThaiCu = oldStatus, TrangThaiMoi = article.TrangThai,
            NgayChuyen = article.NgayCapNhat, MaNguoiThucHien = userId, GhiChu = "Tác giả phản hồi PDF bản bông.",
            ThongBaoChoTacGia = (dto.Approve ? "Đã xác nhận bản bông." : "Đã gửi yêu cầu sửa bản bông. ") + dto.Note });
        db.EmailOutboxes.Add(WorkflowTools.Mail("PhanHoiBanBong", config["Workflow:EditorialInbox"] ?? settings.Value.SenderEmail,
            "[HUIT Journal] Phản hồi bản bông bài #" + article.MaBaiBao, record.State + "\n" + dto.Note));
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public static Task<ThuMucBaiBao?> LatestPdf(QLTapChiKhoaHocContext db, int articleId) => db.ThuMucBaiBaos
        .Where(f => f.MaBaiBao == articleId && (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản"))
        .OrderByDescending(f => f.NgayTaiLen).ThenByDescending(f => f.MaThuMuc).FirstOrDefaultAsync();
    public static async Task<bool> ProofApproved(QLTapChiKhoaHocContext db, int articleId)
    {
        var latest = await LatestPdf(db, articleId);
        var record = await db.WorkflowRecords.AsNoTracking().Where(r => r.ArticleId == articleId && r.Kind == "Proof")
            .OrderByDescending(r => r.CreatedUtc).FirstOrDefaultAsync();
        return latest != null && record?.State == "Approved" && JsonDocument.Parse(record.Payload).RootElement.GetProperty("FileId").GetInt32() == latest.MaThuMuc;
    }

    public async Task Screening(int articleId, int editorId, ScreeningRequest dto)
    {
        Validate(dto);
        if (dto.Report == null) throw new ArgumentException("Vui lòng đính kèm báo cáo kiểm tra trùng lặp.");
        await WorkflowFileStorage.ValidateAsync(dto.Report);
        await using var tx = await db.Database.BeginTransactionAsync();
        await WorkflowTools.LockAsync(db, "Journal:Article:" + articleId);
        var article = await db.BaiBaos.FindAsync(articleId) ?? throw new KeyNotFoundException();
        var source = await db.ThuMucBaiBaos.Where(f => f.MaBaiBao == articleId && (f.LoaiThuMuc == "Bản thảo gốc" || f.LoaiThuMuc == "Bản chỉnh sửa")).OrderByDescending(f => f.NgayTaiLen).ThenByDescending(f => f.MaThuMuc).FirstOrDefaultAsync() ?? throw new ArgumentException("Chưa có bản thảo để kiểm tra.");
        if (article.TrangThai is not ("Chờ sơ duyệt" or "Chờ sửa hình thức" or "Chờ quyết định")) throw new ArgumentException("Bài không ở bước sơ duyệt.");
        var record = new WorkflowRecord { UserId = editorId, ArticleId = articleId, Kind = "Screening", State = dto.Approve ? "Approved" : "ChangesRequested",
            Payload = JsonSerializer.Serialize(new { dto.SimilarityPercent, dto.Note, EditorId = editorId, FileId = source.MaThuMuc }) };
        var file = await WorkflowFileStorage.SaveAsync(env.ContentRootPath, record, editorId, "SimilarityReport", dto.Report);
        try {
            db.WorkflowRecords.Add(record); db.WorkflowFiles.Add(file);
            var old = article.TrangThai;
            if (!dto.Approve) article.TrangThai = old == "Chờ quyết định" ? "Chờ chỉnh sửa" : "Chờ sửa hình thức";
            article.NgayCapNhat = WorkflowTools.VietnamNow;
            db.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao { MaBaiBao = articleId, MaNguoiThucHien = editorId,
                TrangThaiCu = old, TrangThaiMoi = article.TrangThai, NgayChuyen = article.NgayCapNhat,
                GhiChu = "Đã lưu kết quả kiểm tra trùng lặp và báo cáo nội bộ.",
                ThongBaoChoTacGia = dto.Approve ? "Tòa soạn đã hoàn tất sơ duyệt bản thảo." : dto.Note });
            if (!dto.Approve) {
                var author = await db.NguoiDungs.FindAsync(article.MaNguoiDung);
                db.EmailOutboxes.Add(AuthorWorkflowNotifications.Decision(author!.Email, author.HoTen, article, dto.Note, settings.Value.PublicWebBaseUrl));
            }
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        catch { WorkflowFileStorage.Delete(env.ContentRootPath, file); throw; }
    }
}
