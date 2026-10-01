using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;
using HuitJournal.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text;
using HuitJournal.Api.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using HuitJournal.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

var options = new DbContextOptionsBuilder<QLTapChiKhoaHocContext>()
    .UseSqlServer("Server=.;Database=QL_TapChiKhoaHoc_Test;Integrated Security=True;TrustServerCertificate=True;")
    .Options;
await using var db = new QLTapChiKhoaHocContext(options);
await db.Database.OpenConnectionAsync();
var connection = db.Database.GetDbConnection();
if (connection.Database != "QL_TapChiKhoaHoc_Test" || connection.DataSource != ".")
    throw new InvalidOperationException("STOP: only local QL_TapChiKhoaHoc_Test is allowed.");
await db.Database.CloseConnectionAsync();

var baselineArticles = await db.BaiBaos.CountAsync();
var baselineAssignments = await db.PhanCongPhanBiens.CountAsync();
var baselineDrafts = await db.PhieuDanhGiaBanNhaps.CountAsync();
var categoryId = 1;
var authorId = await db.NguoiDungs.AsNoTracking()
    .Where(u => u.TrangThai && !u.NguoiDungVaiTros.Any(r => r.VaiTro.TenVaiTro == "Chuyên gia phản biện"))
    .Select(u => u.MaNguoiDung).FirstAsync();
var editorId = await db.NguoiDungVaiTros.AsNoTracking()
    .Where(r => r.VaiTro.TenVaiTro == "Ban biên tập" || r.VaiTro.TenVaiTro == "Quản trị hệ thống")
    .Select(r => r.MaNguoiDung).FirstAsync();
var reviewers = await db.NguoiDungs.AsNoTracking()
    .Where(u => u.TrangThai && u.MaNguoiDung != authorId &&
        u.NguoiDungVaiTros.Any(r => r.VaiTro.TenVaiTro == "Chuyên gia phản biện") &&
        u.NguoiDungChuyenMons.Any(c => c.MaChuyenNganh == categoryId))
    .OrderBy(u => u.MaNguoiDung).Select(u => u.MaNguoiDung).Take(3).ToListAsync();
if (reviewers.Count != 3) throw new InvalidOperationException("Testing DB needs three eligible reviewers in category 1.");

