using Microsoft.EntityFrameworkCore;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;

namespace HuitJournal.Api.Services;

public class PhanBienService : IPhanBienService
{
    private readonly QLTapChiKhoaHocContext _context;
    private readonly IWebHostEnvironment _env;

    public PhanBienService(QLTapChiKhoaHocContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<(bool Success, string Message, int? MaPhanCong)> AssignReviewerAsync(int maNguoiThucHien, PhanCongRequestDto dto)
    {
        try
        {
            var baiBao = await _context.BaiBaos.FindAsync(dto.MaBaiBao);
            if (baiBao == null)
            {
                return (false, "Không tìm thấy bài báo.", null);
            }

            if (baiBao.TrangThai != "Chờ sơ duyệt" &&
                baiBao.TrangThai != "Chờ quyết định" &&
                baiBao.TrangThai != "Đang phản biện")
            {
                return (false, $"Không thể phân công phản biện khi bài đang ở trạng thái '{baiBao.TrangThai}'.", null);
            }

            var reviewer = await _context.NguoiDungs.FindAsync(dto.MaNguoiDungReviewer);
            if (reviewer == null)
            {
                return (false, "Không tìm thấy chuyên gia phản biện.", null);
            }
            if (!reviewer.TrangThai || !await _context.NguoiDungVaiTros.AnyAsync(r =>
                    r.MaNguoiDung == reviewer.MaNguoiDung && r.VaiTro.TenVaiTro == "Chuyên gia phản biện"))
            {
                return (false, "Tài khoản chưa hoạt động hoặc chưa có vai trò chuyên gia phản biện.", null);
            }

            var responseDue = (dto.HanPhanHoi ?? DateTime.Today.AddDays(7)).Date;
            var completionDue = (dto.HanHoanThanh ?? DateTime.Today.AddDays(21)).Date;
            if (responseDue < DateTime.Today || completionDue < responseDue.AddDays(3))
            {
                return (false, "Hạn phản hồi phải từ hôm nay; hạn hoàn thành phải sau hạn phản hồi ít nhất 3 ngày.", null);
            }

            // Xác định số vòng phản biện mục tiêu:
            // Ưu tiên dto.SoVong nếu truyền vào
            int targetRound;
            if (dto.SoVong.HasValue && dto.SoVong.Value > 0)
            {
                targetRound = dto.SoVong.Value;
            }
            else
            {
                // Kiểm tra vòng cao nhất của bản sửa hoặc bản thảo ẩn danh
                var maxRevisionRound = await _context.ThuMucBaiBaos
                    .Where(f => f.MaBaiBao == dto.MaBaiBao && (f.LoaiThuMuc == "Bản chỉnh sửa" || f.LoaiThuMuc == "File ẩn danh" || f.LoaiThuMuc == "Bản thảo ẩn danh"))
                    .Select(f => (int?)f.SoVong)
                    .MaxAsync() ?? 1;

                var maxAssignmentRound = await _context.PhanCongPhanBiens
                    .Where(p => p.MaBaiBao == dto.MaBaiBao)
                    .Select(p => (int?)p.SoVong)
                    .MaxAsync() ?? 1;

                targetRound = Math.Max(maxRevisionRound, maxAssignmentRound);
            }

            // Ràng buộc quy chuẩn Phản biện kín hai chiều (COPE Double-Blind):
            // Tệp ẩn danh phải thuộc đúng vòng phân công.
            var hasAnonymous = await _context.ThuMucBaiBaos.AnyAsync(f =>
                f.MaBaiBao == dto.MaBaiBao &&
                (f.LoaiThuMuc == "File ẩn danh" || f.LoaiThuMuc == "Bản thảo ẩn danh") &&
                f.SoVong == targetRound);

            if (!hasAnonymous)
            {
                return (false, $"Bài báo #{dto.MaBaiBao} chưa có tệp 'File ẩn danh' cho Vòng {targetRound}. Để đảm bảo tính khách quan và tuân thủ quy chuẩn phản biện kín hai chiều (Double-Blind), Ban biên tập cần tải lên bản thảo ẩn danh Vòng {targetRound} trước khi phân công chuyên gia.", null);
            }

            if (await _context.PhanCongPhanBiens.AnyAsync(p => p.MaBaiBao == dto.MaBaiBao &&
                    p.MaNguoiDung == dto.MaNguoiDungReviewer && p.SoVong == targetRound))
            {
                return (false, "Chuyên gia này đã được phân công cho bài báo ở cùng vòng phản biện.", null);
            }

            var existingRoundAssignments = await _context.PhanCongPhanBiens.CountAsync(p =>
                p.MaBaiBao == dto.MaBaiBao && p.SoVong == targetRound && p.TrangThai != "Từ chối phản biện");

            var phanCong = new PhanCongPhanBien
            {
                MaBaiBao = dto.MaBaiBao,
                MaNguoiDung = dto.MaNguoiDungReviewer,
                SoVong = targetRound,
                NgayPhanCong = DateTime.Now,
                HanPhanHoi = responseDue,
                HanHoanThanh = completionDue,
                TrangThai = "Chờ phản hồi"
            };

            _context.PhanCongPhanBiens.Add(phanCong);

            if (existingRoundAssignments >= 2 && !string.IsNullOrWhiteSpace(dto.LyDo))
            {
                _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
                {
                    MaBaiBao = baiBao.MaBaiBao,
                    TrangThaiCu = baiBao.TrangThai,
                    TrangThaiMoi = baiBao.TrangThai,
                    MaNguoiThucHien = maNguoiThucHien,
                    NgayChuyen = DateTime.Now,
                    GhiChu = $"Ban biên tập mời phản biện bổ sung Vòng {targetRound}. Lý do: {dto.LyDo.Trim()}"
                });
            }

            // SQL Server Trigger TRG_PhanCongPhanBien_KiemTraChuyenMon sẽ tự động kiểm tra:
            // 1. Chuyên ngành chuyên gia có khớp chuyên ngành bài báo không.
            // 2. Chuyên gia có trùng với tác giả chính hoặc bất kỳ đồng tác giả nào (theo Id hoặc Email) không.
            await _context.SaveChangesAsync();

            return (true, $"Đã phân công chuyên gia {reviewer.HoTen} phản biện bài báo thành công!", phanCong.MaPhanCong);
        }
        catch (DbUpdateException ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return (false, $"Ràng buộc nghiệp vụ tòa soạn (Trigger SQL): {innerMsg}", null);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi: {ex.Message}", null);
        }
    }

