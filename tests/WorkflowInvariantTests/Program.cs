using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;
using HuitJournal.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text;

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
var passed = 0;
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

    var noReviewsDecision = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    { MaBaiBao = articleId.Value, TrangThaiMoi = "Chờ chỉnh sửa" });
    Check(!noReviewsDecision.Success && await Status() == "Đang phản biện", "Decision requires BM-04 results");

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
        NhanXetChoTacGia = "Cần bổ sung kết quả thực nghiệm.",
        NhanXetBaoMat = "Ghi chú nội bộ dành cho biên tập.",
        KienNghi = "Chỉnh sửa nhỏ"
    };
    var wrongOwnerReview = await service.SubmitEvaluationAsync(reviewers[1], firstReview);
    Check(!wrongOwnerReview.Success, "Unassigned reviewer cannot submit BM-04");
    var savedFirstReview = await service.SubmitEvaluationAsync(reviewers[0], firstReview);
    Check(savedFirstReview.Success && await db.PhieuDanhGias.AsNoTracking()
        .AnyAsync(p => p.MaPhanCong == first && p.DiemTongKet == 8),
        "First BM-04 is stored with server-calculated score");
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
    Check(revisionHistory?.GhiChu == "Vui lòng nộp bản sửa và BM-03 theo góp ý phản biện.",
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

    var publishedId = await db.BaiBaos.AsNoTracking().Where(b => b.TrangThai == "Đã xuất bản")
        .Select(b => b.MaBaiBao).FirstAsync();
    var rejectPublished = await service.MakeEditorialDecisionAsync(editorId, new QuyetDinhBienTapDto
    { MaBaiBao = publishedId, TrangThaiMoi = "Từ chối" });
    Check(!rejectPublished.Success && await db.BaiBaos.AsNoTracking()
        .AnyAsync(b => b.MaBaiBao == publishedId && b.TrangThai == "Đã xuất bản"),
        "Published article cannot be silently rejected");
}
finally
{
    if (articleId.HasValue)
    {
        db.ChangeTracker.Clear();
        var owned = await db.BaiBaos.AsNoTracking()
            .AnyAsync(b => b.MaBaiBao == articleId.Value && b.TieuDe == title);
        if (!owned) throw new InvalidOperationException($"Cleanup refused: fixture identity mismatch, article={articleId}");
        var assignmentIds = await db.PhanCongPhanBiens.AsNoTracking()
            .Where(p => p.MaBaiBao == articleId.Value).Select(p => p.MaPhanCong).ToListAsync();
        await db.PhieuDanhGias.Where(p => assignmentIds.Contains(p.MaPhanCong)).ExecuteDeleteAsync();
        await db.PhanCongPhanBiens.Where(p => p.MaBaiBao == articleId.Value).ExecuteDeleteAsync();
        await db.LichSuTrangThais.Where(h => h.MaBaiBao == articleId.Value).ExecuteDeleteAsync();
        await db.ThuMucBaiBaos.Where(f => f.MaBaiBao == articleId.Value).ExecuteDeleteAsync();
        await db.BaiBaos.Where(b => b.MaBaiBao == articleId.Value && b.TieuDe == title).ExecuteDeleteAsync();
    }
    Check(await db.BaiBaos.CountAsync() == baselineArticles &&
        await db.PhanCongPhanBiens.CountAsync() == baselineAssignments,
        "Scoped cleanup restores article/assignment counts");
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
