using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HuitJournal.Api.Services;

public partial class BaiBaoService
{
    private static List<T> ParseValidated<T>(string? json)
    {
        var items = string.IsNullOrWhiteSpace(json) ? new List<T>() :
            JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new ArgumentException("Danh sách tác giả/chuyên gia không hợp lệ.");
        if (items.Count > 50) throw new ArgumentException("Mỗi danh sách tối đa 50 người.");
        foreach (var item in items)
        {
            if (item == null) throw new ArgumentException("Danh sách có dòng trống.");
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(item, new ValidationContext(item), errors, true))
                throw new ArgumentException("Thông tin tác giả/chuyên gia không hợp lệ: " + string.Join(" ", errors.Select(e => e.ErrorMessage)));
        }
        return items;
    }

    private async Task<(bool Success, string Message, int? MaBaiBao, string? MaDinhDanh)> SubmitValidatedAsync(int owner, BaiBaoSubmitDto dto)
    {
        var written = new List<WorkflowFile>();
        IDbContextTransaction? transaction = null;
        const string savepoint = "JournalSubmission";
        var outer = _context.Database.CurrentTransaction;
        bool savepointCreated = false;
        try
        {
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
                return (false, string.Join(" ", errors.Select(e => e.ErrorMessage)), null, null);
            if (dto.SubmissionId == Guid.Empty) return (false, "Thiếu mã yêu cầu nộp bài. Vui lòng tải lại biểu mẫu.", null, null);
            if (!dto.Consent || dto.ConsentVersion != WorkflowTools.ConsentVersion)
                return (false, "Vui lòng xác nhận bản cam đoan hiện hành trước khi nộp bài.", null, null);
            if (dto.TapTinBanThao == null) return (false, "Vui lòng tải bản thảo toàn văn.", null, null);
            if (string.IsNullOrWhiteSpace(dto.LoaiBai) || string.IsNullOrWhiteSpace(dto.NgonNgu))
                return (false, "Vui lòng chọn loại bài và ngôn ngữ.", null, null);
            var authors = ParseValidated<DongTacGiaSubmitDto>(dto.DongTacGiaJson);
            var reviewers = ParseValidated<PhanBienDeXuatSubmitDto>(dto.PhanBienDeXuatJson);
            var user = await _context.NguoiDungs.Include(u => u.NguoiDungChuyenMons).SingleOrDefaultAsync(u => u.MaNguoiDung == owner && u.TrangThai);
            if (user == null) return (false, "Tài khoản không hoạt động.", null, null);
            if (!await _context.ChuyenNganhs.AnyAsync(c => c.MaChuyenNganh == dto.MaChuyenNganh))
                return (false, "Chuyên ngành không tồn tại.", null, null);
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { user.Email.Trim() };
            foreach (var author in authors)
                if (!emails.Add(author.Email.Trim())) return (false, "Email các tác giả không được trùng nhau.", null, null);
            var files = new List<(string Kind, IFormFile File)> { ("Manuscript", dto.TapTinBanThao) };
            if (dto.FileBm02 != null) files.Add(("BM02", dto.FileBm02));
            files.AddRange(dto.Supplements.Select(f => ("Supplement", f)));
            if (dto.Supplements.Count > 5 || files.Sum(f => f.File.Length) > 120L * 1024 * 1024)
                return (false, "Tối đa 5 phụ lục, tổng dung lượng hồ sơ tối đa 120 MB.", null, null);
            var fingerprints = new List<object>();
            foreach (var (kind, file) in files)
            {
                await WorkflowFileStorage.ValidateAsync(file, kind == "Supplement");
                await using var stream = file.OpenReadStream();
                fingerprints.Add(new { kind, name = file.FileName, hash = Convert.ToHexString(await SHA256.HashDataAsync(stream)) });
            }
            var metadata = new { dto.LoaiBai, dto.NgonNgu, dto.TuKhoaTiengAnh, dto.ConsentVersion, ConsentUtc = DateTime.UtcNow };
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            { dto.TieuDe, dto.TieuDeTiengAnh, dto.TomTat, dto.TomTatTiengAnh, dto.TuKhoa, dto.MaChuyenNganh,
                dto.LoaiBai, dto.NgonNgu, dto.TuKhoaTiengAnh, dto.ConsentVersion, authors, reviewers, fingerprints }))));
            if (outer == null) transaction = await _context.Database.BeginTransactionAsync();
            else { await outer.CreateSavepointAsync(savepoint); savepointCreated = true; }
            await WorkflowTools.LockAsync(_context, "Journal:Submission:" + dto.SubmissionId);
            var previous = await _context.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == dto.SubmissionId);
            if (previous != null)
            {
                if (previous.UserId != owner || previous.Kind != "Submission" ||
                    JsonDocument.Parse(previous.Payload).RootElement.GetProperty("Fingerprint").GetString() != fingerprint)
                    throw new ArgumentException("Mã yêu cầu đã được dùng cho hồ sơ khác. Tạo bản nháp mới để gửi bài khác.");
                var existing = await _context.BaiBaos.SingleAsync(b => b.MaBaiBao == previous.ArticleId);
                if (transaction != null) await transaction.CommitAsync();
                return (true, "Hồ sơ đã được tiếp nhận trước đó.", existing.MaBaiBao, $"JST-{existing.NgayGui.Year}-SUB{existing.MaBaiBao:D4}");
            }
            if (!user.NguoiDungChuyenMons.Any(c => c.MaChuyenNganh == dto.MaChuyenNganh))
            {
                _context.NguoiDungChuyenMons.Add(new NguoiDungChuyenMon { MaNguoiDung = owner, MaChuyenNganh = dto.MaChuyenNganh, NgayDangKy = WorkflowTools.VietnamNow });
                await _context.SaveChangesAsync();
            }
            var article = new BaiBao { MaNguoiDung = owner, MaChuyenNganh = dto.MaChuyenNganh,
                TieuDe = dto.TieuDe.Trim(), TieuDeTiengAnh = dto.TieuDeTiengAnh?.Trim(), TomTat = dto.TomTat.Trim(),
                TomTatTiengAnh = dto.TomTatTiengAnh?.Trim(), TuKhoa = dto.TuKhoa.Trim(), TrangThai = "Chờ sơ duyệt",
                NgayGui = WorkflowTools.VietnamNow, NgayCapNhat = WorkflowTools.VietnamNow };
            _context.BaiBaos.Add(article);
            await _context.SaveChangesAsync();
            var record = new WorkflowRecord { Id = dto.SubmissionId, UserId = owner, ArticleId = article.MaBaiBao, Kind = "Submission", State = "Submitted",
                Payload = JsonSerializer.Serialize(new { Fingerprint = fingerprint, Metadata = metadata }) };
            _context.WorkflowRecords.Add(record);
            foreach (var (kind, file) in files)
            {
                var saved = await WorkflowFileStorage.SaveAsync(_env.ContentRootPath, record, owner, kind, file);
                written.Add(saved);
                if (kind == "Manuscript") _context.ThuMucBaiBaos.Add(new ThuMucBaiBao { MaBaiBao = article.MaBaiBao,
                    TenThuMuc = saved.Name, DuongDan = saved.Path, KichThuoc = saved.Size, LoaiThuMuc = "Bản thảo gốc", SoVong = 1, NgayTaiLen = WorkflowTools.VietnamNow });
                else _context.WorkflowFiles.Add(saved);
            }
            int order = 1;
            foreach (var author in authors)
            {
                var email = author.Email.Trim().ToLowerInvariant();
                var linkedId = await _context.NguoiDungs.Where(u => u.Email.ToLower() == email && u.TrangThai).Select(u => (int?)u.MaNguoiDung).FirstOrDefaultAsync();
                _context.DongTacGias.Add(new DongTacGia { MaBaiBao = article.MaBaiBao, HoTen = author.HoTen.Trim(), Email = email,
                    DonVi = author.DonVi?.Trim(), MaORCID = author.MaORCID?.Trim(), LaTacGiaLienHe = author.LaTacGiaLienHe, ThuTu = order++, MaNguoiDung = linkedId });
            }
            foreach (var reviewer in reviewers)
            {
                var email = reviewer.Email.Trim().ToLowerInvariant();
                var linked = await _context.NguoiDungs.Where(u => u.Email.ToLower() == email && u.TrangThai).Select(u => (int?)u.MaNguoiDung).FirstOrDefaultAsync();
                _context.PhanBienDeXuats.Add(new PhanBienDeXuat { MaBaiBao = article.MaBaiBao, HoTen = reviewer.HoTen.Trim(), Email = email,
                    DonVi = reviewer.DonVi?.Trim(), LinhVuc = reviewer.LinhVuc?.Trim(), MaNguoiDung = linked, LaChuyenGiaHeThong = linked.HasValue, NgayTao = WorkflowTools.VietnamNow });
            }
            _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao { MaBaiBao = article.MaBaiBao, TrangThaiMoi = article.TrangThai,
                MaNguoiThucHien = owner, NgayChuyen = WorkflowTools.VietnamNow, GhiChu = "Tác giả nộp bài và xác nhận cam đoan " + dto.ConsentVersion });
            if (dto.DraftId.HasValue)
            {
                var draft = await _context.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == dto.DraftId && r.UserId == owner && r.Kind == "SubmissionDraft");
                if (draft != null) { draft.State = "Submitted"; draft.ArticleId = article.MaBaiBao; draft.UpdatedUtc = DateTime.UtcNow; }
            }
            _context.EmailOutboxes.Add(WorkflowTools.Mail("TiepNhanBanThao", user.Email, "[HUIT Journal] Đã tiếp nhận bài #" + article.MaBaiBao,
                $"Hệ thống đã tiếp nhận bản thảo: {article.TieuDe}. Bạn có thể theo dõi hồ sơ tại Cổng thông tin Tạp chí."));
            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
            return (true, "Nộp bản thảo thành công!", article.MaBaiBao, $"JST-{article.NgayGui.Year}-SUB{article.MaBaiBao:D4}");
        }
        catch (Exception ex)
        {
            if (transaction != null) await transaction.RollbackAsync();
            else if (outer != null && savepointCreated) await outer.RollbackToSavepointAsync(savepoint);
            foreach (var file in written) WorkflowFileStorage.Delete(_env.ContentRootPath, file);
            _context.ChangeTracker.Clear();
            return (false, ex is ArgumentException ? ex.Message : ex is JsonException ? "Dữ liệu tác giả/chuyên gia không đúng định dạng." : "Chưa thể lưu hồ sơ. Không có bài mới được tiếp nhận; vui lòng thử lại.", null, null);
        }
        finally { if (transaction != null) await transaction.DisposeAsync(); }
    }
}
