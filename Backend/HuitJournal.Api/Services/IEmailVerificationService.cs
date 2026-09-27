using HuitJournal.Api.DTOs;

namespace HuitJournal.Api.Services;

public interface IEmailVerificationService
{
    /// <summary>
    /// Sinh mã OTP ngẫu nhiên 6 chữ số CSPRNG (000000 - 999999) và tính chuỗi băm HMAC-SHA256
    /// </summary>
    (string PlainCode, string HashedCode) GenerateOtp(Guid maId);

    /// <summary>
    /// Tính giá trị băm HMAC-SHA256 của mã 6 chữ số gắn với MaId và khóa bí mật máy chủ
    /// </summary>
    string ComputeHmacHash(Guid maId, string plainCode);

    /// <summary>
    /// So sánh hash thời gian cố định (Constant-time comparison) chống tấn công Timing Attack
    /// </summary>
    bool VerifyHash(Guid maId, string inputCode, string storedHash);

    /// <summary>
    /// Che giấu địa chỉ email để hiển thị công khai trên giao diện (ví dụ: d***i@gmail.com)
    /// </summary>
    string MaskEmail(string email);

    /// <summary>
    /// Kiểm tra tính hợp lệ của mã OTP người dùng nhập vào, tăng bộ đếm nhập sai nếu không khớp, hoặc trả về thành công
    /// </summary>
    Task<VerifyOtpResult> ValidateAndConsumeOtpAsync(Guid registrationId, string inputCode);

    /// <summary>
    /// Kiểm tra tính hợp lệ của yêu cầu gửi lại mã (cooldown 60 giây, quota 3 lần/giờ, 10 lần/ngày)
    /// </summary>
    Task<ResendEligibilityResult> CheckResendEligibilityAsync(Guid registrationId);

    /// <summary>
    /// Tạo mã OTP mới cho hồ sơ chờ, vô hiệu hóa mã cũ và lưu trữ vào CSDL
    /// </summary>
    Task<(bool Success, string Message, string? PlainCode, DateTime? ExpiresAtUtc, int ResendAfterSeconds)> CreateNewVerificationCodeAsync(Guid registrationId);
}
