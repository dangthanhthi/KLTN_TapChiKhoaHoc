using HuitJournal.Api.DTOs;

namespace HuitJournal.Api.Services;

public interface IEmailSenderService
{
    /// <summary>
    /// Tạo và gửi thư điện tử chứa mã xác nhận đăng ký (ghi nhận EmailOutbox và gửi qua SMTP / dev simulation)
    /// </summary>
    Task<(bool Success, string Message)> SendVerificationEmailAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode,
        int expiryMinutes = 10,
        Guid? registrationId = null);

    /// <summary>
    /// Quét và xử lý các thư điện tử đang chờ trong EmailOutbox
    /// </summary>
    Task<int> ProcessPendingOutboxAsync(int batchSize = 10);

    /// <summary>
    /// Dọn dẹp các bản ghi hồ sơ chờ quá hạn và thanh lọc thư outbox cũ
    /// </summary>
    Task<(int ExpiredRegistrations, int CleanedOutbox)> CleanupExpiredRecordsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Thống kê trạng thái hàng đợi Outbox phục vụ giám sát vận hành
    /// </summary>
    Task<OutboxStatusDto> GetOutboxStatusAsync(CancellationToken cancellationToken = default);
}
