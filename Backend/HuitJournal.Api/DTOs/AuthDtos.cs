using System.ComponentModel.DataAnnotations;

namespace HuitJournal.Api.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
    public string UsernameOrEmail { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    public string Password { get; set; } = null!;
}

public class RegisterRequest
{
    public string? TenDangNhap { get; set; }

    public string? HoDem { get; set; }

    public string? Ten { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    public string HoTen { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
    public string Password { get; set; } = null!;

    public string? DonVi { get; set; }
    public string? SoDienThoai { get; set; }
    public string? HocVi { get; set; } = "Không";
    public string? HocHam { get; set; } = "Không";
    public string? GioiTinh { get; set; } = "Nam";
    public string? QuocGia { get; set; } = "Vietnam";
    public string? NgonNgu { get; set; } = "Tiếng Việt";
    public string? DiaChi { get; set; }
    public string? SoTaiKhoan { get; set; }
    public string? ChuTaiKhoan { get; set; }
    public string? NganHang { get; set; }
    public string? MaORCID { get; set; }
    public int? ChuyenNganhId { get; set; }
    public bool DangKyPhanBien { get; set; } = false;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Token { get; set; }
    public UserProfileDto? User { get; set; }
    public int SoBaiDongTacGiaLienKet { get; set; }
    public int? RemainingAttempts { get; set; }
}

public class UserProfileDto
{
    public int MaNguoiDung { get; set; }
    public string? TenDangNhap { get; set; }
    public string? HoDem { get; set; }
    public string? Ten { get; set; }
    public string HoTen { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string HocVi { get; set; } = "Không";
    public string HocHam { get; set; } = "Không";
    public string GioiTinh { get; set; } = "Nam";
    public string NgonNgu { get; set; } = "Tiếng Việt";
    public string QuocGia { get; set; } = "Vietnam";
    public string? SoDienThoai { get; set; }
    public string? DonVi { get; set; }
    public string? DiaChi { get; set; }
    public string? SoTaiKhoan { get; set; }
    public string? ChuTaiKhoan { get; set; }
    public string? NganHang { get; set; }
    public string? MaORCID { get; set; }
    public string? AnhDaiDien { get; set; }
    public List<string> VaiTros { get; set; } = new();
    public List<int> ChuyenMonIds { get; set; } = new();
    public List<string> ChuyenMonNames { get; set; } = new();
}

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    public string HoTen { get; set; } = null!;

    public string? DonVi { get; set; }
    public string? DiaChi { get; set; }
    public string? SoDienThoai { get; set; }
    public string HocVi { get; set; } = "Không";
    public string HocHam { get; set; } = "Không";
    public string GioiTinh { get; set; } = "Nam";
    public string? SoTaiKhoan { get; set; }
    public string? ChuTaiKhoan { get; set; }
    public string? NganHang { get; set; }
    public string? MaORCID { get; set; }
    public string? AnhDaiDien { get; set; }
    public List<int>? ChuyenMonIds { get; set; }
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự")]
    public string NewPassword { get; set; } = null!;
}

public class DonDangKyPhanBienDto
{
    public int MaDon { get; set; }
    public int MaNguoiDung { get; set; }
    public string HoTen { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? DonVi { get; set; }
    public string HocVi { get; set; } = null!;
    public string HocHam { get; set; } = null!;
    public string? MaORCID { get; set; }
    public List<string> ChuyenMonNames { get; set; } = new();
    public DateTime NgayDangKy { get; set; }
    public string? GhiChu { get; set; }
    public string TrangThai { get; set; } = null!;
    public int? MaNguoiDuyet { get; set; }
    public string? NguoiDuyetHoTen { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }
}

public class RequestReviewerRoleDto
{
    public string? GhiChu { get; set; }
}

public class RejectReviewerRequestDto
{
    public string? LyDo { get; set; }
}

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Mã định danh hồ sơ đăng ký không được để trống")]
    public Guid RegistrationId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã xác nhận 6 chữ số")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã xác nhận phải gồm đúng 6 chữ số (từ 000000 đến 999999)")]
    public string VerificationCode { get; set; } = null!;
}

public class ResendVerificationRequest
{
    [Required(ErrorMessage = "Mã định danh hồ sơ đăng ký không được để trống")]
    public Guid RegistrationId { get; set; }
}

public class RegisterPendingResponse
{
    public bool Success { get; set; } = true;
    public bool RequiresVerification { get; set; } = true;
    public bool EmailSent { get; set; }
    public Guid RegistrationId { get; set; }
    public string MaskedEmail { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public int ResendAfterSeconds { get; set; }
    public string Message { get; set; } = "";
}

public class VerifyOtpResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public bool IsExpired { get; set; }
    public bool IsMaxAttemptsReached { get; set; }
    public int RemainingAttempts { get; set; }
}

public class ResendEligibilityResult
{
    public bool IsEligible { get; set; }
    public string Message { get; set; } = "";
    public int CooldownRemainingSeconds { get; set; }
}

public class OutboxStatusDto
{
    public int PendingCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalCount { get; set; }
    public DateTime? OldestPendingTaoLucUtc { get; set; }
    public DateTime? LastSentUtc { get; set; }
    public bool SimulateDeliveryInDev { get; set; }
    public bool EnableBackgroundDispatcher { get; set; }
    public int OutboxPollingIntervalSeconds { get; set; }
}


