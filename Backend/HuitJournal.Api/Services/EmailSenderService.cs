using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HuitJournal.Api.Configuration;
using HuitJournal.Api.Data;
using HuitJournal.Api.DTOs;
using HuitJournal.Api.Models;

namespace HuitJournal.Api.Services;

public class EmailSenderService : IEmailSenderService
{
    private readonly QLTapChiKhoaHocContext _context;
    private readonly EmailVerificationSettings _settings;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(
        QLTapChiKhoaHocContext context,
        IOptions<EmailVerificationSettings> settingsOptions,
        ILogger<EmailSenderService> logger)
    {
        _context = context;
        _settings = settingsOptions.Value ?? new EmailVerificationSettings();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SendVerificationEmailAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode,
        int expiryMinutes = 10,
        Guid? registrationId = null)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return (false, "Địa chỉ email người nhận không hợp lệ.");
        }

        var cleanName = string.IsNullOrWhiteSpace(recipientName) ? "Quý tác giả / Quý bạn đọc" : recipientName.Trim();
        var subject = "[HUIT Journal] Xác nhận đăng ký tài khoản Tòa soạn Điện tử";

        var textBody = BuildPlainTextBody(cleanName, verificationCode, expiryMinutes, registrationId);
        var htmlBody = BuildHtmlBody(cleanName, verificationCode, expiryMinutes, registrationId);

        // 1. Lưu bản ghi Email vào Outbox để đảm bảo tính bền vững (Reliable Outbox Pattern)
        var outbox = new EmailOutbox
        {
            MaOutbox = Guid.NewGuid(),
            LoaiThu = "XacNhanDangKy",
            NguoiNhan = recipientEmail.Trim().ToLowerInvariant(),
            TieuDe = subject,
            NoiDungHtml = htmlBody,
            NoiDungText = textBody,
            TrangThai = "Processing",
            TaoLucUtc = DateTime.UtcNow
        };

        _context.EmailOutboxes.Add(outbox);
        await _context.SaveChangesAsync();

        // 2. Chế độ giả lập gửi thư trong môi trường Dev / Testing (Tránh gián đoạn kiểm thử khi không có kết nối SMTP ngoài)
        bool hasSmtpCredentials = !string.IsNullOrWhiteSpace(_settings.SmtpHost) &&
                                  !string.IsNullOrWhiteSpace(_settings.SmtpUsername) &&
                                  !string.IsNullOrWhiteSpace(_settings.SmtpPassword);

        if (_settings.SimulateDeliveryInDev)
        {
            _logger.LogInformation(
                "[EMAIL OUTBOX] Giả lập gửi email thành công cho: {Recipient} | Loại: {Type} | OutboxId: {OutboxId}",
                recipientEmail, outbox.LoaiThu, outbox.MaOutbox);

            outbox.TrangThai = "Sent";
            outbox.GuiLucUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Mã xác nhận bảo mật đã được gửi đến hộp thư của bạn.");
        }

        if (!hasSmtpCredentials)
        {
            outbox.TrangThai = "Failed";
            outbox.LoiKyThuat = "Thiếu cấu hình SMTP.";
            await _context.SaveChangesAsync();
            _logger.LogError("Không thể gửi thư xác nhận: thiếu cấu hình SMTP.");
            return (false, "Máy chủ chưa thể gửi mã xác nhận. Vui lòng thử lại sau.");
        }

        // 3. Gửi thư thực tế qua máy chủ SMTP
        try
        {
            await DispatchEmailViaSmtpAsync(recipientEmail, cleanName, subject, htmlBody, textBody);

            outbox.TrangThai = "Sent";
            outbox.GuiLucUtc = DateTime.UtcNow;
            outbox.NoiDungHtml = null;
            outbox.NoiDungText = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Đã gửi thư xác nhận email thực tế đến {Recipient} thành công.", recipientEmail);
            return (true, "Mã xác nhận bảo mật đã được gửi đến hộp thư của bạn.");
        }
        catch (Exception ex)
        {
            outbox.TrangThai = "Failed";
            outbox.SoLanThuLai += 1;
            outbox.LoiKyThuat = ex.Message.Length > 950 ? ex.Message[..950] : ex.Message;
            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Lỗi khi gửi email xác thực đến {Recipient}: {Error}", recipientEmail, ex.Message);
            return (false, "Không thể gửi thư xác nhận do sự cố kết nối máy chủ gửi thư. Vui lòng bấm \"Gửi lại mã\" sau ít phút.");
        }
    }

    /// <inheritdoc />
    public async Task<int> ProcessPendingOutboxAsync(int batchSize = 10)
    {
        var maxAttempts = Math.Max(1, _settings.MaxDeliveryAttempts);
        var pendingItems = await _context.EmailOutboxes
            .Where(o => o.TrangThai == "Pending" ||
                (o.TrangThai == "Failed" && o.SoLanThuLai < maxAttempts) ||
                (o.TrangThai == "Processing" && o.TaoLucUtc < DateTime.UtcNow.AddMinutes(-2)))
            .OrderBy(o => o.TaoLucUtc)
            .Take(batchSize)
            .ToListAsync();

        if (pendingItems.Count == 0)
        {
            return 0;
        }

        int sentCount = 0;
        bool hasSmtpCredentials = !string.IsNullOrWhiteSpace(_settings.SmtpHost) &&
                                  !string.IsNullOrWhiteSpace(_settings.SmtpUsername) &&
                                  !string.IsNullOrWhiteSpace(_settings.SmtpPassword);

        foreach (var item in pendingItems)
        {
            if ((item.LoaiThu == "XacNhanDangKy" || item.LoaiThu == "KhoiPhucMatKhau") && DateTime.UtcNow - item.TaoLucUtc > TimeSpan.FromMinutes(Math.Max(1, _settings.OtpExpiryMinutes)))
            {
                item.TrangThai = "Failed";
                item.SoLanThuLai = maxAttempts;
                item.NoiDungHtml = null;
                item.NoiDungText = null;
                item.LoiKyThuat = "Mã xác nhận đã hết hạn trước khi thư được gửi.";
                continue;
            }
            // Nếu thư đã từng thử lại, chỉ gửi khi đã qua thời gian lùi số mũ (Exponential Backoff): 2^SoLanThuLai * 30 giây
            if (item.SoLanThuLai > 0)
            {
                var lastAttempt = item.GuiLucUtc ?? item.TaoLucUtc;
                var backoffSeconds = Math.Min(1800, Math.Pow(2, item.SoLanThuLai) * 30);
                if ((DateTime.UtcNow - lastAttempt).TotalSeconds < backoffSeconds)
                {
                    continue;
                }
            }

            if (_settings.SimulateDeliveryInDev)
            {
                item.TrangThai = "Sent";
                item.GuiLucUtc = DateTime.UtcNow;
                sentCount++;
                continue;
            }

            if (!hasSmtpCredentials)
            {
                _logger.LogError("Không thể gửi hàng đợi email: thiếu cấu hình SMTP.");
                break;
            }

            try
            {
                await DispatchEmailViaSmtpAsync(item.NguoiNhan, item.NguoiNhan, item.TieuDe, item.NoiDungHtml ?? "", item.NoiDungText ?? "");
                item.TrangThai = "Sent";
                item.GuiLucUtc = DateTime.UtcNow;
                item.NoiDungHtml = null;
                item.NoiDungText = null;
                sentCount++;
            }
            catch (Exception ex)
            {
                item.SoLanThuLai += 1;
                item.TrangThai = item.SoLanThuLai >= maxAttempts ? "Failed" : "Pending";
                item.LoiKyThuat = ex.Message.Length > 950 ? ex.Message[..950] : ex.Message;
                _logger.LogWarning("Thử lại gửi outbox {OutboxId} thất bại: {Error}", item.MaOutbox, ex.Message);
            }
        }

        await _context.SaveChangesAsync();
        return sentCount;
    }

    /// <inheritdoc />
    public async Task<(int ExpiredRegistrations, int CleanedOutbox)> CleanupExpiredRecordsAsync(CancellationToken cancellationToken = default)
    {
        int expiredRegCount = 0;
        int cleanedOutboxCount = 0;

        var now = DateTime.UtcNow;

        // 1. Chuyển trạng thái các hồ sơ đăng ký chờ đã quá hạn thành Expired
        var expiredRegistrations = await _context.DangKyChoXacNhans
            .Where(r => r.TrangThai == "Pending" && r.HetHanHoSoUtc < now)
            .ToListAsync(cancellationToken);

        if (expiredRegistrations.Count > 0)
        {
            foreach (var reg in expiredRegistrations)
            {
                reg.TrangThai = "Expired";
                expiredRegCount++;
            }
        }

        // 2. Dọn dẹp các thư Outbox đã gửi thành công vượt quá thời gian lưu trữ (SentRetentionDays)
        var retentionDays = Math.Max(1, _settings.SentRetentionDays);
        var retentionCutoff = now.AddDays(-retentionDays);

        var oldSentOutbox = await _context.EmailOutboxes
            .Where(o => o.TrangThai == "Sent" && o.GuiLucUtc != null && o.GuiLucUtc < retentionCutoff)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (oldSentOutbox.Count > 0)
        {
            _context.EmailOutboxes.RemoveRange(oldSentOutbox);
            cleanedOutboxCount = oldSentOutbox.Count;
        }

        // 3. Khử nhạy cảm: Xóa mã OTP khỏi các thư Sent đã gửi quá 1 giờ
        var oneHourAgo = now.AddHours(-1);
        var sentNeedsMasking = await _context.EmailOutboxes
            .Where(o => (o.LoaiThu == "XacNhanDangKy" || o.LoaiThu == "KhoiPhucMatKhau") && o.TrangThai == "Sent" && o.GuiLucUtc != null && o.GuiLucUtc < oneHourAgo && o.NoiDungText != null)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var outbox in sentNeedsMasking)
        {
            outbox.NoiDungText = "[Nội dung thư chứa mã OTP đã được tự động thanh lọc để bảo vệ quyền riêng tư]";
            outbox.NoiDungHtml = "<p>Nội dung thư chứa mã OTP đã được tự động thanh lọc để bảo vệ quyền riêng tư theo tiêu chuẩn an toàn học thuật HUIT.</p>";
        }

        if (expiredRegCount > 0 || cleanedOutboxCount > 0 || sentNeedsMasking.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return (expiredRegCount, cleanedOutboxCount);
    }

    /// <inheritdoc />
    public async Task<OutboxStatusDto> GetOutboxStatusAsync(CancellationToken cancellationToken = default)
    {
        var pendingCount = await _context.EmailOutboxes.CountAsync(o => o.TrangThai == "Pending", cancellationToken);
        var sentCount = await _context.EmailOutboxes.CountAsync(o => o.TrangThai == "Sent", cancellationToken);
        var failedCount = await _context.EmailOutboxes.CountAsync(o => o.TrangThai == "Failed", cancellationToken);
        var totalCount = pendingCount + sentCount + failedCount;

        var oldestPending = await _context.EmailOutboxes
            .Where(o => o.TrangThai == "Pending")
            .OrderBy(o => o.TaoLucUtc)
            .Select(o => (DateTime?)o.TaoLucUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var lastSent = await _context.EmailOutboxes
            .Where(o => o.TrangThai == "Sent" && o.GuiLucUtc != null)
            .OrderByDescending(o => o.GuiLucUtc)
            .Select(o => o.GuiLucUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new OutboxStatusDto
        {
            PendingCount = pendingCount,
            SentCount = sentCount,
            FailedCount = failedCount,
            TotalCount = totalCount,
            OldestPendingTaoLucUtc = oldestPending,
            LastSentUtc = lastSent,
            SimulateDeliveryInDev = _settings.SimulateDeliveryInDev,
            EnableBackgroundDispatcher = _settings.EnableBackgroundDispatcher,
            OutboxPollingIntervalSeconds = _settings.OutboxPollingIntervalSeconds
        };
    }

    private async Task DispatchEmailViaSmtpAsync(string recipientEmail, string recipientName, string subject, string htmlBody, string textBody)
    {
        var cleanPassword = (_settings.SmtpPassword ?? "").Replace(" ", "").Trim();
        var cleanUsername = (_settings.SmtpUsername ?? "").Trim();

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(cleanUsername, cleanPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderDisplayName, Encoding.UTF8),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = htmlBody
        };

        message.To.Add(new MailAddress(recipientEmail, recipientName, Encoding.UTF8));

        // Bổ sung luồng văn bản thuần (Plaintext) và HTML song song (MIME multipart/alternative)
        if (!string.IsNullOrWhiteSpace(textBody))
        {
            var textView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain");
            message.AlternateViews.Add(textView);
        }

        if (!string.IsNullOrWhiteSpace(htmlBody))
        {
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");
            message.AlternateViews.Add(htmlView);
        }

        await client.SendMailAsync(message);
    }

    private string BuildPlainTextBody(string recipientName, string verificationCode, int expiryMinutes, Guid? registrationId = null)
    {
        var activationSection = registrationId.HasValue
            ? $@"Mở trang sau để nhập mã và hoàn tất đăng ký:
{_settings.PublicWebBaseUrl.TrimEnd('/')}/register.html?regId={registrationId.Value:D}
"
            : "";

        return $@"Kính gửi {recipientName},

Tòa soạn Tạp chí Khoa học Đại học Công Thương TP.HCM (HUIT Journal of Science)
đã nhận được yêu cầu đăng ký tài khoản từ địa chỉ email này.

MÃ XÁC NHẬN BẢO MẬT (Nhấp đúp chuột vào dãy số để sao chép):

{verificationCode}

(Mã xác nhận gồm 6 chữ số, có hiệu lực trong vòng {expiryMinutes} phút và chỉ sử dụng một lần).

{activationSection}
Nếu Quý vị không thực hiện yêu cầu này, vui lòng bỏ qua thư.
Lưu ý an toàn: Tòa soạn không bao giờ yêu cầu Quý vị cung cấp mật khẩu hoặc mã xác nhận qua email hay điện thoại.

Trân trọng,
Hội đồng Biên tập Tạp chí Khoa học Đại học Công Thương TP.HCM (HUIT)
Trường Đại học Công Thương TP. Hồ Chí Minh
Địa chỉ: 140 Lê Trọng Tấn, P. Tây Thạnh, Q. Tân Phú, TP. Hồ Chí Minh";
    }

    private string BuildHtmlBody(string recipientName, string verificationCode, int expiryMinutes, Guid? registrationId = null)
    {
        var activationButtonHtml = registrationId.HasValue
            ? $@"<div style=""margin-top: 20px; padding-top: 18px; border-top: 1px dashed #bae6fd;"">
                  <a href=""{_settings.PublicWebBaseUrl.TrimEnd('/')}/register.html?regId={registrationId.Value:D}"" target=""_blank"" rel=""noopener noreferrer"" style=""display: inline-block; background: linear-gradient(135deg, #1da1f2 0%, #0f82c7 100%); color: #ffffff; text-decoration: none; font-size: 15px; font-weight: 700; padding: 12px 28px; border-radius: 6px; box-shadow: 0 4px 12px rgba(15, 130, 199, 0.25);"">
                    Mở trang nhập mã xác nhận
                  </a>
                  <div style=""font-size: 12px; color: #64748b; margin-top: 8px;"">
                    (Nhập mã 6 chữ số trong thư để hoàn tất đăng ký)
                  </div>
                </div>"
            : "";

        // Tuân thủ triệt để tiêu chuẩn thiết kế Hallmark (không dùng bullet point, bảng màu #1da1f2 / #0f82c7, chữ đứng rõ ràng)
        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>Xác nhận đăng ký Tạp chí Khoa học HUIT</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f4f6f9; font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1e293b; line-height: 1.6;"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f4f6f9; padding: 32px 16px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""max-width: 600px; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;"">
          <!-- Header Banner -->
          <tr>
            <td style=""background: linear-gradient(135deg, #1da1f2 0%, #0f82c7 100%); padding: 28px 32px; text-align: left;"">
              <div style=""font-size: 13px; text-transform: uppercase; letter-spacing: 1px; color: #e0f2fe; font-weight: 600; margin-bottom: 4px;"">
                Trường Đại học Công Thương TP. Hồ Chí Minh
              </div>
              <div style=""font-size: 20px; font-weight: 700; color: #ffffff; line-height: 1.3;"">
                Tạp chí Khoa học (HUIT Journal of Science)
              </div>
            </td>
          </tr>

          <!-- Main Content -->
          <tr>
            <td style=""padding: 36px 32px 24px 32px;"">
              <h1 style=""font-size: 18px; font-weight: 600; color: #0f172a; margin: 0 0 16px 0; font-style: normal;"">
                Kính gửi {WebUtility.HtmlEncode(recipientName)},
              </h1>
              
              <p style=""margin: 0 0 20px 0; font-size: 15px; color: #334155; line-height: 1.6;"">
                Tòa soạn Tạp chí Khoa học Đại học Công Thương TP.HCM đã nhận được yêu cầu đăng ký tài khoản tác giả / bạn đọc của Quý vị. Để xác thực quyền sở hữu hộp thư và kích hoạt tài khoản chính thức, vui lòng sử dụng mã xác nhận bên dưới:
              </p>

              <!-- OTP Code Display Card -->
              <div style=""margin: 28px 0; padding: 24px; background-color: #f0f7ff; border: 1px solid #bae6fd; border-radius: 8px; text-align: center;"">
                <div style=""font-size: 13px; font-weight: 600; color: #0369a1; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 12px;"">
                  Mã xác nhận bảo mật của Quý vị
                </div>
                <div style=""display: inline-block; background-color: #ffffff; border: 2px dashed #0284c7; border-radius: 8px; padding: 10px 24px; margin: 6px 0 14px 0;"">
                  <span style=""font-family: 'Consolas', 'Courier New', Courier, monospace; font-size: 38px; font-weight: 700; color: #0f82c7; letter-spacing: 8px; user-select: all; -webkit-user-select: all; -moz-user-select: all; -ms-user-select: all;"" title=""Nhấp đúp chuột để sao chép toàn bộ mã"">{verificationCode}</span>
                </div>
                <div style=""font-size: 13px; color: #0284c7; font-weight: 500; margin-bottom: 8px;"">
                  ✦ Mẹo: Nhấp đúp chuột vào dãy số trên để sao chép ngay lập tức
                </div>
                <div style=""font-size: 13px; color: #64748b;"">
                  Mã có hiệu lực trong vòng {expiryMinutes} phút và chỉ có giá trị sử dụng một lần.
                </div>
                {activationButtonHtml}
              </div>

              <!-- Notice (Continuous paragraph, strictly NO bullet points) -->
              <p style=""margin: 0 0 16px 0; font-size: 14px; color: #475569; line-height: 1.6;"">
                Nếu Quý vị không thực hiện yêu cầu đăng ký tài khoản này, vui lòng bỏ qua thư này hoặc thông báo cho Tòa soạn. Tài khoản chưa được xác nhận sẽ tự động hết hạn sau 24 giờ mà không lưu lại thông tin.
              </p>

              <div style=""margin-top: 24px; padding: 14px 18px; background-color: #f8fafc; border-left: 4px solid #1da1f2; border-radius: 4px; font-size: 13px; color: #475569; line-height: 1.5;"">
                <strong>Lưu ý bảo mật:</strong> Tòa soạn Tạp chí Khoa học HUIT tuyệt đối không bao giờ yêu cầu Quý vị cung cấp mật khẩu, mã OTP hoặc thông tin ngân hàng qua email hay điện thoại.
              </div>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background-color: #f8fafc; padding: 24px 32px; border-top: 1px solid #e2e8f0; font-size: 12px; color: #64748b; line-height: 1.6;"">
              <div style=""font-weight: 600; color: #334155; margin-bottom: 4px;"">
                Hội đồng Biên tập Tạp chí Khoa học Đại học Công Thương TP.HCM (HUIT)
              </div>
              <div>Trường Đại học Công Thương TP. Hồ Chí Minh - 140 Lê Trọng Tấn, P. Tây Thạnh, Q. Tân Phú, TP.HCM</div>
              <div>Email: journal@huit.edu.vn | Trang tin: https://tapchikhoahoc.huit.edu.vn</div>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
