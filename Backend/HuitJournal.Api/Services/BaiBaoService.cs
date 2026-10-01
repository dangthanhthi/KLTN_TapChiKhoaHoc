using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;
using HuitJournal.Api.Infrastructure;

namespace HuitJournal.Api.Services;

public partial class BaiBaoService : IBaiBaoService
{
    private readonly QLTapChiKhoaHocContext _context;
    private readonly IWebHostEnvironment _env;

    public BaiBaoService(QLTapChiKhoaHocContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public Task<(bool Success, string Message, int? MaBaiBao, string? MaDinhDanh)> SubmitPaperAsync(int maNguoiDung, BaiBaoSubmitDto dto) =>
        SubmitValidatedAsync(maNguoiDung, dto);

    public async Task<List<BaiBaoListItemDto>> GetMySubmissionsAsync(int maNguoiDung)
    {
        var user = await _context.NguoiDungs.FindAsync(maNguoiDung);
        if (user == null) return new List<BaiBaoListItemDto>();

        await CoauthorAccountLinker.LinkAsync(_context, maNguoiDung);

        var articles = await _context.BaiBaos.AsNoTracking()
            .Where(b => b.MaNguoiDung == maNguoiDung || b.DongTacGias.Any(d => d.MaNguoiDung == maNguoiDung))
            .Include(b => b.ChuyenNganh).Include(b => b.SoTapChi).Include(b => b.DongTacGias)
            .Include(b => b.ThuMucBaiBaos).Include(b => b.LichSuTrangThais)
            .Include(b => b.PhanCongPhanBiens).ThenInclude(p => p.PhieuDanhGia)
            .OrderByDescending(b => b.NgayGui).AsSplitQuery().ToListAsync();
        var items = articles.Select(b => new BaiBaoListItemDto
        {
            MaBaiBao = b.MaBaiBao,
            MaDinhDanh = $"JST-{b.NgayGui.Year}-SUB{b.MaBaiBao:D4}",
            TieuDe = b.TieuDe, TieuDeTiengAnh = b.TieuDeTiengAnh,
            ChuyenNganh = b.ChuyenNganh.TenChuyenNganh, MaChuyenNganh = b.MaChuyenNganh,
            TrangThai = b.TrangThai, NgayGui = b.NgayGui, NgayCapNhat = b.NgayCapNhat,
            SoDongTacGia = b.DongTacGias.Count,
            TapTinGoc = b.ThuMucBaiBaos.Where(f => f.LoaiThuMuc == "Bản thảo gốc").OrderByDescending(f => f.NgayTaiLen).Select(f => f.TenThuMuc).FirstOrDefault(),
            NhanXetPhanBien = b.LichSuTrangThais.Where(h => !string.IsNullOrWhiteSpace(h.ThongBaoChoTacGia)).OrderByDescending(h => h.NgayChuyen).ThenByDescending(h => h.MaLichSu).Select(h => h.ThongBaoChoTacGia).FirstOrDefault(),
            LaDongTacGia = b.MaNguoiDung != maNguoiDung,
            NgayPhatHanh = b.TrangThai == "Đã xuất bản" ? b.SoTapChi?.NgayPhatHanh : null,
            CoTheNopLai = b.MaNguoiDung == maNguoiDung && (b.TrangThai == "Chờ sửa hình thức" || b.TrangThai == "Chờ chỉnh sửa"),
            SoVong = b.ThuMucBaiBaos.Select(f => f.SoVong).DefaultIfEmpty(1).Max()
        }).ToList();
        foreach (var item in items)
        {
            var article = articles.Single(b => b.MaBaiBao == item.MaBaiBao);
            var releasedDecision = article.LichSuTrangThais.Where(h => h.TrangThaiCu == "Đang phản biện" &&
                (h.TrangThaiMoi == "Chờ chỉnh sửa" || h.TrangThaiMoi == "Đã chấp nhận" || h.TrangThaiMoi == "Từ chối"))
                .OrderByDescending(h => h.NgayChuyen).ThenByDescending(h => h.MaLichSu).FirstOrDefault();
            if (releasedDecision != null)
                item.NhanXetPhanBien = string.Join("\n\n", new[] { item.NhanXetPhanBien, AuthorReviewFeedback.ForDecision(article, releasedDecision) }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
        }
        return items;
    }

    public async Task<BaiBaoDetailDto?> GetSubmissionDetailAsync(int maBaiBao, int maNguoiDung, bool isEditorOrAdmin)
    {
        var b = await _context.BaiBaos
            .AsNoTracking()
            .Include(x => x.TacGia)
            .Include(x => x.ChuyenNganh)
            .Include(x => x.SoTapChi)
            .Include(x => x.DongTacGias)
            .Include(x => x.PhanBienDeXuats)
            .Include(x => x.ThuMucBaiBaos)
            .Include(x => x.LichSuTrangThais).ThenInclude(ls => ls.NguoiThucHien)
            .Include(x => x.PhanCongPhanBiens).ThenInclude(p => p.PhieuDanhGia)
            .FirstOrDefaultAsync(x => x.MaBaiBao == maBaiBao);

        if (b == null) return null;

        // Phân quyền: Chỉ tác giả chính, đồng tác giả, hoặc Ban biên tập/Admin mới có quyền xem chi tiết bản thảo chưa công bố
        if (!isEditorOrAdmin)
        {
            var isMainAuthor = b.MaNguoiDung == maNguoiDung;
            var isCoAuthor = b.DongTacGias.Any(d => d.MaNguoiDung == maNguoiDung);
            if (!isMainAuthor && !isCoAuthor)
            {
                return null;
            }
        }

        return new BaiBaoDetailDto
        {
            MaBaiBao = b.MaBaiBao,
            MaDinhDanh = $"JST-{b.NgayGui.Year}-SUB{b.MaBaiBao:D4}",
            TieuDe = b.TieuDe,
            TieuDeTiengAnh = b.TieuDeTiengAnh,
            TomTat = b.TomTat,
            TomTatTiengAnh = b.TomTatTiengAnh,
            TuKhoa = b.TuKhoa,
            TrangThai = b.TrangThai,
            MaDOI = b.MaDOI,
            NgayGui = b.NgayGui,
            NgayCapNhat = b.NgayCapNhat,
            MaNguoiDung = b.MaNguoiDung,
            TacGiaChinh = b.TacGia.HoTen,
            EmailTacGiaChinh = b.TacGia.Email,
            MaChuyenNganh = b.MaChuyenNganh,
            TenChuyenNganh = b.ChuyenNganh.TenChuyenNganh,
            MaSoTapChi = b.MaSoTapChi,
            TenSoTapChi = b.SoTapChi?.TenSo,
            LaDongTacGia = b.MaNguoiDung != maNguoiDung && b.DongTacGias.Any(d => d.MaNguoiDung == maNguoiDung),
            NgayPhatHanh = b.TrangThai == "Đã xuất bản" ? b.SoTapChi?.NgayPhatHanh : null,
            TrangBatDau = b.TrangBatDau,
            TrangKetThuc = b.TrangKetThuc,
            DongTacGias = b.DongTacGias.OrderBy(d => d.ThuTu).Select(d => new DongTacGiaDetailDto
            {
                MaDongTacGia = d.MaDongTacGia,
                HoTen = d.HoTen,
                Email = d.Email,
                DonVi = d.DonVi,
                MaORCID = d.MaORCID,
                LaTacGiaLienHe = d.LaTacGiaLienHe,
                ThuTu = d.ThuTu,
                MaNguoiDung = d.MaNguoiDung
            }).ToList(),
            PhanBienDeXuats = b.PhanBienDeXuats.OrderBy(p => p.MaDeXuat).Select(p => new PhanBienDeXuatDto
            {
                MaDeXuat = p.MaDeXuat,
                HoTen = p.HoTen,
                Email = p.Email,
                DonVi = p.DonVi,
                LinhVuc = p.LinhVuc,
                LaChuyenGiaHeThong = p.LaChuyenGiaHeThong,
                MaNguoiDung = p.MaNguoiDung,
                NgayTao = p.NgayTao
            }).ToList(),
            TapTins = b.ThuMucBaiBaos
                .Where(f => isEditorOrAdmin || (f.LoaiThuMuc != "File ẩn danh" && f.LoaiThuMuc != "Bản thảo ẩn danh"))
                .OrderByDescending(f => f.NgayTaiLen)
                .Select(f => new ThuMucBaiBaoDto
            {
                MaThuMuc = f.MaThuMuc,
                TenThuMuc = f.TenThuMuc,
                DuongDan = f.DuongDan,
                LoaiThuMuc = f.LoaiThuMuc,
                KichThuoc = f.KichThuoc,
                SoVong = f.SoVong,
                NgayTaiLen = f.NgayTaiLen
            }).ToList(),
            LichSuTrangThais = b.LichSuTrangThais.OrderBy(ls => ls.NgayChuyen).Select(ls => new LichSuTrangThaiDto
            {
                MaLichSu = ls.MaLichSu,
                TrangThaiCu = ls.TrangThaiCu,
                TrangThaiMoi = ls.TrangThaiMoi,
                NgayChuyen = ls.NgayChuyen,
                NguoiThucHien = ls.MaNguoiThucHien == null
                    ? "Hệ thống"
                    : isEditorOrAdmin
                        ? ls.NguoiThucHien?.HoTen
                        : (ls.MaNguoiThucHien == b.MaNguoiDung ? b.TacGia.HoTen : "Ban biên tập"),
                GhiChu = isEditorOrAdmin 
                    ? ls.GhiChu 
                    : string.Join("\n\n", new[] { GetAuthorSafeHistoryNote(ls), AuthorReviewFeedback.ForDecision(b, ls) }
                        .Where(s => !string.IsNullOrWhiteSpace(s)))
            }).ToList()
        };
    }

    /// <summary>
    /// Bảo vệ phản biện kín hai chiều (Double-Blind) & Ngăn ngừa rò rỉ dữ liệu nội bộ:
    /// Tạo thông điệp trạng thái chuẩn tắc dành riêng cho tác giả, không để lộ ghi chú nội bộ,
    /// tên chuyên gia, tài khoản hay đường dẫn tệp riêng tư.
    /// </summary>
    private static string? GetAuthorSafeHistoryNote(LichSuTrangThaiBaiBao ls)
    {
        // Chỉ trả thông điệp chuẩn hoặc trường được biên tập viên chọn để gửi tác giả.
        // GhiChu luôn là nội dung nội bộ, kể cả khi tài khoản tác giả có thêm vai trò biên tập.
        return ls.TrangThaiMoi switch
        {
            "Chờ sơ duyệt" => "Hồ sơ bài báo đã được tiếp nhận và chuyển đến Ban biên tập để sơ duyệt thể thức.",
            "Chờ sửa hình thức" => !string.IsNullOrWhiteSpace(ls.ThongBaoChoTacGia) ? ls.ThongBaoChoTacGia : "Ban biên tập yêu cầu tác giả điều chỉnh thể thức bài viết.",
            "Đang phản biện" => "Ban biên tập đã gửi bản thảo ẩn danh tới Hội đồng chuyên gia phản biện độc lập.",
            "Chờ chỉnh sửa" => !string.IsNullOrWhiteSpace(ls.ThongBaoChoTacGia) ? ls.ThongBaoChoTacGia : "Ban biên tập yêu cầu tác giả tiếp thu ý kiến phản biện và nộp lại bản thảo hoàn thiện.",
            "Chờ quyết định" => "Tác giả đã nộp bản thảo hoàn thiện. Hồ sơ đang chờ Ban biên tập xem xét quyết định.",
            "Đã chấp nhận" => "Bài báo đã được Ban biên tập chính thức chấp nhận đăng.",
            "Đang chế bản" => "Bài báo đang trong quá trình biên tập kỹ thuật, định dạng và chế bản xuất bản.",
            "Sẵn sàng xuất bản" => "Bài báo đã hoàn tất duyệt bản bông và sẵn sàng đưa vào số phát hành.",
            "Đã xuất bản" => !string.IsNullOrWhiteSpace(ls.ThongBaoChoTacGia) ? ls.ThongBaoChoTacGia : "Bài báo đã được xuất bản chính thức trên Cổng thông tin Tạp chí.",
            "Từ chối" => !string.IsNullOrWhiteSpace(ls.ThongBaoChoTacGia) ? ls.ThongBaoChoTacGia : "Bản thảo chưa đáp ứng tiêu chí xuất bản của Tòa soạn.",
            _ => $"Trạng thái bài báo: {ls.TrangThaiMoi}."
        };
    }

    public async Task<BaiBaoPublicDto?> GetPublicArticleAsync(int maBaiBao)
    {
        var b = await _context.BaiBaos
            .AsNoTracking()
            .Include(x => x.TacGia)
            .Include(x => x.ChuyenNganh)
            .Include(x => x.SoTapChi)
            .Include(x => x.DongTacGias)
            .Include(x => x.ThuMucBaiBaos)
            .FirstOrDefaultAsync(x => x.MaBaiBao == maBaiBao && x.TrangThai == "Đã xuất bản" && x.SoTapChi != null && (x.SoTapChi.TrangThai == "Đã xuất bản" || x.SoTapChi.TrangThai == "Đã phát hành"));

        if (b == null) return null;

        // Chỉ công bố bài báo nếu Tòa soạn đã tải lên và phát hành "PDF thành phẩm"
        var hasPublishedPdf = b.ThuMucBaiBaos
            .Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                   && f.TenThuMuc.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

        if (!hasPublishedPdf) return null;

        var publicPdfUrl = $"/api/baibao/public/{b.MaBaiBao}/pdf";

        var authors = new List<DongTacGiaDetailDto>
        {
            new DongTacGiaDetailDto
            {
                HoTen = b.TacGia.HoTen,
                Email = b.TacGia.Email,
                DonVi = b.TacGia.DonVi,
                MaORCID = b.TacGia.MaORCID,
                LaTacGiaLienHe = true,
                ThuTu = 0,
                MaNguoiDung = b.TacGia.MaNguoiDung
            }
        };

        authors.AddRange(b.DongTacGias.OrderBy(d => d.ThuTu).Select(d => new DongTacGiaDetailDto
        {
            MaDongTacGia = d.MaDongTacGia,
            HoTen = d.HoTen,
            Email = d.Email,
            DonVi = d.DonVi,
            MaORCID = d.MaORCID,
            LaTacGiaLienHe = d.LaTacGiaLienHe,
            ThuTu = d.ThuTu,
            MaNguoiDung = d.MaNguoiDung
        }));

        return new BaiBaoPublicDto
        {
            MaBaiBao = b.MaBaiBao,
            MaSoTapChi = b.MaSoTapChi,
            TieuDe = b.TieuDe,
            TieuDeTiengAnh = b.TieuDeTiengAnh,
            TomTat = b.TomTat,
            TomTatTiengAnh = b.TomTatTiengAnh,
            TuKhoa = b.TuKhoa,
            MaDOI = b.MaDOI,
            NgayGui = b.NgayGui,
            NgayPhatHanh = b.SoTapChi?.NgayPhatHanh,
            ChuyenNganh = b.ChuyenNganh.TenChuyenNganh,
            TenSoTapChi = b.SoTapChi?.TenSo,
            Tap = b.SoTapChi?.Tap,
            So = b.SoTapChi?.So,
            Nam = b.SoTapChi?.Nam,
            TrangBatDau = b.TrangBatDau,
            TrangKetThuc = b.TrangKetThuc,
            FilePdfUrl = publicPdfUrl,
            AnhBiaUrl = b.SoTapChi != null
                ? (b.SoTapChi.TenSo.Contains("Yersin")
                    ? $"assets/images/cover_yersin_no{b.SoTapChi.So}.svg"
                    : $"assets/images/cover_huit_vol{b.SoTapChi.Tap}_no{b.SoTapChi.So}e.jpg")
                : null,
            TacGias = authors
        };
    }

    public async Task<(bool Success, string Message)> ResubmitPaperAsync(int maBaiBao, int maNguoiDung, BaiBaoResubmitDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var lockName = $"Journal:Article:{maBaiBao}";
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock @Resource = {lockName}, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @lockResult < 0 THROW 51000, 'Khong the khoa lan nop ban sua.', 1;
            """);
        var baiBao = await _context.BaiBaos
            .Include(b => b.ThuMucBaiBaos)
            .Include(b => b.LichSuTrangThais)
            .FirstOrDefaultAsync(b => b.MaBaiBao == maBaiBao);

        if (baiBao == null)
        {
            return (false, "Không tìm thấy bài báo.");
        }

        if (baiBao.MaNguoiDung != maNguoiDung)
        {
            return (false, "Bạn không có quyền chỉnh sửa bài báo này.");
        }

        // Ràng buộc máy trạng thái (State Machine): Thống nhất theo ràng buộc CHECK SQL (CHK_BaiBao_TrangThai)
        // Format corrections return to screening; peer-review revisions return to editorial decision.
        var isFormatRevision = baiBao.TrangThai == "Chờ sửa hình thức";
        if (!isFormatRevision && baiBao.TrangThai != "Chờ chỉnh sửa")
        {
            return (false, $"Bài báo đang ở trạng thái '{baiBao.TrangThai}', chỉ được phép nộp bản chỉnh sửa và giải trình BM-03 khi bài ở trạng thái 'Chờ chỉnh sửa'.");
        }

        if (string.IsNullOrWhiteSpace(dto.GiaiTrinh))
            return (false, "Vui lòng nhập nội dung giải trình tiếp thu ý kiến phản biện.");
        if (dto.GiaiTrinh.Trim().Length > 400)
            return (false, "Giải trình tóm tắt tối đa 400 ký tự. Nội dung chi tiết đặt trong BM-03.");
        if (!isFormatRevision && (dto.FileBm03 == null || dto.FileBm03.Length == 0))
            return (false, "Vui lòng tải lên bản giải trình BM-03.");
        if (dto.FileClean == null || dto.FileClean.Length == 0)
            return (false, "Vui lòng tải lên bản thảo đã chỉnh sửa.");

        // Tính số vòng chỉnh sửa dựa trên các vòng phân công trước đó
        var currentAssignmentRound = await _context.PhanCongPhanBiens
            .Where(p => p.MaBaiBao == maBaiBao)
            .Select(p => (int?)p.SoVong)
            .MaxAsync() ?? 1;
        var revisionRound = isFormatRevision ? 1 : Math.Max(currentAssignmentRound,
            baiBao.ThuMucBaiBaos.Select(f => f.SoVong).DefaultIfEmpty(1).Max()) + 1;

        if (dto.FileClean != null && dto.FileClean.Length > 0)
        {
            var (validClean, cleanErr) = await ValidateUploadedFileAsync(dto.FileClean, new[] { ".pdf", ".docx", ".doc" }, 30 * 1024 * 1024);
            if (!validClean) return (false, cleanErr);
        }
        if (dto.FileBm03 != null && dto.FileBm03.Length > 0)
        {
            var (validBm, bmErr) = await ValidateUploadedFileAsync(dto.FileBm03, new[] { ".pdf", ".docx", ".doc" }, 30 * 1024 * 1024);
            if (!validBm) return (false, bmErr);
        }
        if (dto.FileTracked != null && dto.FileTracked.Length > 0)
        {
            var (validTr, trErr) = await ValidateUploadedFileAsync(dto.FileTracked, new[] { ".pdf", ".docx", ".doc" }, 30 * 1024 * 1024);
            if (!validTr) return (false, trErr);
        }

        var trangThaiCu = baiBao.TrangThai;
        var nextStatus = isFormatRevision ? "Chờ sơ duyệt" : "Chờ quyết định";
        baiBao.TrangThai = nextStatus;
        baiBao.NgayCapNhat = WorkflowTools.VietnamNow;

        var uploadDir = Path.Combine(UploadStoragePaths.GetRoot(_env.ContentRootPath), "revisions", $"paper_{maBaiBao}");
        Directory.CreateDirectory(uploadDir);

        if (dto.FileClean != null && dto.FileClean.Length > 0)
        {
            var ext = Path.GetExtension(dto.FileClean.FileName).ToLowerInvariant();
            var fileName = $"Clean_R{revisionRound}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.FileClean.CopyToAsync(stream);
            }

            baiBao.ThuMucBaiBaos.Add(new ThuMucBaiBao
            {
                TenThuMuc = isFormatRevision ? $"BanSuaHinhThuc_{Guid.NewGuid():N}{ext}" : $"BanChinhSua_Vong{revisionRound}{ext}",
                DuongDan = $"/Uploads/revisions/paper_{maBaiBao}/{fileName}",
                LoaiThuMuc = isFormatRevision ? "Bản thảo gốc" : "Bản chỉnh sửa",
                KichThuoc = dto.FileClean.Length,
                SoVong = revisionRound,
                NgayTaiLen = WorkflowTools.VietnamNow
            });
        }

        if (dto.FileBm03 != null && dto.FileBm03.Length > 0)
        {
            var ext = Path.GetExtension(dto.FileBm03.FileName).ToLowerInvariant();
            var fileName = $"BM03_R{revisionRound}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.FileBm03.CopyToAsync(stream);
            }

            baiBao.ThuMucBaiBaos.Add(new ThuMucBaiBao
            {
                TenThuMuc = $"GiaiTrinh_BM03_Vong{revisionRound}{ext}",
                DuongDan = $"/Uploads/revisions/paper_{maBaiBao}/{fileName}",
                LoaiThuMuc = "Bản giải trình BM-03",
                KichThuoc = dto.FileBm03.Length,
                SoVong = revisionRound,
                NgayTaiLen = WorkflowTools.VietnamNow
            });
        }

        if (dto.FileTracked != null && dto.FileTracked.Length > 0)
        {
            var ext = Path.GetExtension(dto.FileTracked.FileName).ToLowerInvariant();
            var fileName = $"Tracked_R{revisionRound}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.FileTracked.CopyToAsync(stream);
            }

            baiBao.ThuMucBaiBaos.Add(new ThuMucBaiBao
            {
                TenThuMuc = $"DanhDauSuaDoi_Vong{revisionRound}{ext}",
                DuongDan = $"/Uploads/revisions/paper_{maBaiBao}/{fileName}",
                LoaiThuMuc = "Bản đánh dấu sửa đổi",
                KichThuoc = dto.FileTracked.Length,
                SoVong = revisionRound,
                NgayTaiLen = WorkflowTools.VietnamNow
            });
        }

        baiBao.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
        {
            TrangThaiCu = trangThaiCu,
            TrangThaiMoi = nextStatus,
            NgayChuyen = WorkflowTools.VietnamNow,
            MaNguoiThucHien = maNguoiDung,
            GhiChu = $"Tác giả nộp bản sửa ({nextStatus}): {dto.GiaiTrinh.Trim()}"
        });

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
        return (true, isFormatRevision
            ? "Đã nộp bản sửa hình thức. Hồ sơ được chuyển về sơ duyệt."
            : "Đã nộp bản sửa và BM-03. Ban biên tập sẽ xem xét hoặc tổ chức vòng phản biện tiếp theo.");
    }

    public async Task<(bool Success, string Message, string? PhysicalPath, string? FileName, string? ContentType)> GetManuscriptForAuthorAsync(int maBaiBao, int maNguoiDung, bool isEditorOrAdmin, int? fileId = null)
    {
        var baiBao = await _context.BaiBaos
            .Include(b => b.DongTacGias)
            .Include(b => b.ThuMucBaiBaos)
            .FirstOrDefaultAsync(b => b.MaBaiBao == maBaiBao);

        if (baiBao == null)
        {
            return (false, "Không tìm thấy bài báo.", null, null, null);
        }

        var isAuthor = baiBao.MaNguoiDung == maNguoiDung || baiBao.DongTacGias.Any(d => d.MaNguoiDung == maNguoiDung);
        if (!isEditorOrAdmin && !isAuthor)
        {
            return (false, "Bạn không có quyền tải tệp bản thảo của bài báo này.", null, null, null);
        }

        var fileRecord = baiBao.ThuMucBaiBaos
            .Where(f => isEditorOrAdmin || (f.LoaiThuMuc != "File ẩn danh" && f.LoaiThuMuc != "Bản thảo ẩn danh"))
            .Where(f => fileId.HasValue ? f.MaThuMuc == fileId.Value : f.LoaiThuMuc == "Bản thảo gốc" || f.LoaiThuMuc == "Bản chỉnh sửa")
            .OrderByDescending(f => f.SoVong)
            .ThenByDescending(f => f.NgayTaiLen)
            .ThenByDescending(f => f.MaThuMuc)
            .FirstOrDefault();

        if (fileRecord == null)
        {
            return (false, "Hồ sơ bài báo này chưa có tệp đính kèm.", null, null, null);
        }

        var physicalPath = UploadStoragePaths.ResolveExistingFile(_env.ContentRootPath, fileRecord.DuongDan);

        if (physicalPath == null)
        {
            return (false, "Tệp bản thảo không tồn tại trên hệ thống lưu trữ máy chủ.", null, null, null);
        }

        var ext = Path.GetExtension(fileRecord.TenThuMuc).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            _ => "application/octet-stream"
        };

        return (true, "Thành công", physicalPath, fileRecord.TenThuMuc, contentType);
    }

    public async Task<(bool Success, string Message, string? PhysicalPath, string? FileName, string? ContentType)> GetPublicArticlePdfAsync(int maBaiBao)
    {
        var baiBao = await _context.BaiBaos
            .Include(b => b.ThuMucBaiBaos)
            .Include(b => b.SoTapChi)
            .FirstOrDefaultAsync(b => b.MaBaiBao == maBaiBao);

        if (baiBao == null)
        {
            return (false, "Không tìm thấy bài báo.", null, null, null);
        }

        if (baiBao.TrangThai != "Đã xuất bản")
        {
            return (false, "Bài báo này chưa chính thức xuất bản công khai.", null, null, null);
        }

        if (baiBao.SoTapChi == null || (baiBao.SoTapChi.TrangThai != "Đã xuất bản" && baiBao.SoTapChi.TrangThai != "Đã phát hành"))
        {
            return (false, "Số tạp chí chứa bài báo này chưa được xuất bản công khai.", null, null, null);
        }

        // Tiêu chuẩn xuất bản học thuật chính thức:
        // CHỈ cho phép tải tệp loại "PDF thành phẩm" hoặc "PDF Xuất bản" đã qua khâu duyệt phát hành
        // Tuyệt đối không fallback sang bản thảo gốc, bản nháp docx hay bản chỉnh sửa nội bộ
        var fileRecord = baiBao.ThuMucBaiBaos
            .Where(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                     && f.TenThuMuc.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.NgayTaiLen)
            .ThenByDescending(f => f.MaThuMuc)
            .FirstOrDefault();

        if (fileRecord == null)
        {
            return (false, "Tệp PDF thành phẩm của bài báo chưa được phát hành trên hệ thống.", null, null, null);
        }

        var physicalPath = UploadStoragePaths.ResolveExistingFile(_env.ContentRootPath, fileRecord.DuongDan);

        if (physicalPath == null)
        {
            return (false, "Tệp PDF thành phẩm không tồn tại trên hệ thống lưu trữ máy chủ.", null, null, null);
        }

        return (true, "Thành công", physicalPath, fileRecord.TenThuMuc, "application/pdf");
    }

    /// <summary>
    /// Kiểm tra và xác thực an toàn tệp tải lên theo tiêu chuẩn OWASP File Upload Cheat Sheet:
    /// 1. Giới hạn dung lượng tệp (tối đa 30MB)
    /// 2. Danh sách trắng phần mở rộng (Extension Whitelist: .pdf, .docx, .doc)
    /// 3. Xác thực chữ ký nhị phân (Magic Bytes Header)
    /// 4. Xác minh cấu trúc hoàn chỉnh của tệp (PDF bắt buộc có %PDF- và %%EOF)
    /// 5. Chống Path Traversal và Null-byte Injection
    /// 6. Rà soát nội dung độc hại (chặn lệnh thực thi nhúng /Launch, script)
    /// </summary>
    private static async Task<(bool IsValid, string ErrorMessage)> ValidateUploadedFileAsync(
        IFormFile file, 
        string[] allowedExtensions, 
        long maxSizeBytes = 30 * 1024 * 1024)
    {
        if (file == null || file.Length == 0)
        {
            return (false, "Tệp tải lên không được để trống.");
        }

        if (file.Length > maxSizeBytes)
        {
            var maxMb = maxSizeBytes / (1024 * 1024);
            return (false, $"Dung lượng tệp ({file.Length / 1024 / 1024:N1} MB) vượt quá giới hạn tối đa cho phép ({maxMb} MB).");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            return (false, $"Định dạng tệp '{ext}' không được chấp nhận. Hệ thống chỉ cho phép: {string.Join(", ", allowedExtensions)}.");
        }

        // Chống Path Traversal và Null-byte
        var rawName = file.FileName;
        if (rawName.Contains('\0') || rawName.Contains(".."))
        {
            return (false, "Tên tệp không hợp lệ hoặc chứa ký tự bị cấm.");
        }

        // Đọc header để kiểm tra Magic Bytes
        byte[] headerBuffer = new byte[Math.Min(file.Length, 1024)];
        using (var stream = file.OpenReadStream())
        {
            await stream.ReadExactlyAsync(headerBuffer, 0, headerBuffer.Length);
        }

        if (ext == ".pdf")
        {
            // PDF Magic byte: %PDF- (0x25, 0x50, 0x44, 0x46, 0x2D)
            if (headerBuffer.Length < 5 ||
                headerBuffer[0] != 0x25 || headerBuffer[1] != 0x50 || headerBuffer[2] != 0x44 || headerBuffer[3] != 0x46 || headerBuffer[4] != 0x2D)
            {
                return (false, "Tệp không có chữ ký nhị phân hợp lệ của định dạng PDF chuẩn (thiếu header %PDF-).");
            }

            // Đọc phần cuối để kiểm tra dấu %%EOF (chuẩn ISO 32000-1)
            byte[] tailBuffer = new byte[Math.Min(file.Length, 1024)];
            using (var stream = file.OpenReadStream())
            {
                stream.Seek(-tailBuffer.Length, SeekOrigin.End);
                await stream.ReadExactlyAsync(tailBuffer, 0, tailBuffer.Length);
            }
            var tailText = System.Text.Encoding.ASCII.GetString(tailBuffer);
            if (!tailText.Contains("%%EOF"))
            {
                return (false, "Tệp PDF không hoàn chỉnh hoặc lỗi cấu trúc (thiếu dấu kết thúc %%EOF theo chuẩn ISO 32000-1).");
            }

            // Quét nội dung độc hại: Chặn lệnh thực thi nhúng /Launch hoặc script nguy hiểm
            var headerText = System.Text.Encoding.ASCII.GetString(headerBuffer);
            if (headerText.Contains("/Launch") || headerText.Contains("<script", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Tệp bị từ chối do chứa chỉ thị thực thi không an toàn theo tiêu chuẩn OWASP.");
            }
        }
        else if (ext == ".docx")
        {
            // DOCX là ZIP (PK\x03\x04: 0x50, 0x4B, 0x03, 0x04)
            if (headerBuffer.Length < 4 || headerBuffer[0] != 0x50 || headerBuffer[1] != 0x4B || headerBuffer[2] != 0x03 || headerBuffer[3] != 0x04)
            {
                return (false, "Tệp không có chữ ký nhị phân hợp lệ của định dạng Word (.docx).");
            }
        }
        else if (ext == ".doc")
        {
            // DOC là OLE Compound File (0xD0, 0xCF, 0x11, 0xE0)
            if (headerBuffer.Length < 4 || headerBuffer[0] != 0xD0 || headerBuffer[1] != 0xCF || headerBuffer[2] != 0x11 || headerBuffer[3] != 0xE0)
            {
                return (false, "Tệp không có chữ ký nhị phân hợp lệ của định dạng Word (.doc).");
            }
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Ban biên tập tải lên bản thảo ẩn danh (loại bỏ thông tin tác giả) theo từng vòng phản biện
    /// </summary>
    public async Task<(bool Success, string Message, string? DuongDan)> UploadAnonymousManuscriptAsync(int maBaiBao, IFormFile file, int? soVong, int maNguoiThucHien)
    {
        var baiBao = await _context.BaiBaos.FindAsync(maBaiBao);
        if (baiBao == null) return (false, "Không tìm thấy bài báo.", null);

        var (isValid, errorMsg) = await ValidateUploadedFileAsync(file, new[] { ".pdf", ".docx", ".doc" }, 30 * 1024 * 1024);
        if (!isValid)
        {
            return (false, errorMsg, null);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Xác định số vòng: ưu tiên soVong nếu truyền vào, hoặc vòng phân công lớn nhất
        var targetRound = soVong ?? (await _context.PhanCongPhanBiens
            .Where(p => p.MaBaiBao == maBaiBao)
            .Select(p => (int?)p.SoVong)
            .MaxAsync() ?? 1);
        if (targetRound < 1 || baiBao.TrangThai == "Đã xuất bản" || baiBao.TrangThai == "Đã rút")
            return (false, "Vòng phản biện hoặc trạng thái bài báo không hợp lệ.", null);

        var now = WorkflowTools.VietnamNow;
                var uploadSubFolder = Path.Combine("Submissions", now.Year.ToString(), now.Month.ToString("D2"));
                var uploadPhysicalPath = Path.Combine(UploadStoragePaths.GetRoot(_env.ContentRootPath), uploadSubFolder);
        if (!Directory.Exists(uploadPhysicalPath))
        {
            Directory.CreateDirectory(uploadPhysicalPath);
        }

        // Tên tệp lưu trữ vật lý hoàn toàn độc lập với tên tệp từ client (chống path traversal và rò rỉ metadata)
        var safeFileName = $"Anonymous_R{targetRound}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadPhysicalPath, safeFileName);

        try
        {
            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await file.CopyToAsync(stream);
        }
        catch { File.Delete(filePath); throw; }

                var relativeUrl = $"/Uploads/{uploadSubFolder.Replace("\\", "/")}/{safeFileName}";
        var displayTitle = $"BanThaoAnDanh_Vong{targetRound}{ext}";

        var thuMuc = new ThuMucBaiBao
        {
            MaBaiBao = maBaiBao,
            TenThuMuc = displayTitle,
            DuongDan = relativeUrl,
            LoaiThuMuc = "File ẩn danh",
            KichThuoc = file.Length,
            SoVong = targetRound,
            NgayTaiLen = WorkflowTools.VietnamNow
        };

        _context.ThuMucBaiBaos.Add(thuMuc);

        _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
        {
            MaBaiBao = maBaiBao,
            TrangThaiCu = baiBao.TrangThai,
            TrangThaiMoi = baiBao.TrangThai,
            NgayChuyen = WorkflowTools.VietnamNow,
            MaNguoiThucHien = maNguoiThucHien,
            GhiChu = $"Ban biên tập đã tải lên tệp bản thảo ẩn danh Vòng {targetRound}."
        });

        try { await _context.SaveChangesAsync(); }
        catch { File.Delete(filePath); throw; }
        return (true, $"Tải lên bản thảo ẩn danh Vòng {targetRound} thành công.", relativeUrl);
    }

    /// <summary>
    /// Ban biên tập tải lên tệp PDF xuất bản thành phẩm sau khi duyệt bản bông
    /// </summary>
    public async Task<(bool Success, string Message, string? DuongDan)> UploadPublishedPdfAsync(int maBaiBao, IFormFile file, int maNguoiThucHien)
    {
        var baiBao = await _context.BaiBaos.FindAsync(maBaiBao);
        if (baiBao == null) return (false, "Không tìm thấy bài báo.", null);
        if (baiBao.TrangThai != "Đã chấp nhận" && baiBao.TrangThai != "Đang chế bản" &&
            baiBao.TrangThai != "Sẵn sàng xuất bản")
            return (false, "Chỉ được tải PDF thành phẩm sau khi bài được chấp nhận.", null);

        var (isValid, errorMsg) = await ValidateUploadedFileAsync(file, new[] { ".pdf" }, 30 * 1024 * 1024);
        if (!isValid)
        {
            return (false, errorMsg, null);
        }

        var now = WorkflowTools.VietnamNow;
                var uploadSubFolder = Path.Combine("Published", now.Year.ToString(), now.Month.ToString("D2"));
                var uploadPhysicalPath = Path.Combine(UploadStoragePaths.GetRoot(_env.ContentRootPath), uploadSubFolder);
        if (!Directory.Exists(uploadPhysicalPath))
        {
            Directory.CreateDirectory(uploadPhysicalPath);
        }

        // Tên tệp vật lý an toàn độc lập
        var safeFileName = $"Published_{maBaiBao}_{Guid.NewGuid():N}.pdf";
        var filePath = Path.Combine(uploadPhysicalPath, safeFileName);

        try
        {
            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await file.CopyToAsync(stream);
        }
        catch { File.Delete(filePath); throw; }

                var relativeUrl = $"/Uploads/{uploadSubFolder.Replace("\\", "/")}/{safeFileName}";
        var displayTitle = $"XuatBan_BaiBao_{maBaiBao}.pdf";

        var thuMuc = new ThuMucBaiBao
        {
            MaBaiBao = maBaiBao,
            TenThuMuc = displayTitle,
            DuongDan = relativeUrl,
            LoaiThuMuc = "PDF thành phẩm",
            KichThuoc = file.Length,
            SoVong = 1,
            NgayTaiLen = WorkflowTools.VietnamNow
        };

        _context.ThuMucBaiBaos.Add(thuMuc);

        _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
        {
            MaBaiBao = maBaiBao,
            TrangThaiCu = baiBao.TrangThai,
            TrangThaiMoi = baiBao.TrangThai,
            NgayChuyen = WorkflowTools.VietnamNow,
            MaNguoiThucHien = maNguoiThucHien,
            GhiChu = $"Ban biên tập đã tải lên tệp PDF xuất bản thành phẩm."
        });

        try { await _context.SaveChangesAsync(); }
        catch { File.Delete(filePath); throw; }
        return (true, "Tải lên tệp PDF xuất bản thành phẩm thành công.", relativeUrl);
    }

    /// <summary>
    /// Ban biên tập xếp bài báo vào số tạp chí phát hành
    /// </summary>
    public async Task<(bool Success, string Message)> AssignToIssueAsync(int maBaiBao, AssignIssueDto dto, int maNguoiThucHien)
    {
        var baiBao = await _context.BaiBaos.FindAsync(maBaiBao);
        if (baiBao == null) return (false, "Không tìm thấy bài báo.");

        if (baiBao.TrangThai != "Đã chấp nhận" && baiBao.TrangThai != "Đang chế bản" && baiBao.TrangThai != "Sẵn sàng xuất bản")
            return (false, "Chỉ bài đã được chấp nhận mới có thể xếp vào số tạp chí.");

        var soTapChi = await _context.SoTapChis.FindAsync(dto.MaSoTapChi);
        if (soTapChi == null) return (false, "Không tìm thấy số tạp chí.");
        if (soTapChi.TrangThai == "Đã xuất bản" || soTapChi.TrangThai == "Đã phát hành")
            return (false, "Số tạp chí đã phát hành; không thể thêm hoặc chuyển bài vào số này.");
        if (dto.TrangBatDau.HasValue != dto.TrangKetThuc.HasValue ||
            (dto.TrangBatDau.HasValue && (dto.TrangBatDau < 1 || dto.TrangKetThuc < dto.TrangBatDau)))
            return (false, "Khoảng trang phát hành không hợp lệ.");

        baiBao.MaSoTapChi = dto.MaSoTapChi;
        if (dto.TrangBatDau.HasValue) baiBao.TrangBatDau = dto.TrangBatDau.Value;
        if (dto.TrangKetThuc.HasValue) baiBao.TrangKetThuc = dto.TrangKetThuc.Value;
        if (!string.IsNullOrWhiteSpace(dto.MaDOI))
        {
            baiBao.MaDOI = dto.MaDOI;
        }
        baiBao.NgayCapNhat = WorkflowTools.VietnamNow;

        _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
        {
            MaBaiBao = maBaiBao,
            TrangThaiCu = baiBao.TrangThai,
            TrangThaiMoi = baiBao.TrangThai,
            NgayChuyen = WorkflowTools.VietnamNow,
            MaNguoiThucHien = maNguoiThucHien,
            GhiChu = $"Ban biên tập đã xếp bài báo vào {soTapChi.TenSo}."
        });

        await _context.SaveChangesAsync();
        return (true, $"Đã xếp bài báo vào {soTapChi.TenSo} thành công.");
    }
}