var runId = $"QA-REVIEW-{Guid.NewGuid():N}";
var title = $"{runId} - thử bất biến quy trình";
var qaParent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "qa-content"));
var qaRoot = Path.GetFullPath(Path.Combine(qaParent, runId));
if (!qaRoot.StartsWith(qaParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Invalid QA content directory.");
var article = new BaiBao
{
    TieuDe = title,
    MaNguoiDung = authorId,
    MaChuyenNganh = categoryId,
    TrangThai = "Chờ sơ duyệt",
    NgayGui = DateTime.Now,
    NgayCapNhat = DateTime.Now
};
int? articleId = null;
int? coauthorUserId = null;
int? fixtureIssueId = null;
var registrationId = Guid.NewGuid();
var coauthorEmail = $"coauthor-{Guid.NewGuid():N}@example.test";
var passed = 0;
var extraArticles = new List<int>();
void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAIL {name}; run={runId}, article={articleId}");
    passed++;
    Console.WriteLine($"PASS {name}");
}

try
{
    db.BaiBaos.Add(article);
    await db.SaveChangesAsync();
    articleId = article.MaBaiBao;
    db.ThuMucBaiBaos.Add(new ThuMucBaiBao
    {
        MaBaiBao = articleId.Value,
        TenThuMuc = $"{runId}.pdf",
        DuongDan = $"Uploads/QA/{runId}.pdf",
        LoaiThuMuc = "File ẩn danh",
        KichThuoc = 1,
        SoVong = 1,
        NgayTaiLen = DateTime.Now
    });
    await db.SaveChangesAsync();

    var service = new PhanBienService(db, new TestHostEnvironment());
    async Task<string> Status() => (await db.BaiBaos.AsNoTracking()
        .Where(b => b.MaBaiBao == articleId.Value)
        .Select(b => b.TrangThai).SingleAsync());
    async Task<int> Invite(int reviewerId)
    {
        var result = await service.AssignReviewerAsync(editorId, new PhanCongRequestDto
        {
            MaBaiBao = articleId.Value, MaNguoiDungReviewer = reviewerId, SoVong = 1
        });
        Check(result.Success && result.MaPhanCong.HasValue, $"Invite reviewer {reviewerId}");
        return result.MaPhanCong!.Value;
    }

    var first = await Invite(reviewers[0]);
    var second = await Invite(reviewers[1]);
    Check(await Status() == "Chờ sơ duyệt", "Two pending invitations do not start review");
    var premature = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    { MaBaiBao = articleId.Value, TrangThaiMoi = "Đang phản biện" });
    Check(!premature.Success && await Status() == "Chờ sơ duyệt", "Editor cannot bypass two acceptances");

    var otherUser = await service.RespondToAssignmentAsync(first, reviewers[1], true);
    Check(!otherUser.Success, "Another reviewer cannot accept this invitation");
    var acceptedFirst = await service.RespondToAssignmentAsync(first, reviewers[0], true);
    Check(acceptedFirst.Success && await Status() == "Chờ sơ duyệt", "One acceptance keeps article waiting");
    var declinedSecond = await service.RespondToAssignmentAsync(second, reviewers[1], false);
    Check(declinedSecond.Success && await Status() == "Chờ sơ duyệt", "Decline does not start review");
    var duplicateResponse = await service.RespondToAssignmentAsync(second, reviewers[1], true);
    Check(!duplicateResponse.Success, "Declined invitation cannot be accepted later");

    var replacement = await Invite(reviewers[2]);
    var acceptedReplacement = await service.RespondToAssignmentAsync(replacement, reviewers[2], true);
    Check(acceptedReplacement.Success && await Status() == "Đang phản biện", "Two acceptances start review");
    var transitions = await db.LichSuTrangThais.AsNoTracking()
        .CountAsync(h => h.MaBaiBao == articleId.Value && h.TrangThaiMoi == "Đang phản biện");
    Check(transitions == 1, "Review start has exactly one audit record");

    db.ChangeTracker.Clear();
    await db.PhanCongPhanBiens.Where(p => p.MaPhanCong == first).ExecuteUpdateAsync(u => u.SetProperty(p => p.NgayPhanCong, WorkflowTools.VietnamNow.Date.AddDays(-3)).SetProperty(p => p.HanPhanHoi, WorkflowTools.VietnamNow.Date.AddDays(-1)).SetProperty(p => p.HanHoanThanh, WorkflowTools.VietnamNow.Date.AddDays(1)));
    var maintenance = new WorkflowMaintenanceService(db, new ConfigurationBuilder().Build());
    await maintenance.RunAsync(articleId.Value);
    await maintenance.RunAsync(articleId.Value);
    Check(await db.WorkflowRecords.CountAsync(r => r.ArticleId == articleId.Value && r.Kind == "ReviewReminder") == 1, "Reminder is queued once per assignment per day");
    await db.PhanCongPhanBiens.Where(p => p.MaPhanCong == second).ExecuteUpdateAsync(u => u.SetProperty(p => p.TrangThai, "Chờ phản hồi").SetProperty(p => p.NgayPhanCong, WorkflowTools.VietnamNow.Date.AddDays(-3)).SetProperty(p => p.HanPhanHoi, WorkflowTools.VietnamNow.Date.AddDays(-1)));
    db.ChangeTracker.Clear(); await maintenance.RunAsync(articleId.Value);
    Check(await db.PhanCongPhanBiens.Where(p => p.MaPhanCong == second).Select(p => p.TrangThai).SingleAsync() == "Từ chối phản biện", "Expired invitation closes automatically without deleting assignment");

    var noReviewsDecision = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    { MaBaiBao = articleId.Value, TrangThaiMoi = "Chờ chỉnh sửa" });
    Check(!noReviewsDecision.Success && await Status() == "Đang phản biện", "Decision requires BM-04 results");

    await db.PhanCongPhanBiens.Where(p => p.MaPhanCong == first || p.MaPhanCong == replacement).ExecuteUpdateAsync(u => u.SetProperty(p => p.HanPhanHoi, WorkflowTools.VietnamNow.Date).SetProperty(p => p.HanHoanThanh, WorkflowTools.VietnamNow.Date.AddDays(7)));

    // Reuse only this run's fixture to prove two separate API-style contexts
    // cannot race and leave the article waiting after both acceptances.
    db.ChangeTracker.Clear();
    await db.PhanCongPhanBiens.Where(p => p.MaPhanCong == first || p.MaPhanCong == replacement)
        .ExecuteUpdateAsync(update => update.SetProperty(p => p.TrangThai, "Chờ phản hồi"));
    await db.BaiBaos.Where(b => b.MaBaiBao == articleId.Value && b.TieuDe == title)
        .ExecuteUpdateAsync(update => update.SetProperty(b => b.TrangThai, "Chờ sơ duyệt"));
    await db.LichSuTrangThais.Where(h => h.MaBaiBao == articleId.Value).ExecuteDeleteAsync();

    async Task<(bool Success, string Message)> AcceptInSeparateContext(int assignmentId, int reviewerId)
    {
        await using var separateDb = new QLTapChiKhoaHocContext(options);
        var separateService = new PhanBienService(separateDb, new TestHostEnvironment());
        return await separateService.RespondToAssignmentAsync(assignmentId, reviewerId, true);
    }
    var concurrent = await Task.WhenAll(
        AcceptInSeparateContext(first, reviewers[0]),
        AcceptInSeparateContext(replacement, reviewers[2]));
    Check(concurrent.All(result => result.Success) && await Status() == "Đang phản biện",
        "Concurrent acceptances start review exactly once");
    var concurrentTransitions = await db.LichSuTrangThais.AsNoTracking()
        .CountAsync(h => h.MaBaiBao == articleId.Value && h.TrangThaiMoi == "Đang phản biện");
    Check(concurrentTransitions == 1, "Concurrent acceptances create one audit record");
    var authorDetail = await new BaiBaoService(db, new TestHostEnvironment())
        .GetSubmissionDetailAsync(articleId.Value, authorId, false);
    var authorHistory = authorDetail?.LichSuTrangThais.SingleOrDefault(h => h.TrangThaiMoi == "Đang phản biện");
    Check(authorHistory?.NguoiThucHien == "Hệ thống" &&
        authorHistory.GhiChu?.Contains("chuyên gia đã nhận lời", StringComparison.OrdinalIgnoreCase) != true,
        "Author sees a safe automated status update without reviewer identity");

    var firstReview = new PhieuDanhGiaDto
    {
        MaPhanCong = first,
        DiemTinhMoi = 8, DiemPhuongPhap = 8, DiemKetQua = 7, DiemTrinhBay = 9,
        DiemTongKet = 0, // A client-supplied total must never override the server calculation.
        NhanXetChoTacGia = "Cần bổ sung kết quả thực nghiệm.\n" + new string('A', 6000),
        NhanXetBaoMat = "Ghi chú nội bộ dành cho biên tập.",
        KienNghi = "Chỉnh sửa nhỏ"
    };
    var draftInput = new PhieuDanhGiaBanNhapDto
    {
        DiemTinhMoi = 8, NhanXetChoTacGia = "Bản nháp chưa hoàn tất.",
        NhanXetBaoMat = "Ghi chú riêng.", KienNghi = "Chỉnh sửa nhỏ"
    };
    var foreignDraft = await service.SaveMyEvaluationDraftAsync(first, reviewers[1], draftInput);
    Check(!foreignDraft.Success && await db.PhieuDanhGiaBanNhaps.CountAsync() == baselineDrafts,
        "Unassigned reviewer cannot create another reviewer's draft");
    var draftSaved = await service.SaveMyEvaluationDraftAsync(first, reviewers[0], draftInput);
    var draftRow = await db.PhieuDanhGiaBanNhaps.AsNoTracking().SingleOrDefaultAsync(d => d.MaPhanCong == first);
    Check(draftSaved.Success && draftRow?.NhanXetChoTacGia == "Bản nháp chưa hoàn tất." &&
        await db.PhanCongPhanBiens.AsNoTracking().AnyAsync(p => p.MaPhanCong == first && p.TrangThai == "Đồng ý phản biện"),
        "Accepted reviewer draft is stored without completing the assignment");
    var draftIndex = await service.GetMyEvaluationDraftsAsync(reviewers[0]);
    var draftRead = await service.GetMyEvaluationDraftAsync(first, reviewers[0]);
    Check(draftIndex.Any(d => d.MaPhanCong == first) && draftRead?.NhanXetBaoMat == "Ghi chú riêng." &&
        await service.GetMyEvaluationDraftAsync(first, reviewers[1]) == null,
        "Draft read and index stay private to the assigned reviewer");
    draftInput.NhanXetChoTacGia = "Bản nháp đã cập nhật.";
    var draftUpdated = await service.SaveMyEvaluationDraftAsync(first, reviewers[0], draftInput);
    Check(draftUpdated.Success && await db.PhieuDanhGiaBanNhaps.CountAsync(d => d.MaPhanCong == first) == 1 &&
        await db.PhieuDanhGiaBanNhaps.AsNoTracking().AnyAsync(d => d.MaPhanCong == first && d.NhanXetChoTacGia == "Bản nháp đã cập nhật."),
        "Saving again updates the same draft row");
    var wrongOwnerReview = await service.SubmitEvaluationAsync(reviewers[1], firstReview);
    Check(!wrongOwnerReview.Success, "Unassigned reviewer cannot submit BM-04");
    async Task<(bool Success, string Message)> ConcurrentEvaluation()
    {
        await using var reviewDb = new QLTapChiKhoaHocContext(options);
        return await new PhanBienService(reviewDb, new TestHostEnvironment(qaRoot)).SubmitEvaluationAsync(reviewers[0], firstReview);
    }
    var simultaneousReviews = await Task.WhenAll(ConcurrentEvaluation(), ConcurrentEvaluation());
    Check(simultaneousReviews.Count(r => r.Success) == 1 && await db.PhieuDanhGias.CountAsync(p => p.MaPhanCong == first) == 1,
        "Concurrent BM-04 submissions keep exactly one result and one completed assignment");
    db.ChangeTracker.Clear();
    var savedFirstReview = simultaneousReviews.Single(r => r.Success);
    Check(savedFirstReview.Success && await db.PhieuDanhGias.AsNoTracking()
        .AnyAsync(p => p.MaPhanCong == first && p.DiemTongKet == 8) &&
        !await db.PhieuDanhGiaBanNhaps.AsNoTracking().AnyAsync(p => p.MaPhanCong == first),
        "BM-04 is stored with server-calculated score and its draft is removed");
    var duplicateReview = await service.SubmitEvaluationAsync(reviewers[0], firstReview);
    Check(!duplicateReview.Success, "BM-04 cannot be submitted twice");
    var oneReviewDecision = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    { MaBaiBao = articleId.Value, TrangThaiMoi = "Chờ chỉnh sửa" });
    Check(!oneReviewDecision.Success && await Status() == "Đang phản biện",
        "Editorial decision waits for both BM-04 forms");

    var replacementReview = new PhieuDanhGiaDto
    {
        MaPhanCong = replacement,
        DiemTinhMoi = 7, DiemPhuongPhap = 7, DiemKetQua = 7, DiemTrinhBay = 7,
        NhanXetChoTacGia = "Đề nghị tác giả giải trình và nộp bản sửa.",
        KienNghi = "Chỉnh sửa nhỏ"
    };
    var savedReplacementReview = await service.SubmitEvaluationAsync(reviewers[2], replacementReview);
    Check(savedReplacementReview.Success, "Second accepted reviewer submits BM-04");
    var revisionDecision = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    {
        MaBaiBao = articleId.Value,
        TrangThaiMoi = "Chờ chỉnh sửa",
        GhiChu = "Nội bộ: không gửi tên chuyên gia phản biện cho tác giả.",
        ThongBaoChoTacGia = "Vui lòng nộp bản sửa và BM-03 theo góp ý phản biện."
    });
    Check(revisionDecision.Success && await Status() == "Chờ chỉnh sửa",
        "Two completed BM-04 forms allow revision decision");
    var authorRevisionDetail = await new BaiBaoService(db, new TestHostEnvironment())
        .GetSubmissionDetailAsync(articleId.Value, authorId, false);
    var revisionHistory = authorRevisionDetail?.LichSuTrangThais
        .LastOrDefault(h => h.TrangThaiMoi == "Chờ chỉnh sửa");
    Check(revisionHistory?.GhiChu?.StartsWith("Vui lòng nộp bản sửa và BM-03 theo góp ý phản biện.") == true &&
        revisionHistory.GhiChu.Contains("Cần bổ sung kết quả thực nghiệm.") &&
        !revisionHistory.GhiChu.Contains("Ghi chú nội bộ"),
        "Author sees only the approved revision notice");
    var filesBeforeInvalidRevision = await db.ThuMucBaiBaos.CountAsync(f => f.MaBaiBao == articleId.Value);
    var paperService = new BaiBaoService(db, new TestHostEnvironment());
    var missingRevision = await paperService.ResubmitPaperAsync(articleId.Value, authorId,
        new BaiBaoResubmitDto { GiaiTrinh = "Đã tiếp thu và chỉnh sửa." });
    Check(!missingRevision.Success && await Status() == "Chờ chỉnh sửa" &&
        await db.ThuMucBaiBaos.CountAsync(f => f.MaBaiBao == articleId.Value) == filesBeforeInvalidRevision,
        "Missing revised manuscript and BM-03 cannot advance article");
    var blankExplanation = await paperService.ResubmitPaperAsync(articleId.Value, authorId,
        new BaiBaoResubmitDto { GiaiTrinh = "   " });
    Check(!blankExplanation.Success && await Status() == "Chờ chỉnh sửa",
        "Blank BM-03 explanation cannot advance article");

    static IFormFile PdfFixture(string name)
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\n%%EOF\n");
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name)
        { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
    }
    var validRevision = await new BaiBaoService(db, new TestHostEnvironment(qaRoot))
        .ResubmitPaperAsync(articleId.Value, authorId, new BaiBaoResubmitDto
        {
            GiaiTrinh = "Đã chỉnh sửa phương pháp và giải trình từng góp ý.",
            FileClean = PdfFixture("Revised.pdf"),
            FileBm03 = PdfFixture("BM03.pdf")
        });
    var revisionFiles = await db.ThuMucBaiBaos.AsNoTracking()
        .Where(f => f.MaBaiBao == articleId.Value && f.SoVong == 2)
        .ToListAsync();
    Check(validRevision.Success && await Status() == "Chờ quyết định" &&
        revisionFiles.Count == 2 && revisionFiles.All(f =>
            File.Exists(Path.Combine(qaRoot, f.DuongDan.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar)))),
        "Valid revised manuscript and BM-03 advance to editorial decision");

    var authorList = await paperService.GetMySubmissionsAsync(authorId);
    var listItem = authorList.Single(s => s.MaBaiBao == articleId.Value);
    Check(listItem.NhanXetPhanBien?.Contains("Cần bổ sung kết quả thực nghiệm.") == true &&
        listItem.NhanXetPhanBien.Contains(new string('A', 6000)) &&
        listItem.SoVong == 2 && !listItem.CoTheNopLai,
        "Author dashboard receives released feedback and current-round action permissions");
    // A guest authorship exists before registration; linking must preserve its snapshot.
    var guest = new DongTacGia { MaBaiBao = articleId.Value, Email = " " + coauthorEmail.ToUpperInvariant() + " ",
        HoTen = "Tên tác giả lúc nộp", DonVi = "Đơn vị lúc nộp", MaORCID = "0000-0001-0002-0003", ThuTu = 2 };
    var alreadyLinked = new DongTacGia { MaBaiBao = articleId.Value, Email = coauthorEmail,
        HoTen = "Không thay liên kết cũ", MaNguoiDung = authorId, ThuTu = 3 };
    db.DongTacGias.AddRange(guest, alreadyLinked);
    var settings = Options.Create(new EmailVerificationSettings
    { HmacSecretKey = "coauthor-local-test-hmac-key-2026", SimulateDeliveryInDev = true });
    var verifier = new EmailVerificationService(db, settings, NullLogger<EmailVerificationService>.Instance);
    var otpId = Guid.NewGuid();
    var (otp, hash) = verifier.GenerateOtp(otpId);
    db.DangKyChoXacNhans.Add(new DangKyChoXacNhan
    {
        MaDangKy = registrationId, EmailGoc = coauthorEmail, EmailSoSanh = coauthorEmail,
        TenDangNhapSoSanh = "qa" + Guid.NewGuid().ToString("N"), MatKhauHash = BCrypt.Net.BCrypt.HashPassword("LocalFixture#2026"),
        Ten = "Mới", HoTen = "Tên tài khoản mới", DonVi = "Đơn vị tài khoản mới", HetHanHoSoUtc = DateTime.UtcNow.AddHours(1),
        MaXacNhanEmails = new List<MaXacNhanEmail> { new() { MaId = otpId, MaHash = hash, HetHanUtc = DateTime.UtcNow.AddMinutes(10) } }
    });
    await db.SaveChangesAsync();
    var auth = new AuthService(db, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        { ["Jwt:Key"] = "local-fixture-signing-key-2026-long-enough-for-hs256" }).Build(), verifier,
        new EmailSenderService(db, settings, NullLogger<EmailSenderService>.Instance), settings,
        NullLogger<AuthService>.Instance, new TestHostEnvironment(qaRoot));
    var invalidRegistration = await auth.VerifyEmailAsync(new VerifyEmailRequest
    { RegistrationId = registrationId, VerificationCode = otp == "000000" ? "000001" : "000000" });
    Check(!invalidRegistration.Success && await db.DongTacGias.AsNoTracking().AnyAsync(d => d.MaDongTacGia == guest.MaDongTacGia && d.MaNguoiDung == null),
        "Guest coauthor remains unlinked before successful email verification");
    var registration = await auth.VerifyEmailAsync(new VerifyEmailRequest { RegistrationId = registrationId, VerificationCode = otp });
    coauthorUserId = await db.NguoiDungs.Where(u => u.Email == coauthorEmail).Select(u => (int?)u.MaNguoiDung).SingleOrDefaultAsync();
    Check(registration.Success && coauthorUserId.HasValue && registration.SoBaiDongTacGiaLienKet == 1,
        "Verified registration atomically links historical guest authorship without stored procedure");
    Check(registration.User?.VaiTros.Contains("Tác giả") == true,
        "Registered coauthor retains author role for submitting their own new articles");
    db.ChangeTracker.Clear();
    var linkedGuest = await db.DongTacGias.AsNoTracking().SingleAsync(d => d.MaDongTacGia == guest.MaDongTacGia);
    Check(linkedGuest.MaNguoiDung == coauthorUserId && linkedGuest.HoTen == guest.HoTen && linkedGuest.DonVi == guest.DonVi &&
        linkedGuest.MaORCID == guest.MaORCID && linkedGuest.ThuTu == 2, "Linking preserves original authorship metadata");
    Check(await db.DongTacGias.AsNoTracking().AnyAsync(d => d.MaDongTacGia == alreadyLinked.MaDongTacGia && d.MaNguoiDung == authorId),
        "Email match cannot overwrite another account's existing authorship link");
    Check(await CoauthorAccountLinker.LinkAsync(db, coauthorUserId!.Value) == 0, "Account linking is idempotent");
    await using (var ownArticleTransaction = await db.Database.BeginTransactionAsync())
    {
        var ownPaper = await new BaiBaoService(db, new TestHostEnvironment(qaRoot)).SubmitPaperAsync(coauthorUserId.Value,
            new BaiBaoSubmitDto { SubmissionId = Guid.NewGuid(), Consent = true, TieuDe = "Bài mới của đồng tác giả " + runId, TomTat = "Tóm tắt thử nghiệm", TuKhoa = "thử nghiệm",
                MaChuyenNganh = categoryId, TapTinBanThao = PdfFixture("OwnPaper.pdf") });
        var ownDetail = ownPaper.MaBaiBao.HasValue ? await paperService.GetSubmissionDetailAsync(ownPaper.MaBaiBao.Value, coauthorUserId.Value, false) : null;
        Check(ownPaper.Success && ownDetail != null && !ownDetail.LaDongTacGia && ownDetail.MaNguoiDung == coauthorUserId,
            "Coauthor can submit a new article and becomes its submitting author");
        await ownArticleTransaction.RollbackAsync();
        db.ChangeTracker.Clear();
    }
    var coauthorList = await paperService.GetMySubmissionsAsync(coauthorUserId.Value);
    var coauthorDetail = await paperService.GetSubmissionDetailAsync(articleId.Value, coauthorUserId.Value, false);
    var workflows = new JournalWorkflowService(db, new TestHostEnvironment(qaRoot), settings, verifier,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Workflow:EditorialInbox"] = coauthorEmail }).Build());
    var draftId = Guid.NewGuid();
    var savedDraft = await workflows.SaveDraft(coauthorUserId.Value, new SubmissionDraftRequest { Id = draftId,
        Payload = System.Text.Json.JsonSerializer.Serialize(new { title = runId }), Manuscript = PdfFixture("Draft.pdf") });
    Check(await db.WorkflowFiles.AnyAsync(f => f.RecordId == draftId && f.Kind == "Manuscript"), "Server draft retains its private manuscript");
    var version = Convert.ToBase64String((await db.WorkflowRecords.AsNoTracking().SingleAsync(r => r.Id == draftId)).RowVersion);
    bool draftDenied = false;
    try { await workflows.SaveDraft(authorId, new SubmissionDraftRequest { Id = draftId, Payload = "{}", ExpectedVersion = version }); } catch (UnauthorizedAccessException) { draftDenied = true; }
    db.ChangeTracker.Clear();
    Check(draftDenied, "Another account cannot overwrite the submission draft");
    bool staleDenied = false;
    try { await workflows.SaveDraft(coauthorUserId.Value, new SubmissionDraftRequest { Id = draftId, Payload = "{}", ExpectedVersion = "stale" }); } catch (InvalidOperationException) { staleDenied = true; }
    db.ChangeTracker.Clear();
    Check(staleDenied, "Stale draft version cannot overwrite changes from another machine");
    await workflows.SaveDraft(coauthorUserId.Value, new SubmissionDraftRequest { Id = draftId, Payload = System.Text.Json.JsonSerializer.Serialize(new { title = runId }), ExpectedVersion = version, ReplaceManuscript = true });
    Check(!await db.WorkflowFiles.AnyAsync(f => f.RecordId == draftId), "Clearing manuscript removes the file from the saved draft");
    await workflows.DeleteDraft(draftId, coauthorUserId.Value);
    Check((await db.WorkflowRecords.AsNoTracking().SingleAsync(r => r.Id == draftId)).State == "Deleted", "Deleting draft changes only the owned draft");
    await using (var submissionTx = await db.Database.BeginTransactionAsync())
    {
        var submission = new BaiBaoSubmitDto { SubmissionId = Guid.NewGuid(), Consent = true, TieuDe = runId + " initial submission",
            TomTat = "Tóm tắt hồ sơ kiểm thử", TuKhoa = "kiểm thử", MaChuyenNganh = categoryId };
        Check(!(await paperService.SubmitPaperAsync(coauthorUserId.Value, submission)).Success, "Missing initial manuscript is rejected");
        submission.TapTinBanThao = PdfFixture("Initial.pdf"); submission.DongTacGiaJson = "not json";
        Check(!(await paperService.SubmitPaperAsync(coauthorUserId.Value, submission)).Success, "Malformed coauthor JSON is rejected");
        submission.DongTacGiaJson = "[]"; submission.Consent = false;
        Check(!(await paperService.SubmitPaperAsync(coauthorUserId.Value, submission)).Success, "Initial submission requires server-validated consent");
        submission.Consent = true; submission.FileBm02 = PdfFixture("BM02.pdf"); submission.Supplements.Add(PdfFixture("Appendix.pdf"));
        var initial = await paperService.SubmitPaperAsync(coauthorUserId.Value, submission);
        var retry = await paperService.SubmitPaperAsync(coauthorUserId.Value, submission);
        Check(initial.Success && retry.Success && initial.MaBaiBao == retry.MaBaiBao, "Retry of initial submission returns exactly the same article");
        Check(await db.WorkflowFiles.CountAsync(f => f.ArticleId == initial.MaBaiBao) == 2, "BM02 and appendix are stored privately with the submitted article");
        submission.TieuDe += " changed";
        Check(!(await paperService.SubmitPaperAsync(coauthorUserId.Value, submission)).Success, "Reusing submission key with a different payload is rejected");
        await submissionTx.RollbackAsync(); db.ChangeTracker.Clear();
    }
    db.NguoiDungChuyenMons.Add(new NguoiDungChuyenMon { MaNguoiDung = coauthorUserId.Value, MaChuyenNganh = categoryId }); await db.SaveChangesAsync();
    var withdrawalArticle = new BaiBao { TieuDe = runId + " withdrawal", MaNguoiDung = coauthorUserId.Value, MaChuyenNganh = categoryId, TrangThai = "Chờ sơ duyệt" };
    db.BaiBaos.Add(withdrawalArticle); await db.SaveChangesAsync(); extraArticles.Add(withdrawalArticle.MaBaiBao);
    bool withdrawalDenied = false;
    try { await workflows.RequestWithdrawal(withdrawalArticle.MaBaiBao, authorId, new ReasonRequest { Reason = "Xin rút hồ sơ kiểm thử." }); } catch (UnauthorizedAccessException) { withdrawalDenied = true; }
    db.ChangeTracker.Clear();
    Check(withdrawalDenied, "Unrelated account cannot request withdrawal");
    var withdrawal = await workflows.RequestWithdrawal(withdrawalArticle.MaBaiBao, coauthorUserId.Value, new ReasonRequest { Reason = "Xin rút hồ sơ kiểm thử." });
    Check(await workflows.RequestWithdrawal(withdrawalArticle.MaBaiBao, coauthorUserId.Value, new ReasonRequest { Reason = "Xin rút hồ sơ kiểm thử." }) == withdrawal, "Duplicate withdrawal request preserves one pending record");
    Check(await db.BaiBaos.AsNoTracking().AnyAsync(b => b.MaBaiBao == withdrawalArticle.MaBaiBao && b.TrangThai == "Chờ sơ duyệt"), "Withdrawal request does not immediately withdraw the article");
    await workflows.ReviewWithdrawal(withdrawal, editorId, new ActionReviewRequest { Approve = true, Note = "Duyệt yêu cầu của tác giả." });
    Check(await db.BaiBaos.AsNoTracking().AnyAsync(b => b.MaBaiBao == withdrawalArticle.MaBaiBao && b.TrangThai == "Đã rút"), "Editorial approval completes author withdrawal");
    bool withdrawalReplay = false;
    try { await workflows.ReviewWithdrawal(withdrawal, editorId, new ActionReviewRequest { Approve = true }); } catch (ArgumentException) { withdrawalReplay = true; }
    db.ChangeTracker.Clear();
    Check(withdrawalReplay, "Withdrawal approval cannot be processed twice");
    var resetId = await workflows.RequestReset(new ResetRequest { Email = coauthorEmail });
    Check(await workflows.RequestReset(new ResetRequest { Email = coauthorEmail }) == resetId, "Reset cooldown preserves the pending request id");
    var resetMail = await db.EmailOutboxes.AsNoTracking().Where(o => o.NguoiNhan == coauthorEmail && o.LoaiThu == "KhoiPhucMatKhau").OrderByDescending(o => o.TaoLucUtc).FirstAsync();
    var resetCode = System.Text.RegularExpressions.Regex.Match(resetMail.NoiDungText!, @"\b\d{6}\b").Value;
    Check(!await workflows.ConfirmReset(new ResetConfirm { Id = resetId, Code = resetCode == "000000" ? "000001" : "000000", Password = "NewFixture#2026" }), "Incorrect reset OTP cannot change password");
    Check(await workflows.ConfirmReset(new ResetConfirm { Id = resetId, Code = resetCode, Password = "NewFixture#2026" }), "Valid reset OTP changes password");
    Check(!await workflows.ConfirmReset(new ResetConfirm { Id = resetId, Code = resetCode, Password = "OtherFixture#2026" }), "Consumed reset OTP cannot be replayed");
    Check(await db.WorkflowRecords.AnyAsync(r => r.UserId == coauthorUserId && r.Kind == "AuthVersion"), "Password reset creates session revocation version");
    var resetUser = await db.NguoiDungs.AsNoTracking().SingleAsync(u => u.MaNguoiDung == coauthorUserId);
    Check(BCrypt.Net.BCrypt.Verify("NewFixture#2026", resetUser.MatKhau), "Reset stores a verified password hash");
    db.ChangeTracker.Clear();
    Check(coauthorList.Single(s => s.MaBaiBao == articleId.Value).LaDongTacGia && coauthorDetail?.LaDongTacGia == true &&
        coauthorDetail.LichSuTrangThais.Any(h => h.GhiChu?.Contains("Cần bổ sung kết quả thực nghiệm.") == true) &&
        !coauthorDetail.LichSuTrangThais.Any(h => h.GhiChu?.Contains("Ghi chú nội bộ") == true),
        "Linked coauthor sees previous article and released feedback without internal notes");
    await db.BaiBaos.Where(b => b.MaBaiBao == articleId.Value).ExecuteUpdateAsync(u => u.SetProperty(b => b.TrangThai, "Chờ chỉnh sửa"));
    var deniedCoauthorRevision = await paperService.ResubmitPaperAsync(articleId.Value, coauthorUserId.Value,
        new BaiBaoResubmitDto { GiaiTrinh = "Không được nộp", FileClean = PdfFixture("denied.pdf"), FileBm03 = PdfFixture("denied-bm03.pdf") });
    Check(!deniedCoauthorRevision.Success && !(await paperService.GetMySubmissionsAsync(coauthorUserId.Value))
        .Single(s => s.MaBaiBao == articleId.Value).CoTheNopLai, "Coauthor cannot resubmit even by calling the API directly");
    await db.BaiBaos.Where(b => b.MaBaiBao == articleId.Value).ExecuteUpdateAsync(u => u.SetProperty(b => b.TrangThai, "Chờ quyết định"));
    var notification = await db.EmailOutboxes.AsNoTracking().SingleAsync(o =>
        o.LoaiThu == "QuyetDinhBienTap" && o.NoiDungText != null && o.NoiDungText.Contains(title));
    Check(notification.TrangThai == "Pending" && notification.NoiDungText!.Contains("Cần bổ sung kết quả thực nghiệm.") &&
        !notification.NoiDungText.Contains("Ghi chú nội bộ"), "Decision queues author email with public feedback only");
    // SMTP is simulated and all changes made by this dispatcher check are rolled back.
    await using (var mailTransaction = await db.Database.BeginTransactionAsync())
    {
        await db.EmailOutboxes.Where(o => o.MaOutbox == notification.MaOutbox)
            .ExecuteUpdateAsync(u => u.SetProperty(o => o.TaoLucUtc, DateTime.UtcNow.AddDays(-1)));
        var mailer = new EmailSenderService(db, Options.Create(new EmailVerificationSettings
        { SimulateDeliveryInDev = true, OtpExpiryMinutes = 10 }), NullLogger<EmailSenderService>.Instance);
        await mailer.ProcessPendingOutboxAsync(1000);
        Check(await db.EmailOutboxes.AsNoTracking().AnyAsync(o => o.MaOutbox == notification.MaOutbox && o.TrangThai == "Sent"),
            "Decision email survives an IIS sleep longer than the OTP expiration window");
        await mailTransaction.RollbackAsync();
        db.ChangeTracker.Clear();
    }
    var anonymousFileId = await db.ThuMucBaiBaos.Where(f => f.MaBaiBao == articleId.Value && f.LoaiThuMuc == "File ẩn danh")
        .Select(f => f.MaThuMuc).FirstAsync();
    var deniedFile = await paperService.GetManuscriptForAuthorAsync(articleId.Value, authorId, false, anonymousFileId);
    Check(!deniedFile.Success, "Author cannot request internal anonymous manuscript by file id");
    var latestDownload = await new BaiBaoService(db, new TestHostEnvironment(qaRoot))
        .GetManuscriptForAuthorAsync(articleId.Value, authorId, false);
    Check(latestDownload.Success && latestDownload.FileName?.StartsWith("BanChinhSua") == true,
        "Default author download returns clean manuscript, never BM-03 or anonymous file");
    var restartOld = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    { MaBaiBao = articleId.Value, TrangThaiMoi = "Đang phản biện" });
    Check(!restartOld.Success && await Status() == "Chờ quyết định", "Re-review cannot reuse anonymous manuscript from old round");
    var assignOld = await service.AssignReviewerAsync(editorId, new PhanCongRequestDto
    { MaBaiBao = articleId.Value, MaNguoiDungReviewer = reviewers[1], SoVong = 1 });
    Check(!assignOld.Success, "Explicit round cannot bypass current revised manuscript");

    // Complete a second peer-review round against the new anonymized manuscript.
    db.ChangeTracker.Clear();
    db.ThuMucBaiBaos.Add(new ThuMucBaiBao
    { MaBaiBao = articleId.Value, TenThuMuc = "Anonymous2.pdf", DuongDan = revisionFiles.First(f => f.LoaiThuMuc == "Bản chỉnh sửa").DuongDan,
        LoaiThuMuc = "File ẩn danh", SoVong = 2, KichThuoc = 1, NgayTaiLen = DateTime.Now });
    await db.SaveChangesAsync();
    var secondRound1 = await service.AssignReviewerAsync(editorId, new PhanCongRequestDto
    { MaBaiBao = articleId.Value, MaNguoiDungReviewer = reviewers[0], SoVong = 2 });
    var secondRound2 = await service.AssignReviewerAsync(editorId, new PhanCongRequestDto
    { MaBaiBao = articleId.Value, MaNguoiDungReviewer = reviewers[2], SoVong = 2 });
    Check(secondRound1.Success && secondRound2.Success, "Same reviewers can be invited for revised manuscript in new round");
    await service.RespondToAssignmentAsync(secondRound1.MaPhanCong!.Value, reviewers[0], true);
    await service.RespondToAssignmentAsync(secondRound2.MaPhanCong!.Value, reviewers[2], true);
    Check(await Status() == "Đang phản biện", "Two acceptances automatically start second review round");
    firstReview.MaPhanCong = secondRound1.MaPhanCong.Value;
    replacementReview.MaPhanCong = secondRound2.MaPhanCong.Value;
    Check((await service.SubmitEvaluationAsync(reviewers[0], firstReview)).Success &&
        (await service.SubmitEvaluationAsync(reviewers[2], replacementReview)).Success,
        "Both reviewers can evaluate the revised manuscript with new BM-04 forms");
    var nextRevisionDecision = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    { MaBaiBao = articleId.Value, TrangThaiMoi = "Chờ chỉnh sửa", ThongBaoChoTacGia = "Góp ý mới cho vòng hai." });
    var nextAuthorList = await paperService.GetMySubmissionsAsync(authorId);
    Check(nextRevisionDecision.Success && nextAuthorList.Single(s => s.MaBaiBao == articleId.Value).NhanXetPhanBien?.Contains("Vòng 2") == true,
        "Author receives the second-round decision rather than stale first-round feedback");
    var repeatedRevision = await new BaiBaoService(db, new TestHostEnvironment(qaRoot))
        .ResubmitPaperAsync(articleId.Value, authorId, new BaiBaoResubmitDto
        { GiaiTrinh = "Bổ sung lần tiếp theo.", FileClean = PdfFixture("Clean3.pdf"), FileBm03 = PdfFixture("BM03-3.pdf") });
    Check(repeatedRevision.Success && await db.ThuMucBaiBaos.AnyAsync(f => f.MaBaiBao == articleId.Value && f.SoVong == 3),
        "Second-round author response advances file round without overwriting previous files");
    var moreChanges = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    { MaBaiBao = articleId.Value, TrangThaiMoi = "Chờ chỉnh sửa", ThongBaoChoTacGia = "Bổ sung giải trình trước quyết định cuối." });
    var fourthRevision = await new BaiBaoService(db, new TestHostEnvironment(qaRoot))
        .ResubmitPaperAsync(articleId.Value, authorId, new BaiBaoResubmitDto
        { GiaiTrinh = "Giải trình bổ sung.", FileClean = PdfFixture("Clean4.pdf"), FileBm03 = PdfFixture("BM03-4.pdf") });
    Check(moreChanges.Success && fourthRevision.Success && await db.ThuMucBaiBaos.AnyAsync(f => f.MaBaiBao == articleId.Value && f.SoVong == 4),
        "Additional editorial revision without new peer review still advances file round");

    db.ChangeTracker.Clear();
    await db.BaiBaos.Where(b => b.MaBaiBao == articleId.Value).ExecuteUpdateAsync(u => u.SetProperty(b => b.TrangThai, "Chờ chỉnh sửa"));
    async Task<(bool Success, string Message)> SubmitInSeparateContext()
    {
        await using var separateDb = new QLTapChiKhoaHocContext(options);
        return await new BaiBaoService(separateDb, new TestHostEnvironment(qaRoot)).ResubmitPaperAsync(articleId.Value, authorId,
            new BaiBaoResubmitDto { GiaiTrinh = "Nộp từ hai tab.", FileClean = PdfFixture("Concurrent.pdf"), FileBm03 = PdfFixture("ConcurrentBM03.pdf") });
    }
    var beforeConcurrentFiles = await db.ThuMucBaiBaos.CountAsync(f => f.MaBaiBao == articleId.Value);
    var concurrentRevisions = await Task.WhenAll(SubmitInSeparateContext(), SubmitInSeparateContext());
    Check(concurrentRevisions.Count(r => r.Success) == 1 && await Status() == "Chờ quyết định" &&
        await db.ThuMucBaiBaos.CountAsync(f => f.MaBaiBao == articleId.Value) == beforeConcurrentFiles + 2,
        "Concurrent author submissions create exactly one revision and one set of attachments");

    db.ChangeTracker.Clear();
    await db.BaiBaos.Where(b => b.MaBaiBao == articleId.Value).ExecuteUpdateAsync(u => u.SetProperty(b => b.TrangThai, "Chờ sửa hình thức"));
    var formatRevision = await new BaiBaoService(db, new TestHostEnvironment(qaRoot))
        .ResubmitPaperAsync(articleId.Value, authorId, new BaiBaoResubmitDto
        { GiaiTrinh = "Đã sửa thể thức.", FileClean = PdfFixture("Format.pdf") });
    Check(formatRevision.Success && await Status() == "Chờ sơ duyệt", "Format correction needs clean file and returns to screening without BM-03");

    await workflows.Screening(articleId.Value, editorId, new ScreeningRequest
    { Approve = false, SimilarityPercent = 35, Note = "Cần sửa các đoạn trùng lặp trước khi chuyển phản biện.", Report = PdfFixture("Similarity.pdf") });
    Check(await Status() == "Chờ sửa hình thức", "Negative screening returns manuscript to author correction");
    var authorEmail = await db.NguoiDungs.Where(u => u.MaNguoiDung == authorId).Select(u => u.Email).SingleAsync();
    Check(await db.EmailOutboxes.AnyAsync(o => o.NguoiNhan == authorEmail && o.NoiDungText != null &&
        o.NoiDungText.Contains("Cần sửa các đoạn trùng lặp")), "Negative screening queues the correction notice for author");
    db.ChangeTracker.Clear();

    var publishedId = await db.BaiBaos.AsNoTracking().Where(b => b.TrangThai == "Đã xuất bản")
        .Select(b => b.MaBaiBao).FirstAsync();
    var rejectPublished = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    { MaBaiBao = publishedId, TrangThaiMoi = "Từ chối" });
    Check(!rejectPublished.Success && await db.BaiBaos.AsNoTracking()
        .AnyAsync(b => b.MaBaiBao == publishedId && b.TrangThai == "Đã xuất bản"),
        "Published article cannot be silently rejected");

    var fixtureIssue = new SoTapChi { TenSo = runId, Tap = 1, So = 1, Nam = 2026, TrangThai = "Đang biên tập",
        NgayPhatHanh = new DateTime(2026, 10, 1) };
    db.SoTapChis.Add(fixtureIssue);
    await db.SaveChangesAsync();
    fixtureIssueId = fixtureIssue.MaSoTapChi;
    await db.BaiBaos.Where(b => b.MaBaiBao == articleId.Value).ExecuteUpdateAsync(u => u
        .SetProperty(b => b.MaSoTapChi, fixtureIssueId).SetProperty(b => b.TrangThai, "Sẵn sàng xuất bản")
        .SetProperty(b => b.TrangBatDau, 1).SetProperty(b => b.TrangKetThuc, 10));
    var finalPdf = (await db.ThuMucBaiBaos.AsNoTracking().Where(f => f.MaBaiBao == articleId.Value && f.LoaiThuMuc == "Bản thảo gốc")
        .OrderByDescending(f => f.NgayTaiLen).FirstAsync());
    db.ThuMucBaiBaos.Add(new ThuMucBaiBao { MaBaiBao = articleId.Value, TenThuMuc = "Published.pdf", DuongDan = finalPdf.DuongDan,
        LoaiThuMuc = "PDF thành phẩm", SoVong = 1, NgayTaiLen = DateTime.Now, KichThuoc = 1 });
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();
    var issueController = new SoTapChiController(new SoTapChiService(db), db, new TestHostEnvironment(qaRoot));
    issueController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, editorId.ToString()) }, "test")) } };
    Check(await issueController.PublishIssue(fixtureIssueId.Value) is ObjectResult { StatusCode: 400 }, "Publication is blocked before author proof approval");
    var proofId = await workflows.SendProof(articleId.Value, editorId);
    bool proofDenied = false;
    try { await workflows.ReviewProof(proofId, coauthorUserId.Value, new ActionReviewRequest { Approve = true }); } catch (UnauthorizedAccessException) { proofDenied = true; }
    db.ChangeTracker.Clear();
    Check(proofDenied, "Coauthor cannot approve the submitting author's proof");
    await workflows.ReviewProof(proofId, authorId, new ActionReviewRequest { Approve = false, Note = "Cần sửa lỗi trình bày bản bông." });
    Check((await Status()) == "Đang chế bản", "Author proof correction returns ready article to typesetting");
    await db.BaiBaos.Where(b => b.MaBaiBao == articleId.Value).ExecuteUpdateAsync(u => u.SetProperty(b => b.TrangThai, "Sẵn sàng xuất bản"));
    db.ChangeTracker.Clear();
    proofId = await workflows.SendProof(articleId.Value, editorId);
    await workflows.ReviewProof(proofId, authorId, new ActionReviewRequest { Approve = true });
    Check(await JournalWorkflowService.ProofApproved(db, articleId.Value), "Author approval is tied to the latest PDF");
    var currentPdf = await JournalWorkflowService.LatestPdf(db, articleId.Value);
    var changedPdf = new ThuMucBaiBao { MaBaiBao = articleId.Value, TenThuMuc = "NewVersion.pdf", DuongDan = currentPdf!.DuongDan, LoaiThuMuc = "PDF thành phẩm", SoVong = 1, NgayTaiLen = DateTime.Now.AddSeconds(1), KichThuoc = 1 };
    db.ThuMucBaiBaos.Add(changedPdf); await db.SaveChangesAsync();
    Check(!await JournalWorkflowService.ProofApproved(db, articleId.Value), "Uploading a new PDF invalidates approval of the old version");
    await db.ThuMucBaiBaos.Where(f => f.MaThuMuc == changedPdf.MaThuMuc).ExecuteDeleteAsync(); db.ChangeTracker.Clear();
    var overlapping = new BaiBao { TieuDe = runId + " overlap", MaNguoiDung = authorId, MaChuyenNganh = categoryId, MaSoTapChi = fixtureIssueId,
        TrangThai = "Sẵn sàng xuất bản", TrangBatDau = 10, TrangKetThuc = 20 };
    db.BaiBaos.Add(overlapping); await db.SaveChangesAsync(); extraArticles.Add(overlapping.MaBaiBao);
    var overlappingPdf = new ThuMucBaiBao { MaBaiBao = overlapping.MaBaiBao, TenThuMuc = "Overlap.pdf", DuongDan = currentPdf.DuongDan,
        LoaiThuMuc = "PDF thành phẩm", SoVong = 1, NgayTaiLen = DateTime.Now, KichThuoc = 1 };
    db.ThuMucBaiBaos.Add(overlappingPdf); await db.SaveChangesAsync();
    db.WorkflowRecords.Add(new WorkflowRecord { ArticleId = overlapping.MaBaiBao, UserId = authorId, Kind = "Proof", State = "Approved",
        Payload = System.Text.Json.JsonSerializer.Serialize(new { FileId = overlappingPdf.MaThuMuc }) }); await db.SaveChangesAsync();
    var overlapResult = await issueController.PublishIssue(fixtureIssueId.Value) as ObjectResult;
    Check(overlapResult?.StatusCode == 400 && System.Text.Json.JsonSerializer.Serialize(overlapResult.Value, new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }).Contains("trùng"), "Overlapping article page ranges block publication");
    await db.BaiBaos.Where(b => b.MaBaiBao == overlapping.MaBaiBao).ExecuteUpdateAsync(u => u.SetProperty(b => b.MaSoTapChi, (int?)null));
    db.ChangeTracker.Clear();

    await db.SoTapChis.Where(i => i.MaSoTapChi == fixtureIssueId).ExecuteUpdateAsync(u => u.SetProperty(i => i.NgayPhatHanh, WorkflowTools.VietnamNow.AddDays(1)));
    db.ChangeTracker.Clear();
    Check(await issueController.PublishIssue(fixtureIssueId.Value) is ObjectResult { StatusCode: 400 }, "Future issue cannot be published early");
    await db.SoTapChis.Where(i => i.MaSoTapChi == fixtureIssueId).ExecuteUpdateAsync(u => u.SetProperty(i => i.NgayPhatHanh, fixtureIssue.NgayPhatHanh));
    db.ChangeTracker.Clear();
    Check(await issueController.PublishIssue(fixtureIssueId.Value) is ObjectResult { StatusCode: 200 }, "Publication commits a ready issue and its article");
    var releaseMail = await db.EmailOutboxes.AsNoTracking().Where(o => o.LoaiThu == "ThongBaoPhatHanh" && o.NoiDungText!.Contains(title)).ToListAsync();
    Check(releaseMail.Count == 2 && releaseMail.Any(o => o.NguoiNhan == coauthorEmail && o.NoiDungText!.Contains("01/10/2026")),
        "Publication queues one dated notification per author or linked coauthor account");
    await issueController.PublishIssue(fixtureIssueId.Value);
    Check(await db.EmailOutboxes.CountAsync(o => o.LoaiThu == "ThongBaoPhatHanh" && o.NoiDungText!.Contains(title)) == 2,
        "Repeated publication does not duplicate notifications");
    var releasedDetail = await paperService.GetSubmissionDetailAsync(articleId.Value, coauthorUserId.Value, false);
    var releasedList = (await paperService.GetMySubmissionsAsync(coauthorUserId.Value)).Single(s => s.MaBaiBao == articleId.Value);
    Check(releasedList.NgayPhatHanh == fixtureIssue.NgayPhatHanh && releasedDetail?.NgayPhatHanh == fixtureIssue.NgayPhatHanh &&
        releasedDetail.LichSuTrangThais.Any(h => h.GhiChu?.Contains("01/10/2026") == true),
        "Coauthor dashboard and timeline show actual issue release date");
    var tiedPdf = new ThuMucBaiBao { MaBaiBao = articleId.Value, TenThuMuc = "LatestSameTimestamp.pdf", DuongDan = currentPdf.DuongDan,
        LoaiThuMuc = "PDF thành phẩm", SoVong = 1, NgayTaiLen = currentPdf.NgayTaiLen, KichThuoc = 1 };
    db.ThuMucBaiBaos.Add(tiedPdf); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    var publicPdf = await new BaiBaoService(db, new TestHostEnvironment(qaRoot)).GetPublicArticlePdfAsync(articleId.Value);
    Check(publicPdf.Success && publicPdf.FileName == tiedPdf.TenThuMuc,
        "Public PDF uses the newest file id when upload timestamps are equal");
}
finally
{
    db.ChangeTracker.Clear();
    foreach (var extra in extraArticles)
    {
        if (!await db.BaiBaos.AnyAsync(b => b.MaBaiBao == extra && b.TieuDe.StartsWith(runId))) throw new InvalidOperationException("Extra fixture identity mismatch.");
        await db.WorkflowRecords.Where(r => r.ArticleId == extra).ExecuteDeleteAsync();
        await db.LichSuTrangThais.Where(r => r.MaBaiBao == extra).ExecuteDeleteAsync();
        await db.ThuMucBaiBaos.Where(r => r.MaBaiBao == extra).ExecuteDeleteAsync();
        await db.BaiBaos.Where(b => b.MaBaiBao == extra).ExecuteDeleteAsync();
    }
    if (articleId.HasValue)
    {
        db.ChangeTracker.Clear();
        var owned = await db.BaiBaos.AsNoTracking()
            .AnyAsync(b => b.MaBaiBao == articleId.Value && b.TieuDe == title);
        if (!owned) throw new InvalidOperationException($"Cleanup refused: fixture identity mismatch, article={articleId}");
        var assignmentIds = await db.PhanCongPhanBiens.AsNoTracking()
            .Where(p => p.MaBaiBao == articleId.Value).Select(p => p.MaPhanCong).ToListAsync();
        await db.PhieuDanhGiaBanNhaps.Where(p => assignmentIds.Contains(p.MaPhanCong)).ExecuteDeleteAsync();
        await db.PhieuDanhGias.Where(p => assignmentIds.Contains(p.MaPhanCong)).ExecuteDeleteAsync();
        await db.PhanCongPhanBiens.Where(p => p.MaBaiBao == articleId.Value).ExecuteDeleteAsync();
        await db.LichSuTrangThais.Where(h => h.MaBaiBao == articleId.Value).ExecuteDeleteAsync();
        await db.ThuMucBaiBaos.Where(f => f.MaBaiBao == articleId.Value).ExecuteDeleteAsync();
        await db.DongTacGias.Where(d => d.MaBaiBao == articleId.Value).ExecuteDeleteAsync();
        await db.EmailOutboxes.Where(o => o.NoiDungText != null && o.NoiDungText.Contains(title)).ExecuteDeleteAsync();
        await db.WorkflowRecords.Where(r => r.ArticleId == articleId.Value).ExecuteDeleteAsync();
        await db.EmailOutboxes.Where(o => (o.LoaiThu == "DuyetBanBong" || o.LoaiThu == "NhacHanPhanBien") && o.TieuDe.EndsWith("#" + articleId.Value)).ExecuteDeleteAsync();
        await db.BaiBaos.Where(b => b.MaBaiBao == articleId.Value && b.TieuDe == title).ExecuteDeleteAsync();
    }
    await db.MaXacNhanEmails.Where(c => c.MaDangKy == registrationId).ExecuteDeleteAsync();
    await db.DangKyChoXacNhans.Where(p => p.MaDangKy == registrationId).ExecuteDeleteAsync();
    var fixtureUser = await db.NguoiDungs.Where(u => u.Email == coauthorEmail).Select(u => (int?)u.MaNguoiDung).SingleOrDefaultAsync();
    if (fixtureUser.HasValue)
    {
        await db.WorkflowRecords.Where(r => r.UserId == fixtureUser.Value).ExecuteDeleteAsync();
        await db.EmailOutboxes.Where(o => o.NguoiNhan == coauthorEmail).ExecuteDeleteAsync();
        await db.NguoiDungChuyenMons.Where(r => r.MaNguoiDung == fixtureUser.Value).ExecuteDeleteAsync();
        await db.NguoiDungVaiTros.Where(r => r.MaNguoiDung == fixtureUser.Value).ExecuteDeleteAsync();
        await db.NguoiDungs.Where(u => u.MaNguoiDung == fixtureUser.Value && u.Email == coauthorEmail).ExecuteDeleteAsync();
    }
    if (fixtureIssueId.HasValue) await db.SoTapChis.Where(s => s.MaSoTapChi == fixtureIssueId.Value && s.TenSo == runId).ExecuteDeleteAsync();
    Check(await db.BaiBaos.CountAsync() == baselineArticles &&
        await db.PhanCongPhanBiens.CountAsync() == baselineAssignments &&
        await db.PhieuDanhGiaBanNhaps.CountAsync() == baselineDrafts,
        "Scoped cleanup restores article/assignment/draft counts");
    if (Directory.Exists(qaRoot)) Directory.Delete(qaRoot, recursive: true);
}

Console.WriteLine($"RESULT {passed} checks passed; fixture {runId} removed");

sealed class TestHostEnvironment : IWebHostEnvironment
{
    public TestHostEnvironment(string? contentRoot = null) => ContentRootPath = contentRoot ?? AppContext.BaseDirectory;
    public string ApplicationName { get; set; } = "WorkflowInvariantTests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