    public async Task<List<PhanCongListItemDto>> GetMyReviewAssignmentsAsync(int maReviewer)
    {
        var assignments = await _context.PhanCongPhanBiens
            .AsNoTracking()
            .Where(p => p.MaNguoiDung == maReviewer)
            .OrderByDescending(p => p.NgayPhanCong)
            .Select(p => new PhanCongListItemDto
            {
                MaPhanCong = p.MaPhanCong,
                MaBaiBao = p.MaBaiBao,
                TieuDeBaiBao = p.BaiBao.TieuDe,
                ChuyenNganh = p.BaiBao.ChuyenNganh.TenChuyenNganh,
                SoVong = p.SoVong,
                NgayPhanCong = p.NgayPhanCong,
                HanPhanHoi = p.HanPhanHoi,
                HanHoanThanh = p.HanHoanThanh,
                TrangThai = p.TrangThai,
                DaDanhGia = p.PhieuDanhGia != null,
                DiemTongKet = p.PhieuDanhGia != null ? p.PhieuDanhGia.DiemTongKet : null,
                KienNghi = p.PhieuDanhGia != null ? p.PhieuDanhGia.KienNghi : null
            })
            .ToListAsync();

        return assignments;
    }

    public async Task<(bool Success, string Message)> RespondToAssignmentAsync(int maPhanCong, int maReviewer, bool accept)
    {
        var articleId = await _context.PhanCongPhanBiens.AsNoTracking()
            .Where(p => p.MaPhanCong == maPhanCong && p.MaNguoiDung == maReviewer)
            .Select(p => (int?)p.MaBaiBao)
            .FirstOrDefaultAsync();
        if (articleId == null) return (false, "Không tìm thấy lời mời phản biện của bạn.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        // Serialize responses for the same article so two simultaneous acceptances
        // cannot each see only one accepted reviewer and leave the article waiting.
        var lockName = $"HuitJournal:ReviewResponse:{articleId.Value}";
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock
                @Resource = {lockName}, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @lockResult < 0 THROW 51000, 'Khong the khoa luot phan bien.', 1;
            """);

        var assignment = await _context.PhanCongPhanBiens
            .Include(p => p.BaiBao)
            .FirstOrDefaultAsync(p => p.MaPhanCong == maPhanCong && p.MaNguoiDung == maReviewer);
        if (assignment == null) return (false, "Không tìm thấy lời mời phản biện của bạn.");
        if (assignment.TrangThai != "Chờ phản hồi") return (false, "Lời mời này đã được xử lý. Hãy làm mới danh sách.");
        if (assignment.BaiBao.TrangThai is not ("Chờ sơ duyệt" or "Chờ quyết định" or "Đang phản biện"))
            return (false, "Bản thảo không còn trong quy trình phản biện.");

        assignment.TrangThai = accept ? "Đồng ý phản biện" : "Từ chối phản biện";
        await _context.SaveChangesAsync();

        if (accept && assignment.BaiBao.TrangThai is "Chờ sơ duyệt" or "Chờ quyết định")
        {
            var acceptedCount = await _context.PhanCongPhanBiens.CountAsync(p =>
                p.MaBaiBao == articleId.Value && p.SoVong == assignment.SoVong &&
                (p.TrangThai == "Đồng ý phản biện" || p.TrangThai == "Đang đánh giá" || p.TrangThai == "Đã đánh giá"));

            if (acceptedCount >= 2)
            {
                var oldStatus = assignment.BaiBao.TrangThai;
                assignment.BaiBao.TrangThai = "Đang phản biện";
                assignment.BaiBao.NgayCapNhat = DateTime.Now;
                _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
                {
                    MaBaiBao = articleId.Value,
                    TrangThaiCu = oldStatus,
                    TrangThaiMoi = "Đang phản biện",
                    MaNguoiThucHien = null,
                    NgayChuyen = DateTime.Now,
                    GhiChu = $"Hệ thống ghi nhận đủ {acceptedCount} chuyên gia đã nhận lời phản biện kín Vòng {assignment.SoVong}."
                });
                await _context.SaveChangesAsync();
            }
        }

        await transaction.CommitAsync();
        return (true, accept ? "Đã nhận lời phản biện." : "Đã từ chối lời mời phản biện.");
    }

    public Task<PhieuDanhGiaDetailDto?> GetMyEvaluationAsync(int maPhanCong, int maReviewer) =>
        _context.PhanCongPhanBiens.AsNoTracking()
            .Where(p => p.MaPhanCong == maPhanCong && p.MaNguoiDung == maReviewer && p.PhieuDanhGia != null)
            .Select(p => new PhieuDanhGiaDetailDto
            {
                MaPhanCong = p.MaPhanCong,
                DiemTinhMoi = p.PhieuDanhGia!.DiemTinhMoi,
                DiemPhuongPhap = p.PhieuDanhGia.DiemPhuongPhap,
                DiemKetQua = p.PhieuDanhGia.DiemKetQua,
                DiemTrinhBay = p.PhieuDanhGia.DiemTrinhBay,
                DiemTongKet = p.PhieuDanhGia.DiemTongKet,
                NhanXetChoTacGia = p.PhieuDanhGia.NhanXetChoTacGia,
                NhanXetBaoMat = p.PhieuDanhGia.NhanXetBaoMat,
                KienNghi = p.PhieuDanhGia.KienNghi,
                NgayDanhGia = p.PhieuDanhGia.NgayDanhGia
            }).FirstOrDefaultAsync();

    public async Task<(bool Success, string Message)> SubmitEvaluationAsync(int maReviewer, PhieuDanhGiaDto dto)
    {
        try
        {
            var phanCong = await _context.PhanCongPhanBiens
                .Include(p => p.PhieuDanhGia)
                .Include(p => p.BaiBao)
                .FirstOrDefaultAsync(p => p.MaPhanCong == dto.MaPhanCong);

            if (phanCong == null)
            {
                return (false, "Không tìm thấy phân công phản biện tương ứng.");
            }

            if (phanCong.MaNguoiDung != maReviewer)
            {
                return (false, "Bạn không có quyền đánh giá bài báo này.");
            }

            if (phanCong.BaiBao.TrangThai != "Đang phản biện" ||
                phanCong.TrangThai is not ("Đồng ý phản biện" or "Đang đánh giá"))
            {
                return (false, "Cần nhận lời mời và chờ vòng phản biện bắt đầu trước khi gửi phiếu.");
            }

            if (phanCong.PhieuDanhGia != null)
            {
                return (false, "Phiếu BM-04 đã được gửi. Liên hệ Ban biên tập nếu cần đính chính.");
            }
            else
            {
                var recommendations = new HashSet<string>(StringComparer.Ordinal)
                { "Chấp nhận đăng", "Chỉnh sửa nhỏ", "Chỉnh sửa lớn và phản biện lại", "Từ chối đăng" };
                if (dto.DiemTinhMoi is null or < 0 or > 10 || dto.DiemPhuongPhap is null or < 0 or > 10 ||
                    dto.DiemKetQua is null or < 0 or > 10 || dto.DiemTrinhBay is null or < 0 or > 10 ||
                    string.IsNullOrWhiteSpace(dto.NhanXetChoTacGia) || dto.NhanXetChoTacGia.Length > 20000 ||
                    dto.NhanXetBaoMat?.Length > 20000 || !recommendations.Contains(dto.KienNghi ?? ""))
                {
                    return (false, "Phiếu BM-04 thiếu điểm, nhận xét hoặc kiến nghị hợp lệ.");
                }
                var total = Math.Round((dto.DiemTinhMoi.Value + dto.DiemPhuongPhap.Value +
                    dto.DiemKetQua.Value + dto.DiemTrinhBay.Value) / 4m, 1, MidpointRounding.AwayFromZero);
                // Tạo mới phiếu đánh giá BM-04
                var phieu = new PhieuDanhGia
                {
                    MaPhanCong = dto.MaPhanCong,
                    DiemTinhMoi = dto.DiemTinhMoi,
                    DiemPhuongPhap = dto.DiemPhuongPhap,
                    DiemKetQua = dto.DiemKetQua,
                    DiemTrinhBay = dto.DiemTrinhBay,
                    DiemTongKet = total,
                    NhanXetChoTacGia = dto.NhanXetChoTacGia.Trim(),
                    NhanXetBaoMat = dto.NhanXetBaoMat,
                    KienNghi = dto.KienNghi,
                    NgayDanhGia = DateTime.Now
                };
                _context.PhieuDanhGias.Add(phieu);
            }

            phanCong.TrangThai = "Đã đánh giá";
            phanCong.BaiBao.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, "Nộp phiếu nhận xét và đánh giá phản biện (BM-04) thành công!");
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return (false, $"Lỗi khi nộp phiếu đánh giá: {innerMsg}");
        }
    }

    public async Task<(bool Success, string Message)> MakeEditorialDecisionAsync(int maEditor, QuyetDinhBienTapDto dto)
    {
        try
        {
            var baiBao = await _context.BaiBaos.FindAsync(dto.MaBaiBao);
            if (baiBao == null)
            {
                return (false, "Không tìm thấy bài báo.");
            }

            var oldStatus = baiBao.TrangThai;
            var targetStatus = (dto.TrangThaiMoi ?? "").Trim();
            if (targetStatus.Equals("Yêu cầu chỉnh sửa", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = "Chờ chỉnh sửa";
            }

            var validStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Chờ sơ duyệt", "Chờ sửa hình thức", "Đang phản biện", "Chờ chỉnh sửa",
                "Chờ quyết định", "Đã chấp nhận", "Đang chế bản", "Sẵn sàng xuất bản",
                "Đã xuất bản", "Từ chối"
            };

            if (!validStatuses.Contains(targetStatus))
            {
                return (false, $"Trạng thái '{dto.TrangThaiMoi}' không hợp lệ theo quy chế Tòa soạn.");
            }

            if (oldStatus == "Đã xuất bản")
            {
                return (false, "Bài đã công bố không thể đổi trạng thái bằng quyết định biên tập thông thường. Cần quy trình đính chính hoặc rút bài công khai.");
            }

            // Bảng chuyển đổi trạng thái hợp lệ (State Machine Transition Matrix)
            var allowedTransitions = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Chờ sơ duyệt"] = new(StringComparer.OrdinalIgnoreCase) { "Chờ sửa hình thức", "Đang phản biện", "Từ chối" },
                ["Chờ sửa hình thức"] = new(StringComparer.OrdinalIgnoreCase) { "Chờ sơ duyệt", "Từ chối" },
                ["Đang phản biện"] = new(StringComparer.OrdinalIgnoreCase) { "Chờ chỉnh sửa", "Đã chấp nhận", "Từ chối" },
                ["Chờ chỉnh sửa"] = new(StringComparer.OrdinalIgnoreCase) { "Chờ quyết định", "Từ chối" },
                ["Chờ quyết định"] = new(StringComparer.OrdinalIgnoreCase) { "Đang phản biện", "Đã chấp nhận", "Chờ chỉnh sửa", "Từ chối" },
                ["Đã chấp nhận"] = new(StringComparer.OrdinalIgnoreCase) { "Đang chế bản", "Sẵn sàng xuất bản", "Đã xuất bản", "Từ chối" },
                ["Đang chế bản"] = new(StringComparer.OrdinalIgnoreCase) { "Sẵn sàng xuất bản", "Đã xuất bản", "Từ chối" },
                ["Sẵn sàng xuất bản"] = new(StringComparer.OrdinalIgnoreCase) { "Đã xuất bản", "Từ chối" },
                ["Đã xuất bản"] = new(StringComparer.OrdinalIgnoreCase) { },
                ["Từ chối"] = new(StringComparer.OrdinalIgnoreCase) { }
            };

            if (oldStatus != targetStatus)
            {
                if (!allowedTransitions.TryGetValue(oldStatus, out var nextAllowed) || !nextAllowed.Contains(targetStatus))
                {
                    return (false, $"Quy chế Tòa soạn không cho phép chuyển trạng thái từ '{oldStatus}' sang '{targetStatus}'.");
                }
            }

            // Kiểm soát điều kiện chuyển trạng thái (Workflow Guard Rules):
            // 1. Chuyển sang 'Đang phản biện': Bắt buộc đã có tệp ẩn danh và tối thiểu 2 chuyên gia phản biện
            if (targetStatus == "Đang phản biện")
            {
                var currentRound = await _context.PhanCongPhanBiens
                    .Where(p => p.MaBaiBao == baiBao.MaBaiBao)
                    .Select(p => (int?)p.SoVong)
                    .MaxAsync() ?? 1;
                var hasAnonymous = await _context.ThuMucBaiBaos
                    .AnyAsync(f => f.MaBaiBao == baiBao.MaBaiBao &&
                                   (f.LoaiThuMuc == "File ẩn danh" || f.LoaiThuMuc == "Bản thảo ẩn danh") &&
                                   f.SoVong == currentRound);
                if (!hasAnonymous)
                {
                    return (false, "Bài báo chưa có tệp bản thảo ẩn danh. Ban biên tập cần tải lên tệp ẩn danh trước khi chuyển sang vòng phản biện.");
                }

                var assignments = await _context.PhanCongPhanBiens
                    .Where(p => p.MaBaiBao == baiBao.MaBaiBao && p.SoVong == currentRound &&
                        (p.TrangThai == "Đồng ý phản biện" || p.TrangThai == "Đang đánh giá" || p.TrangThai == "Đã đánh giá"))
                    .ToListAsync();
                if (assignments.Count < 2)
                {
                    return (false, $"Bài báo mới có {assignments.Count}/2 chuyên gia nhận lời phản biện. Cần đủ hai người cùng vòng trước khi bắt đầu.");
                }
            }

            // 2. Chuyển sang 'Chờ chỉnh sửa' hoặc 'Đã chấp nhận' từ vòng phản biện:
            // Bắt buộc có tối thiểu 2 chuyên gia phản biện và TOÀN BỘ chuyên gia được phân công đã nộp phiếu đánh giá (BM-04)
            if ((targetStatus == "Chờ chỉnh sửa" || targetStatus == "Đã chấp nhận") && oldStatus == "Đang phản biện")
            {
                var currentRound = await _context.PhanCongPhanBiens
                    .Where(p => p.MaBaiBao == baiBao.MaBaiBao)
                    .Select(p => (int?)p.SoVong)
                    .MaxAsync() ?? 1;

                var assignments = await _context.PhanCongPhanBiens
                    .Include(p => p.PhieuDanhGia)
                    .Where(p => p.MaBaiBao == baiBao.MaBaiBao && p.SoVong == currentRound && p.TrangThai != "Từ chối phản biện")
                    .ToListAsync();

                if (assignments.Count < 2)
                {
                    return (false, $"Bài báo mới chỉ phân công {assignments.Count}/2 chuyên gia ở Vòng {currentRound}. Quy chế Tòa soạn yêu cầu tối thiểu 2 chuyên gia phản biện độc lập.");
                }

                var pendingCount = assignments.Count(p => p.PhieuDanhGia == null);
                if (pendingCount > 0)
                {
                    var completedCount = assignments.Count - pendingCount;
                    return (false, $"Chưa hoàn tất phản biện ({completedCount}/{assignments.Count} chuyên gia đã gửi phiếu BM-04). Toàn bộ chuyên gia được phân công phải hoàn thành đánh giá trước khi ra quyết định.");
                }
            }

            // 3. Chuyển sang 'Sẵn sàng xuất bản' hoặc 'Đã xuất bản':
            // Bắt buộc bài báo đã xếp vào số báo, có đánh số trang, có PDF thành phẩm và số báo đã phát hành (đối với Đã xuất bản)
            if (targetStatus == "Sẵn sàng xuất bản" || targetStatus == "Đã xuất bản")
            {
                if (baiBao.MaSoTapChi == null)
                {
                    return (false, "Bài báo chưa được xếp vào số tạp chí nào. Vui lòng xếp bài vào số phát hành trước khi xuất bản.");
                }

                var soTapChi = await _context.SoTapChis.FindAsync(baiBao.MaSoTapChi);
                if (soTapChi == null)
                {
                    return (false, "Không tìm thấy thông tin số tạp chí đã xếp cho bài báo.");
                }

                if (targetStatus == "Đã xuất bản" && soTapChi.TrangThai != "Đã xuất bản" && soTapChi.TrangThai != "Đã phát hành")
                {
                    return (false, $"Số tạp chí '{soTapChi.TenSo}' đang ở trạng thái '{soTapChi.TrangThai}'. Không thể xuất bản bài báo công khai khi số tạp chí chưa được phát hành.");
                }

                var hasPublishedPdf = await _context.ThuMucBaiBaos
                    .AnyAsync(f => f.MaBaiBao == baiBao.MaBaiBao && (f.LoaiThuMuc == "PDF thành phẩm" || f.LoaiThuMuc == "PDF Xuất bản")
                                && f.TenThuMuc.EndsWith(".pdf"));
                if (!hasPublishedPdf)
                {
                    return (false, "Bài báo chưa có tệp 'PDF thành phẩm'. Ban biên tập cần tải lên tệp PDF xuất bản trước khi công bố.");
                }

                if (baiBao.TrangBatDau == null || baiBao.TrangKetThuc == null)
                {
                    return (false, "Bài báo chưa được đánh số trang phát hành (TrangBatDau, TrangKetThuc) trong số tạp chí.");
                }
            }

            baiBao.TrangThai = targetStatus;
            baiBao.NgayCapNhat = DateTime.Now;

            var editor = await _context.NguoiDungs.FindAsync(maEditor);

            _context.LichSuTrangThais.Add(new LichSuTrangThaiBaiBao
            {
                MaBaiBao = baiBao.MaBaiBao,
                TrangThaiCu = oldStatus,
                TrangThaiMoi = targetStatus,
                MaNguoiThucHien = maEditor,
                NgayChuyen = DateTime.Now,
                GhiChu = dto.GhiChu ?? $"Ban biên tập ({editor?.HoTen}) ra quyết định: {targetStatus}.",
                ThongBaoChoTacGia = dto.ThongBaoChoTacGia
            });

            await _context.SaveChangesAsync();

            return (true, $"Đã cập nhật trạng thái bài báo thành: '{targetStatus}'.");
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return (false, $"Lỗi khi ra quyết định biên tập: {innerMsg}");
        }
    }

    public async Task<(bool Success, string Message, string? PhysicalPath, string? FileName, string? ContentType)> GetManuscriptForReviewerAsync(int maPhanCong, int maReviewer, bool isEditorOrAdmin)
    {
        var phanCong = await _context.PhanCongPhanBiens
            .Include(p => p.BaiBao)
                .ThenInclude(b => b.ThuMucBaiBaos)
            .FirstOrDefaultAsync(p => p.MaPhanCong == maPhanCong);

        if (phanCong == null)
        {
            return (false, "Không tìm thấy thông tin phân công phản biện.", null, null, null);
        }

        if (!isEditorOrAdmin && phanCong.MaNguoiDung != maReviewer)
        {
            return (false, "Bạn không có quyền truy cập bản thảo của nhiệm vụ này.", null, null, null);
        }
        if (!isEditorOrAdmin && phanCong.TrangThai is not ("Đồng ý phản biện" or "Đang đánh giá" or "Đã đánh giá"))
        {
            return (false, "Chỉ được tải bản thảo sau khi đã nhận lời phản biện.", null, null, null);
        }

        // Tiêu chuẩn Phản biện kín hai chiều (Double-Blind Peer Review - COPE):
        // Chuyên gia phản biện CHỈ được phép tải tệp loại 'File ẩn danh' (hoặc 'Bản thảo ẩn danh') đúng theo vòng phân công (SoVong)
        // Tuyệt đối không fallback về vòng 1 nếu đang phản biện vòng sau, và không trả về Bản thảo gốc hay Bản giải trình BM-03.
        var fileRecord = phanCong.BaiBao.ThuMucBaiBaos
            .Where(f => (f.LoaiThuMuc == "File ẩn danh" || f.LoaiThuMuc == "Bản thảo ẩn danh")
                     && f.SoVong == phanCong.SoVong)
            .OrderByDescending(f => f.NgayTaiLen)
            .FirstOrDefault();

        if (fileRecord == null)
        {
            return (false, $"Bài báo chưa có tệp 'File ẩn danh' cho vòng {phanCong.SoVong}. Theo quy chuẩn bình duyệt kín hai chiều (Double-Blind), Ban biên tập cần loại bỏ thông tin tác giả và tải lên bản thảo ẩn danh Vòng {phanCong.SoVong} trước khi Chuyên gia thẩm định.", null, null, null);
        }

        var cleanPath = fileRecord.DuongDan.TrimStart('/', '\\');
        var basePath = _env.ContentRootPath;
        var candidate1 = Path.Combine(basePath, cleanPath.Replace('/', Path.DirectorySeparatorChar));
        var candidate2 = Path.Combine(basePath, "wwwroot", cleanPath.Replace('/', Path.DirectorySeparatorChar));
        var candidate3 = Path.Combine(basePath, "Uploads", Path.GetFileName(cleanPath));
        var physicalPath = File.Exists(candidate1) ? candidate1 : (File.Exists(candidate2) ? candidate2 : (File.Exists(candidate3) ? candidate3 : null));

        if (physicalPath == null)
        {
            return (false, $"Tệp bản thảo ẩn danh '{fileRecord.TenThuMuc}' không tồn tại trên hệ thống lưu trữ máy chủ.", null, null, null);
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
}
