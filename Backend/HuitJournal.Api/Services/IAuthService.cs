using Microsoft.AspNetCore.Http;
using HuitJournal.Api.DTOs;

namespace HuitJournal.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<RegisterPendingResponse> RegisterPendingAsync(RegisterRequest request);
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request);
    Task<RegisterPendingResponse> ResendVerificationAsync(ResendVerificationRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<UserProfileDto?> GetProfileAsync(int maNguoiDung);
    Task<UserProfileDto?> UpdateProfileAsync(int maNguoiDung, UpdateProfileRequest request);
    Task<bool> ChangePasswordAsync(int maNguoiDung, ChangePasswordRequest request);
    Task<UserProfileDto?> UploadAvatarAsync(int maNguoiDung, IFormFile file);
    Task<UserProfileDto?> DeleteAvatarAsync(int maNguoiDung);
    /// <summary>
    /// Nộp đơn đăng ký tham gia Hội đồng phản biện (Lưu trữ vào CSDL ở trạng thái Chờ duyệt)
    /// </summary>
    Task<(bool Success, string Message, UserProfileDto? Profile)> RequestReviewerRoleAsync(int maNguoiDung, string? ghiChu = null);

    /// <summary>
    /// Lấy danh sách các đơn đăng ký phản biện đang chờ Ban biên tập thẩm định
    /// </summary>
    Task<List<DonDangKyPhanBienDto>> GetPendingReviewerRegistrationsAsync();

    /// <summary>
    /// Ban biên tập / Quản trị viên thẩm định và phê duyệt đơn đăng ký, chính thức cấp vai trò Phản biện viên
    /// </summary>
    Task<(bool Success, string Message)> ApproveReviewerRoleAsync(int maDon, int maEditor);

    /// <summary>
    /// Ban biên tập / Quản trị viên từ chối đơn đăng ký phản biện
    /// </summary>
    Task<(bool Success, string Message)> RejectReviewerRoleAsync(int maDon, int maEditor, string? lyDo = null);
}
