using Microsoft.EntityFrameworkCore;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;

namespace HuitJournal.Api.Services;

public class SoTapChiService : ISoTapChiService
{
    private readonly QLTapChiKhoaHocContext _context;

    public SoTapChiService(QLTapChiKhoaHocContext context)
    {
        _context = context;
    }

    public async Task<List<SoTapChiListItemDto>> GetPublishedIssuesAsync()
    {
        var issues = await _context.SoTapChis
            .AsNoTracking()
            .Where(s => (s.TrangThai == "Đã xuất bản" || s.TrangThai == "Đã phát hành")
                     && s.BaiBaos.Any(b => b.TrangThai == "Đã xuất bản"
                                       && b.ThuMucBaiBaos.Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                                                               && f.TenThuMuc.EndsWith(".pdf"))))
            .OrderByDescending(s => s.Nam)
            .ThenByDescending(s => s.Tap)
            .ThenByDescending(s => s.So)
            .Select(s => new SoTapChiListItemDto
            {
                MaSoTapChi = s.MaSoTapChi,
                TenSo = s.TenSo,
                Tap = s.Tap,
                So = s.So,
                Nam = s.Nam,
                NgayPhatHanh = s.NgayPhatHanh,
                TrangThai = s.TrangThai,
                TongSoBaiBao = s.BaiBaos.Count(b => b.TrangThai == "Đã xuất bản"
                                                 && b.ThuMucBaiBaos.Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                                                                         && f.TenThuMuc.EndsWith(".pdf"))),
                AnhBiaUrl = s.TenSo.Contains("Yersin")
                    ? $"assets/images/cover_yersin_no{s.So}.svg"
                    : $"assets/images/cover_huit_vol{s.Tap}_no{s.So}e.jpg"
            })
            .ToListAsync();

        return issues;
    }

    public async Task<SoTapChiDetailDto?> GetIssueDetailAsync(int maSoTapChi)
    {
        var s = await _context.SoTapChis
            .AsNoTracking()
            .Include(x => x.BaiBaos.Where(b => b.TrangThai == "Đã xuất bản"
                                           && b.ThuMucBaiBaos.Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                                                                   && f.TenThuMuc.EndsWith(".pdf"))))
                .ThenInclude(b => b.TacGia)
            .Include(x => x.BaiBaos.Where(b => b.TrangThai == "Đã xuất bản"
                                           && b.ThuMucBaiBaos.Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                                                                   && f.TenThuMuc.EndsWith(".pdf"))))
                .ThenInclude(b => b.ChuyenNganh)
            .Include(x => x.BaiBaos.Where(b => b.TrangThai == "Đã xuất bản"
                                           && b.ThuMucBaiBaos.Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                                                                   && f.TenThuMuc.EndsWith(".pdf"))))
                .ThenInclude(b => b.DongTacGias)
            .Include(x => x.BaiBaos.Where(b => b.TrangThai == "Đã xuất bản"
                                           && b.ThuMucBaiBaos.Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                                                                   && f.TenThuMuc.EndsWith(".pdf"))))
                .ThenInclude(b => b.ThuMucBaiBaos)
            .FirstOrDefaultAsync(x => x.MaSoTapChi == maSoTapChi);

        if (s == null || (s.TrangThai != "Đã xuất bản" && s.TrangThai != "Đã phát hành"))
        {
            return null;
        }

        var articles = s.BaiBaos
            .OrderBy(b => b.TrangBatDau ?? 999)
            .Select(b =>
            {
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

                var isIssuePublished = s.TrangThai == "Đã xuất bản" || s.TrangThai == "Đã phát hành";
                var hasPublishedPdf = b.TrangThai == "Đã xuất bản"
                    && isIssuePublished
                    && b.ThuMucBaiBaos.Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                                             && f.TenThuMuc.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

                var publicPdfUrl = hasPublishedPdf ? $"/api/baibao/public/{b.MaBaiBao}/pdf" : null;

                return new BaiBaoPublicDto
                {
                    MaBaiBao = b.MaBaiBao,
                    MaSoTapChi = s.MaSoTapChi,
                    TieuDe = b.TieuDe,
                    TieuDeTiengAnh = b.TieuDeTiengAnh,
                    TomTat = b.TomTat,
                    TomTatTiengAnh = b.TomTatTiengAnh,
                    TuKhoa = b.TuKhoa,
                    MaDOI = b.MaDOI,
                    NgayGui = b.NgayGui,
                    NgayPhatHanh = s.NgayPhatHanh,
                    ChuyenNganh = b.ChuyenNganh.TenChuyenNganh,
                    TenSoTapChi = s.TenSo,
                    Tap = s.Tap,
                    So = s.So,
                    Nam = s.Nam,
                    TrangBatDau = b.TrangBatDau,
                    TrangKetThuc = b.TrangKetThuc,
                    FilePdfUrl = publicPdfUrl,
                    TacGias = authors
                };
            }).ToList();

        return new SoTapChiDetailDto
        {
            MaSoTapChi = s.MaSoTapChi,
            TenSo = s.TenSo,
            Tap = s.Tap,
            So = s.So,
            Nam = s.Nam,
            NgayPhatHanh = s.NgayPhatHanh,
            TrangThai = s.TrangThai,
            AnhBiaUrl = s.TenSo.Contains("Yersin")
                ? $"assets/images/cover_yersin_no{s.So}.svg"
                : $"assets/images/cover_huit_vol{s.Tap}_no{s.So}e.jpg",
            MoTa = s.TenSo.Contains("Yersin")
                ? $"Ấn phẩm {s.TenSo}, phát hành chính thức ngày {s.NgayPhatHanh:dd/MM/yyyy}."
                : $"Ấn phẩm Tạp chí Khoa học Đại học Công Thương Tập {s.Tap} Số {s.So} Năm {s.Nam}, phát hành chính thức ngày {s.NgayPhatHanh:dd/MM/yyyy}.",
            DanhSachBaiBao = articles
        };
    }

    public async Task<List<BaiBaoPublicDto>> GetLatestArticlesAsync(int limit = 10)
    {
        // A non-positive limit is used by the archive to request the complete published set.
        var publishedArticles = _context.BaiBaos
            .AsNoTracking()
            .Where(b => b.TrangThai == "Đã xuất bản" && b.SoTapChi != null && (b.SoTapChi.TrangThai == "Đã xuất bản" || b.SoTapChi.TrangThai == "Đã phát hành")
                     && b.ThuMucBaiBaos.Any(f => (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                                             && f.TenThuMuc.EndsWith(".pdf")))
            .OrderByDescending(b => b.NgayCapNhat);
        var targetIds = limit > 0
            ? await publishedArticles.Take(limit).Select(b => b.MaBaiBao).ToListAsync()
            : await publishedArticles.Select(b => b.MaBaiBao).ToListAsync();

        if (targetIds.Count == 0)
        {
            return new List<BaiBaoPublicDto>();
        }

        var rawArticles = await _context.BaiBaos
            .AsNoTracking()
            .Where(b => targetIds.Contains(b.MaBaiBao))
            .Select(b => new
            {
                b.MaBaiBao,
                b.MaSoTapChi,
                b.TieuDe,
                b.TieuDeTiengAnh,
                b.TomTat,
                b.TomTatTiengAnh,
                b.TuKhoa,
                b.MaDOI,
                b.NgayGui,
                NgayPhatHanh = b.SoTapChi != null ? b.SoTapChi.NgayPhatHanh : (DateTime?)null,
                ChuyenNganh = b.ChuyenNganh != null ? b.ChuyenNganh.TenChuyenNganh : "",
                TenSoTapChi = b.SoTapChi != null ? b.SoTapChi.TenSo : null,
                Tap = b.SoTapChi != null ? b.SoTapChi.Tap : (int?)null,
                So = b.SoTapChi != null ? b.SoTapChi.So : (int?)null,
                Nam = b.SoTapChi != null ? b.SoTapChi.Nam : (int?)null,
                b.TrangBatDau,
                b.TrangKetThuc,
                TacGiaChinh = b.TacGia != null ? new DongTacGiaDetailDto
                {
                    HoTen = b.TacGia.HoTen,
                    Email = b.TacGia.Email,
                    DonVi = b.TacGia.DonVi,
                    MaORCID = b.TacGia.MaORCID,
                    LaTacGiaLienHe = true,
                    ThuTu = 0,
                    MaNguoiDung = b.TacGia.MaNguoiDung
                } : null
            })
            .ToListAsync();

        var orderMap = targetIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
        var orderedArticles = rawArticles.OrderBy(a => orderMap.TryGetValue(a.MaBaiBao, out var idx) ? idx : int.MaxValue).ToList();

        var coAuthors = await _context.DongTacGias
            .AsNoTracking()
            .Where(d => targetIds.Contains(d.MaBaiBao))
            .OrderBy(d => d.ThuTu)
            .Select(d => new
            {
                d.MaBaiBao,
                Dto = new DongTacGiaDetailDto
                {
                    MaDongTacGia = d.MaDongTacGia,
                    HoTen = d.HoTen,
                    Email = d.Email,
                    DonVi = d.DonVi,
                    MaORCID = d.MaORCID,
                    LaTacGiaLienHe = d.LaTacGiaLienHe,
                    ThuTu = d.ThuTu,
                    MaNguoiDung = d.MaNguoiDung
                }
            })
            .ToListAsync();

        var coAuthorsByArticle = coAuthors
            .GroupBy(d => d.MaBaiBao)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

        return orderedArticles.Select(b =>
        {
            var authors = new List<DongTacGiaDetailDto>();
            if (b.TacGiaChinh != null)
            {
                authors.Add(b.TacGiaChinh);
            }
            if (coAuthorsByArticle.TryGetValue(b.MaBaiBao, out var listDongTacGia) && listDongTacGia.Count > 0)
            {
                authors.AddRange(listDongTacGia);
            }

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
                NgayPhatHanh = b.NgayPhatHanh,
                ChuyenNganh = b.ChuyenNganh,
                TenSoTapChi = b.TenSoTapChi,
                Tap = b.Tap,
                So = b.So,
                Nam = b.Nam,
                TrangBatDau = b.TrangBatDau,
                TrangKetThuc = b.TrangKetThuc,
                FilePdfUrl = $"/api/baibao/public/{b.MaBaiBao}/pdf",
                AnhBiaUrl = b.TenSoTapChi != null
                    ? (b.TenSoTapChi.Contains("Yersin")
                        ? $"assets/images/cover_yersin_no{b.So}.svg"
                        : $"assets/images/cover_huit_vol{b.Tap}_no{b.So}e.jpg")
                    : null,
                TacGias = authors
            };
        }).ToList();
    }
}
