using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HuitJournal.Api.Configuration;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;

namespace HuitJournal.Api.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly QLTapChiKhoaHocContext _context;
    private readonly EmailVerificationSettings _settings;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        QLTapChiKhoaHocContext context,
        IOptions<EmailVerificationSettings> settingsOptions,
        ILogger<EmailVerificationService> logger)
    {
        _context = context;
        _settings = settingsOptions.Value ?? new EmailVerificationSettings();
        _logger = logger;
    }

    /// <inheritdoc />
    public (string PlainCode, string HashedCode) GenerateOtp(Guid maId)
    {
        // 1. Sinh chuỗi 6 chữ số CSPRNG bảo đảm khoảng từ 000000 đến 999999
        int codeInt = RandomNumberGenerator.GetInt32(0, 1_000_000);
        string plainCode = codeInt.ToString("D6");

        // 2. Tính giá trị băm HMAC-SHA256 kết hợp MaId và Server Secret
        string hashedCode = ComputeHmacHash(maId, plainCode);

        return (plainCode, hashedCode);
    }

    /// <inheritdoc />
    public string ComputeHmacHash(Guid maId, string plainCode)
    {
        var secretKey = _settings.HmacSecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Thiếu khóa HMAC để xác nhận email.");

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        using var hmac = new HMACSHA256(keyBytes);

        // Chuẩn hóa dữ liệu băm: MaId dạng N (32 ký tự hex) + ký tự phân cách + mã 6 chữ số
        string payload = $"{maId:N}:{plainCode.Trim()}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <inheritdoc />
    public bool VerifyHash(Guid maId, string inputCode, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(inputCode) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        string computedHash = ComputeHmacHash(maId, inputCode);
        byte[] computedBytes = Encoding.UTF8.GetBytes(computedHash);
        byte[] storedBytes = Encoding.UTF8.GetBytes(storedHash.Trim().ToLowerInvariant());

        if (computedBytes.Length != storedBytes.Length)
        {
            return false;
        }

        // So sánh thời gian cố định ngăn ngừa Side-Channel / Timing Attack
        return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
    }

    /// <inheritdoc />
    public string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var parts = email.Trim().Split('@');
        if (parts.Length != 2)
        {
            return email;
        }

        string localPart = parts[0];
        string domainPart = parts[1];

        if (localPart.Length <= 1)
        {
            return $"{localPart}*@{domainPart}";
        }

        if (localPart.Length == 2)
        {
            return $"{localPart[0]}*@{domainPart}";
        }

        // Với tài khoản >= 3 ký tự: giữ ký tự đầu và ký tự cuối, ẩn giữa bằng ***
        return $"{localPart[0]}***{localPart[^1]}@{domainPart}";
    }

    /// <inheritdoc />
    public async Task<VerifyOtpResult> ValidateAndConsumeOtpAsync(Guid registrationId, string inputCode)
    {
        var pending = await _context.DangKyChoXacNhans
            .Include(p => p.MaXacNhanEmails)
            .FirstOrDefaultAsync(p => p.MaDangKy == registrationId);

        if (pending == null)
        {
            return new VerifyOtpResult
            {
                IsValid = false,
                Message = "Hồ sơ đăng ký không tồn tại hoặc đã bị hủy khỏi hệ thống."
            };
        }

        if (pending.TrangThai != "Pending")
        {
            return new VerifyOtpResult
            {
                IsValid = false,
                Message = "Hồ sơ đăng ký này đã được xác thực hoặc không còn trong trạng thái chờ."
            };
        }

        var nowUtc = DateTime.UtcNow;
        if (nowUtc > pending.HetHanHoSoUtc)
        {
            pending.TrangThai = "Expired";
            await _context.SaveChangesAsync();

            return new VerifyOtpResult
            {
                IsValid = false,
                IsExpired = true,
                Message = "Hồ sơ đăng ký tạm đã quá hạn (sau 24 giờ). Vui lòng thực hiện đăng ký lại từ đầu."
            };
        }

        var activeCode = pending.MaXacNhanEmails
            .OrderByDescending(c => c.TaoLucUtc)
            .FirstOrDefault(c => c.DaDungLucUtc == null);

        if (activeCode == null)
        {
            return new VerifyOtpResult
            {
                IsValid = false,
                Message = "Không tìm thấy mã xác nhận còn hiệu lực cho hồ sơ này. Vui lòng nhấn \"Gửi lại mã\"."
            };
        }

        if (activeCode.SoLanNhapSai >= _settings.MaxFailedAttempts)
        {
            return new VerifyOtpResult
            {
                IsValid = false,
                IsMaxAttemptsReached = true,
                RemainingAttempts = 0,
                Message = $"Mã xác nhận đã bị vô hiệu hóa do nhập sai quá {_settings.MaxFailedAttempts} lần. Vui lòng bấm \"Gửi lại mã\" để nhận mã mới."
            };
        }

        if (nowUtc > activeCode.HetHanUtc)
        {
            return new VerifyOtpResult
            {
                IsValid = false,
                IsExpired = true,
                Message = "Mã xác nhận 6 chữ số đã hết hạn hiệu lực (sau 10 phút). Vui lòng bấm \"Gửi lại mã\" để nhận mã mới."
            };
        }

        if (string.IsNullOrWhiteSpace(inputCode) || !Regex.IsMatch(inputCode.Trim(), @"^\d{6}$"))
        {
            return new VerifyOtpResult
            {
                IsValid = false,
                Message = "Mã xác nhận không hợp lệ. Vui lòng nhập đúng 6 chữ số."
            };
        }

        bool isMatch = VerifyHash(activeCode.MaId, inputCode.Trim(), activeCode.MaHash);
        if (!isMatch)
        {
            activeCode.SoLanNhapSai += 1;
            await _context.SaveChangesAsync();

            int remaining = Math.Max(0, _settings.MaxFailedAttempts - activeCode.SoLanNhapSai);
            if (remaining == 0)
            {
                return new VerifyOtpResult
                {
                    IsValid = false,
                    IsMaxAttemptsReached = true,
                    RemainingAttempts = 0,
                    Message = $"Bạn đã nhập sai mã xác nhận {_settings.MaxFailedAttempts} lần. Mã xác nhận này đã bị khóa. Vui lòng bấm \"Gửi lại mã\" để nhận mã mới."
                };
            }

            return new VerifyOtpResult
            {
                IsValid = false,
                RemainingAttempts = remaining,
                Message = $"Mã xác nhận không chính xác. Bạn còn {remaining} lần thử trước khi mã bị khóa."
            };
        }

        return new VerifyOtpResult
        {
            IsValid = true,
            RemainingAttempts = Math.Max(0, _settings.MaxFailedAttempts - activeCode.SoLanNhapSai),
            Message = "Mã xác nhận hợp lệ."
        };
    }

    /// <inheritdoc />
    public async Task<ResendEligibilityResult> CheckResendEligibilityAsync(Guid registrationId)
    {
        var pending = await _context.DangKyChoXacNhans
            .Include(p => p.MaXacNhanEmails)
            .FirstOrDefaultAsync(p => p.MaDangKy == registrationId);

        if (pending == null)
        {
            return new ResendEligibilityResult
            {
                IsEligible = false,
                Message = "Hồ sơ đăng ký không tồn tại hoặc đã bị hủy."
            };
        }

        if (pending.TrangThai != "Pending")
        {
            return new ResendEligibilityResult
            {
                IsEligible = false,
                Message = "Hồ sơ đăng ký này đã hoàn tất xác nhận hoặc không còn khả dụng."
            };
        }

        var nowUtc = DateTime.UtcNow;
        if (nowUtc > pending.HetHanHoSoUtc)
        {
            return new ResendEligibilityResult
            {
                IsEligible = false,
                Message = "Hồ sơ đăng ký đã hết hạn 24 giờ. Vui lòng đăng ký tài khoản mới."
            };
        }

        // 1. Kiểm tra Cooldown giữa 2 lần gửi
        var lastCode = pending.MaXacNhanEmails
            .OrderByDescending(c => c.LanGuiCuoiUtc)
            .FirstOrDefault();

        if (lastCode != null)
        {
            var secondsSinceLastSend = (nowUtc - lastCode.LanGuiCuoiUtc).TotalSeconds;
            if (secondsSinceLastSend < _settings.ResendCooldownSeconds)
            {
                int remainingSeconds = (int)Math.Ceiling(_settings.ResendCooldownSeconds - secondsSinceLastSend);
                return new ResendEligibilityResult
                {
                    IsEligible = false,
                    CooldownRemainingSeconds = remainingSeconds,
                    Message = $"Vui lòng đợi {remainingSeconds} giây trước khi yêu cầu gửi lại mã xác nhận."
                };
            }
        }

        // 2. Kiểm tra Quota giới hạn trong 1 giờ
        var oneHourAgo = nowUtc.AddHours(-1);
        int codesInLastHour = pending.MaXacNhanEmails.Count(c => c.TaoLucUtc >= oneHourAgo);
        if (codesInLastHour >= _settings.MaxResendsPerHour)
        {
            return new ResendEligibilityResult
            {
                IsEligible = false,
                Message = $"Bạn đã yêu cầu gửi mã quá {_settings.MaxResendsPerHour} lần trong vòng 1 giờ. Vui lòng thử lại sau."
            };
        }

        // 3. Kiểm tra Quota giới hạn trong 24 giờ
        var oneDayAgo = nowUtc.AddDays(-1);
        int codesInLastDay = pending.MaXacNhanEmails.Count(c => c.TaoLucUtc >= oneDayAgo);
        if (codesInLastDay >= _settings.MaxResendsPerDay)
        {
            return new ResendEligibilityResult
            {
                IsEligible = false,
                Message = $"Bạn đã đạt giới hạn gửi mã tối đa ({_settings.MaxResendsPerDay} lần/ngày). Vui lòng thử lại vào ngày mai."
            };
        }

        return new ResendEligibilityResult
        {
            IsEligible = true,
            CooldownRemainingSeconds = 0,
            Message = "Đủ điều kiện gửi lại mã xác nhận."
        };
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message, string? PlainCode, DateTime? ExpiresAtUtc, int ResendAfterSeconds)> CreateNewVerificationCodeAsync(Guid registrationId)
    {
        var checkResult = await CheckResendEligibilityAsync(registrationId);
        if (!checkResult.IsEligible)
        {
            return (false, checkResult.Message, null, null, checkResult.CooldownRemainingSeconds);
        }

        var nowUtc = DateTime.UtcNow;

        // Vô hiệu hóa tất cả các mã cũ chưa dùng của hồ sơ này
        var pendingCodes = await _context.MaXacNhanEmails
            .Where(c => c.MaDangKy == registrationId && c.DaDungLucUtc == null && c.HetHanUtc > nowUtc)
            .ToListAsync();

        foreach (var oldCode in pendingCodes)
        {
            oldCode.HetHanUtc = nowUtc; // Cho hết hạn ngay lập tức
        }

        var newMaId = Guid.NewGuid();
        var (plainCode, hashCode) = GenerateOtp(newMaId);
        var expiresAtUtc = nowUtc.AddMinutes(_settings.OtpExpiryMinutes);

        var newCodeRecord = new MaXacNhanEmail
        {
            MaId = newMaId,
            MaDangKy = registrationId,
            MaHash = hashCode,
            TaoLucUtc = nowUtc,
            HetHanUtc = expiresAtUtc,
            SoLanNhapSai = 0,
            LanGuiCuoiUtc = nowUtc
        };

        _context.MaXacNhanEmails.Add(newCodeRecord);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Đã tạo mã xác nhận mới cho hồ sơ {RegistrationId}. Hết hạn lúc: {ExpiresAtUtc}",
            registrationId, expiresAtUtc);

        return (true, "Tạo mã xác nhận mới thành công.", plainCode, expiresAtUtc, _settings.ResendCooldownSeconds);
    }
}
