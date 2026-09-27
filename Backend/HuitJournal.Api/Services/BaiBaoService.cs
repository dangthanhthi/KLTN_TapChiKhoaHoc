using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;

namespace HuitJournal.Api.Services;

public class BaiBaoService : IBaiBaoService
{
    private readonly QLTapChiKhoaHocContext _context;
    private readonly IWebHostEnvironment _env;

    public BaiBaoService(QLTapChiKhoaHocContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<(bool Success, string Message, int? MaBaiBao, string? MaDinhDanh)> SubmitPaperAsync(int maNguoiDung, BaiBaoSubmitDto dto)
    {
        try
        {
            // 1. Kiểm tra người dùng nộp bài
            var author = await _context.NguoiDungs
                .Include(u => u.NguoiDungChuyenMons)
                .FirstOrDefaultAsync(u => u.MaNguoiDung == maNguoiDung);

            if (author == null)
            {
                return (false, "Không tìm thấy thông tin tác giả nộp bài.", null, null);
            }

            // 2. Kiểm tra tính hợp lệ và an toàn bảo mật của tệp tải lên (OWASP Validation) trước khi ghi CSDL
            if (dto.TapTinBanThao != null && dto.TapTinBanThao.Length > 0)
            {
                var (isValidFile, fileErr) = await ValidateUploadedFileAsync(dto.TapTinBanThao, new[] { ".pdf", ".docx", ".doc" }, 30 * 1024 * 1024);
                if (!isValidFile)
                {
                    return (false, fileErr, null, null);
                }
            }

            // 3. Đảm bảo ràng buộc chuyên môn (TRG_BaiBao_KiemTraChuyenMonTacGia)
            // Nếu tác giả chưa khai báo chuyên ngành này, tự động thêm vào danh mục chuyên môn của tác giả
            var hasDiscipline = author.NguoiDungChuyenMons.Any(cm => cm.MaChuyenNganh == dto.MaChuyenNganh);
            if (!hasDiscipline)
            {
                var chuyenNganhExists = await _context.ChuyenNganhs.AnyAsync(c => c.MaChuyenNganh == dto.MaChuyenNganh);
                if (!chuyenNganhExists)
                {
                    return (false, "Chuyên ngành được chọn không tồn tại trong hệ thống.", null, null);
                }

                _context.NguoiDungChuyenMons.Add(new NguoiDungChuyenMon
                {
                    MaNguoiDung = maNguoiDung,
                    MaChuyenNganh = dto.MaChuyenNganh,
                    LaChuyenMonChinh = false,
                    GhiChu = "Tự động bổ sung khi nộp bài bản thảo",
                    NgayDangKy = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            // 4. Khởi tạo đối tượng BaiBao
            var baiBao = new BaiBao
            {
                TieuDe = dto.TieuDe.Trim(),
                TieuDeTiengAnh = string.IsNullOrWhiteSpace(dto.TieuDeTiengAnh) ? null : dto.TieuDeTiengAnh.Trim(),
                TomTat = dto.TomTat.Trim(),
                TomTatTiengAnh = string.IsNullOrWhiteSpace(dto.TomTatTiengAnh) ? null : dto.TomTatTiengAnh.Trim(),
                TuKhoa = dto.TuKhoa.Trim(),
                TrangThai = "Chờ sơ duyệt",
                MaNguoiDung = maNguoiDung,
                MaChuyenNganh = dto.MaChuyenNganh,
                NgayGui = DateTime.Now,
                NgayCapNhat = DateTime.Now
            };

            _context.BaiBaos.Add(baiBao);
            await _context.SaveChangesAsync();

            // 5. Lưu tệp đính kèm vào thư mục Uploads/
            if (dto.TapTinBanThao != null && dto.TapTinBanThao.Length > 0)
            {

                var now = DateTime.Now;
                var uploadSubFolder = Path.Combine("Uploads", "Submissions", now.Year.ToString(), now.Month.ToString("D2"));
                var uploadPhysicalPath = Path.Combine(_env.ContentRootPath, uploadSubFolder);

                if (!Directory.Exists(uploadPhysicalPath))
                {
                    Directory.CreateDirectory(uploadPhysicalPath);
                }

                var ext = Path.GetExtension(dto.TapTinBanThao.FileName).ToLowerInvariant();
                var safeFileName = $"Submission_{baiBao.MaBaiBao}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadPhysicalPath, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.TapTinBanThao.CopyToAsync(stream);
                }

                var relativeUrl = $"/{uploadSubFolder.Replace("\\", "/")}/{safeFileName}";

                var thuMuc = new ThuMucBaiBao
                {
                    MaBaiBao = baiBao.MaBaiBao,
                    TenThuMuc = $"BanThaoGoc_{baiBao.MaBaiBao}{ext}",
                    DuongDan = relativeUrl,
                    LoaiThuMuc = "Bản thảo gốc",
                    KichThuoc = dto.TapTinBanThao.Length,
                    SoVong = 1,
                    NgayTaiLen = DateTime.Now
                };

                _context.ThuMucBaiBaos.Add(thuMuc);
            }

            // 5. Thêm nhóm đồng tác giả (nếu có)
            if (!string.IsNullOrWhiteSpace(dto.DongTacGiaJson))
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var coAuthors = JsonSerializer.Deserialize<List<DongTacGiaSubmitDto>>(dto.DongTacGiaJson, options);

                    if (coAuthors != null && coAuthors.Count > 0)
                    {
                        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        int order = 1;

                        foreach (var ca in coAuthors)
                        {
                            var cleanEmail = ca.Email.Trim().ToLower();

                            // Bỏ qua nếu trùng email với tác giả chính hoặc trùng email giữa các đồng tác giả
                            if (string.Equals(cleanEmail, author.Email, StringComparison.OrdinalIgnoreCase) || seenEmails.Contains(cleanEmail))
                            {
                                continue;
                            }
                            seenEmails.Add(cleanEmail);

                            // Tự động kiểm tra xem đồng tác giả này đã có tài khoản trong hệ thống hay chưa
                            var matchedUser = await _context.NguoiDungs
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);

                            var dongTacGia = new DongTacGia
                            {
                                MaBaiBao = baiBao.MaBaiBao,
                                HoTen = ca.HoTen.Trim(),
                                Email = cleanEmail,
                                DonVi = string.IsNullOrWhiteSpace(ca.DonVi) ? null : ca.DonVi.Trim(),
                                MaORCID = string.IsNullOrWhiteSpace(ca.MaORCID) ? null : ca.MaORCID.Trim(),
                                LaTacGiaLienHe = ca.LaTacGiaLienHe,
                                ThuTu = order++,
                                MaNguoiDung = matchedUser?.MaNguoiDung
                            };

                            _context.DongTacGias.Add(dongTacGia);
                        }
                    }
                }
                catch
                {
                    // Nếu parse JSON lỗi thì vẫn tiếp tục tạo bài báo
                }
            }

            // 5.1. Thêm danh sách chuyên gia phản biện do tác giả đề xuất (nếu có)
            if (!string.IsNullOrWhiteSpace(dto.PhanBienDeXuatJson))
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var proposedReviewers = JsonSerializer.Deserialize<List<PhanBienDeXuatSubmitDto>>(dto.PhanBienDeXuatJson, options);

                    if (proposedReviewers != null && proposedReviewers.Count > 0)
                    {
                        var seenRevEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var pr in proposedReviewers)
                        {
                            var cleanRevEmail = pr.Email.Trim().ToLower();
                            if (seenRevEmails.Contains(cleanRevEmail)) continue;
                            seenRevEmails.Add(cleanRevEmail);

                            // Kiểm tra xem chuyên gia này đã có tài khoản trong hệ thống hay chưa
                            int? matchedUserId = pr.MaNguoiDung;
                            if (!matchedUserId.HasValue)
                            {
                                var existingUser = await _context.NguoiDungs
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(u => u.Email.ToLower() == cleanRevEmail);
                                matchedUserId = existingUser?.MaNguoiDung;
                            }

                            var deXuat = new PhanBienDeXuat
                            {
                                MaBaiBao = baiBao.MaBaiBao,
                                HoTen = pr.HoTen.Trim(),
                                Email = cleanRevEmail,
                                DonVi = string.IsNullOrWhiteSpace(pr.DonVi) ? null : pr.DonVi.Trim(),
                                LinhVuc = string.IsNullOrWhiteSpace(pr.LinhVuc) ? null : pr.LinhVuc.Trim(),
                                LaChuyenGiaHeThong = pr.LaChuyenGiaHeThong || matchedUserId.HasValue,
                                MaNguoiDung = matchedUserId,
                                NgayTao = DateTime.Now
                            };

                            _context.PhanBienDeXuats.Add(deXuat);
                        }
                    }
                }
                catch
                {
                    // Nếu parse JSON lỗi thì vẫn tiếp tục
                }
            }

            // 6. Ghi vết lịch sử trạng thái ban đầu (Audit Trail)
            var lichSu = new LichSuTrangThaiBaiBao
            {
                MaBaiBao = baiBao.MaBaiBao,
                TrangThaiCu = null,
                TrangThaiMoi = "Chờ sơ duyệt",
                MaNguoiThucHien = maNguoiDung,
                NgayChuyen = DateTime.Now,
                GhiChu = "Tác giả nộp bản thảo mới qua Cổng thông tin Tạp chí."
            };
            _context.LichSuTrangThais.Add(lichSu);

            await _context.SaveChangesAsync();

            var maDinhDanh = $"JST-{DateTime.Now.Year}-SUB{baiBao.MaBaiBao:D4}";
            return (true, "Nộp bản thảo thành công!", baiBao.MaBaiBao, maDinhDanh);
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return (false, $"Lỗi cơ sở dữ liệu khi nộp bài: {msg}", null, null);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi hệ thống: {ex.Message}", null, null);
        }
    }

    public async Task<List<BaiBaoListItemDto>> GetMySubmissionsAsync(int maNguoiDung)
    {
        var user = await _context.NguoiDungs.FindAsync(maNguoiDung);
        if (user == null) return new List<BaiBaoListItemDto>();

        var userEmail = user.Email.ToLower();

        var query = _context.BaiBaos
            .AsNoTracking()
            .Where(b => b.MaNguoiDung == maNguoiDung || b.DongTacGias.Any(d => d.MaNguoiDung == maNguoiDung || d.Email.ToLower() == userEmail))
            .OrderByDescending(b => b.NgayGui)
            .Select(b => new BaiBaoListItemDto
            {
                MaBaiBao = b.MaBaiBao,
                MaDinhDanh = $"JST-{b.NgayGui.Year}-SUB{b.MaBaiBao:D4}",
                TieuDe = b.TieuDe,
                TieuDeTiengAnh = b.TieuDeTiengAnh,
                ChuyenNganh = b.ChuyenNganh.TenChuyenNganh,
                MaChuyenNganh = b.MaChuyenNganh,
                TrangThai = b.TrangThai,
                NgayGui = b.NgayGui,
                NgayCapNhat = b.NgayCapNhat,
                SoDongTacGia = b.DongTacGias.Count,
                TapTinGoc = b.ThuMucBaiBaos.Where(f => f.LoaiThuMuc == "Bản thảo gốc").Select(f => f.TenThuMuc).FirstOrDefault()
            });

        return await query.ToListAsync();
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
                NguoiThucHien = isEditorOrAdmin 
                    ? ls.NguoiThucHien?.HoTen 
                    : (ls.MaNguoiThucHien == b.MaNguoiDung ? b.TacGia.HoTen : "Ban biên tập"),
                GhiChu = isEditorOrAdmin 
                    ? ls.GhiChu 
                    : GetAuthorSafeHistoryNote(ls)
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
            "Đã xuất bản" => "Bài báo đã được xuất bản chính thức trên Cổng thông tin Tạp chí.",
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
            TacGias = authors
        };
    }

    public async Task<(bool Success, string Message)> ResubmitPaperAsync(int maBaiBao, int maNguoiDung, BaiBaoResubmitDto dto)
    {
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
        // Chỉ cho phép tác giả nộp lại bản thảo chỉnh sửa & giải trình BM-03 khi bài ở trạng thái: 'Chờ chỉnh sửa'
        if (baiBao.TrangThai != "Chờ chỉnh sửa")
        {
            return (false, $"Bài báo đang ở trạng thái '{baiBao.TrangThai}', chỉ được phép nộp bản chỉnh sửa và giải trình BM-03 khi bài ở trạng thái 'Chờ chỉnh sửa'.");
        }

        var trangThaiCu = baiBao.TrangThai;
        baiBao.TrangThai = "Chờ quyết định";
        baiBao.NgayCapNhat = DateTime.Now;

        // Tính số vòng chỉnh sửa dựa trên các vòng phân công trước đó
        var currentAssignmentRound = await _context.PhanCongPhanBiens
            .Where(p => p.MaBaiBao == maBaiBao)
            .Select(p => (int?)p.SoVong)
            .MaxAsync() ?? 1;
        var revisionRound = currentAssignmentRound + 1;

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

        var uploadDir = Path.Combine(_env.ContentRootPath, "Uploads", "revisions", $"paper_{maBaiBao}");
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
                TenThuMuc = $"BanChinhSua_Vong{revisionRound}{ext}",
                DuongDan = $"/Uploads/revisions/paper_{maBaiBao}/{fileName}",
                LoaiThuMuc = "Bản chỉnh sửa",
                KichThuoc = dto.FileClean.Length,
                SoVong = revisionRound,
                NgayTaiLen = DateTime.Now
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
                NgayTaiLen = DateTime.Now
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
                NgayTaiLen = DateTime.Now
            });
        }

        baiBao.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
        {
            TrangThaiCu = trangThaiCu,
            TrangThaiMoi = "Chờ quyết định",
            NgayChuyen = DateTime.Now,
            MaNguoiThucHien = maNguoiDung,
            GhiChu = $"Tác giả nộp bản thảo chỉnh sửa và giải trình BM-03: {dto.GiaiTrinh}"
        });

        await _context.SaveChangesAsync();

        return (true, "Đã nộp bản thảo chỉnh sửa và giải trình BM-03 thành công tới Ban biên tập.");
    }

    public async Task<(bool Success, string Message, string? PhysicalPath, string? FileName, string? ContentType)> GetManuscriptForAuthorAsync(int maBaiBao, int maNguoiDung, bool isEditorOrAdmin)
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
            .OrderByDescending(f => f.SoVong)
            .ThenByDescending(f => f.NgayTaiLen)
            .FirstOrDefault();

        if (fileRecord == null)
        {
            return (false, "Hồ sơ bài báo này chưa có tệp đính kèm.", null, null, null);
        }

        var cleanPath = fileRecord.DuongDan.TrimStart('/', '\\');
        var basePath = _env.ContentRootPath;
        var candidate1 = Path.Combine(basePath, cleanPath.Replace('/', Path.DirectorySeparatorChar));
        var candidate2 = Path.Combine(basePath, "wwwroot", cleanPath.Replace('/', Path.DirectorySeparatorChar));
        var physicalPath = File.Exists(candidate1) ? candidate1 : (File.Exists(candidate2) ? candidate2 : null);

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
            .FirstOrDefault();

        if (fileRecord == null)
        {
            return (false, "Tệp PDF thành phẩm của bài báo chưa được phát hành trên hệ thống.", null, null, null);
        }

        var cleanPath = fileRecord.DuongDan.TrimStart('/', '\\');
        var basePath = _env.ContentRootPath;
        var candidate1 = Path.Combine(basePath, cleanPath.Replace('/', Path.DirectorySeparatorChar));
        var candidate2 = Path.Combine(basePath, "wwwroot", cleanPath.Replace('/', Path.DirectorySeparatorChar));
        var candidate3 = Path.Combine(basePath, "Uploads", Path.GetFileName(cleanPath));
        var physicalPath = File.Exists(candidate1) ? candidate1 : (File.Exists(candidate2) ? candidate2 : (File.Exists(candidate3) ? candidate3 : null));

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

        var now = DateTime.Now;
        var uploadSubFolder = Path.Combine("Uploads", "Submissions", now.Year.ToString(), now.Month.ToString("D2"));
        var uploadPhysicalPath = Path.Combine(_env.ContentRootPath, uploadSubFolder);
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

        var relativeUrl = $"/{uploadSubFolder.Replace("\\", "/")}/{safeFileName}";
        var displayTitle = $"BanThaoAnDanh_Vong{targetRound}{ext}";

        var thuMuc = new ThuMucBaiBao
        {
            MaBaiBao = maBaiBao,
            TenThuMuc = displayTitle,
            DuongDan = relativeUrl,
            LoaiThuMuc = "File ẩn danh",
            KichThuoc = file.Length,
            SoVong = targetRound,
            NgayTaiLen = DateTime.Now
        };

        _context.ThuMucBaiBaos.Add(thuMuc);

        _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
        {
            MaBaiBao = maBaiBao,
            TrangThaiCu = baiBao.TrangThai,
            TrangThaiMoi = baiBao.TrangThai,
            NgayChuyen = DateTime.Now,
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

        var now = DateTime.Now;
        var uploadSubFolder = Path.Combine("Uploads", "Published", now.Year.ToString(), now.Month.ToString("D2"));
        var uploadPhysicalPath = Path.Combine(_env.ContentRootPath, uploadSubFolder);
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

        var relativeUrl = $"/{uploadSubFolder.Replace("\\", "/")}/{safeFileName}";
        var displayTitle = $"XuatBan_BaiBao_{maBaiBao}.pdf";

        var thuMuc = new ThuMucBaiBao
        {
            MaBaiBao = maBaiBao,
            TenThuMuc = displayTitle,
            DuongDan = relativeUrl,
            LoaiThuMuc = "PDF thành phẩm",
            KichThuoc = file.Length,
            SoVong = 1,
            NgayTaiLen = DateTime.Now
        };

        _context.ThuMucBaiBaos.Add(thuMuc);

        _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
        {
            MaBaiBao = maBaiBao,
            TrangThaiCu = baiBao.TrangThai,
            TrangThaiMoi = baiBao.TrangThai,
            NgayChuyen = DateTime.Now,
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
        baiBao.NgayCapNhat = DateTime.Now;

        _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
        {
            MaBaiBao = maBaiBao,
            TrangThaiCu = baiBao.TrangThai,
            TrangThaiMoi = baiBao.TrangThai,
            NgayChuyen = DateTime.Now,
            MaNguoiThucHien = maNguoiThucHien,
            GhiChu = $"Ban biên tập đã xếp bài báo vào {soTapChi.TenSo}."
        });

        await _context.SaveChangesAsync();
        return (true, $"Đã xếp bài báo vào {soTapChi.TenSo} thành công.");
    }
}
