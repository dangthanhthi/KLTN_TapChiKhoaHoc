using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HuitJournal.Api.Configuration;

namespace HuitJournal.Api.Services;

/// <summary>
/// Dịch vụ nền tự động quét và phân phối thư điện tử trong EmailOutbox (Background Outbox Dispatcher)
/// Đảm bảo tính nhất quán (Reliable Outbox Pattern), tự động thử lại có giãn cách số mũ và dọn dẹp hồ sơ quá hạn định kỳ.
/// </summary>
public class EmailOutboxDispatcherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<EmailVerificationSettings> _settingsMonitor;
    private readonly ILogger<EmailOutboxDispatcherService> _logger;

    public EmailOutboxDispatcherService(
        IServiceProvider serviceProvider,
        IOptionsMonitor<EmailVerificationSettings> settingsMonitor,
        ILogger<EmailOutboxDispatcherService> logger)
    {
        _serviceProvider = serviceProvider;
        _settingsMonitor = settingsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(">>> [BACKGROUND WORKER] EmailOutboxDispatcherService đã khởi động thành công.");

        DateTime lastCleanupTime = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = _settingsMonitor.CurrentValue;

            if (settings.EnableBackgroundDispatcher)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();

                        // 1. Quét và gửi các email trong hàng đợi Outbox
                        int batchSize = Math.Max(1, settings.MaxBatchSize);
                        int processedCount = await emailSender.ProcessPendingOutboxAsync(batchSize);
                        if (processedCount > 0)
                        {
                            _logger.LogInformation("[OUTBOX DISPATCHER] Đã xử lý gửi thành công {Count} thư từ hàng đợi Outbox.", processedCount);
                        }

                        // 2. Định kỳ dọn dẹp các hồ sơ đăng ký chờ quá hạn và thanh lọc thư đã gửi
                        var cleanupIntervalHours = Math.Max(1, settings.CleanupIntervalHours);
                        if ((DateTime.UtcNow - lastCleanupTime).TotalHours >= cleanupIntervalHours)
                        {
                            var cleanupResult = await emailSender.CleanupExpiredRecordsAsync(stoppingToken);
                            if (cleanupResult.ExpiredRegistrations > 0 || cleanupResult.CleanedOutbox > 0)
                            {
                                _logger.LogInformation(
                                    "[OUTBOX CLEANUP] Hoàn tất dọn dẹp định kỳ: {Expired} hồ sơ hết hạn -> Expired, {Cleaned} thư outbox cũ được thanh lọc.",
                                    cleanupResult.ExpiredRegistrations, cleanupResult.CleanedOutbox);
                            }
                            lastCleanupTime = DateTime.UtcNow;
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Dừng bình thường khi máy chủ shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[OUTBOX DISPATCHER ERROR] Lỗi không mong muốn trong chu kỳ quét EmailOutbox: {Message}", ex.Message);
                }
            }

            var delaySeconds = Math.Max(2, settings.OutboxPollingIntervalSeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("<<< [BACKGROUND WORKER] EmailOutboxDispatcherService đã dừng an toàn.");
    }
}
