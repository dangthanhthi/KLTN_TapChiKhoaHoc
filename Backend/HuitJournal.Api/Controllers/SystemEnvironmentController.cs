using HuitJournal.Api.Data;
using HuitJournal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Controllers;

[ApiController]
[Route("api/system")]
[Authorize(Roles = "Quản trị hệ thống,Ban biên tập")]
public class SystemEnvironmentController : ControllerBase
{
    private readonly QLTapChiKhoaHocContext _context;
    private readonly IWebHostEnvironment _environment;

    public SystemEnvironmentController(QLTapChiKhoaHocContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("environment")]
    public async Task<IActionResult> GetEnvironment()
    {
        await _context.Database.OpenConnectionAsync();
        try
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CONVERT(nvarchar(256), SERVERPROPERTY('ServerName')), DB_NAME()";
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return StatusCode(503, new { message = "Không xác định được CSDL đang dùng." });

            return Ok(new
            {
                environment = _environment.EnvironmentName,
                serverName = reader.GetString(0),
                databaseName = reader.GetString(1)
            });
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// Giám sát tình trạng hàng đợi thư điện tử EmailOutbox
    /// </summary>
    [HttpGet("outbox-status")]
    public async Task<IActionResult> GetOutboxStatus([FromServices] IEmailSenderService emailSender, CancellationToken cancellationToken)
    {
        var status = await emailSender.GetOutboxStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Kích hoạt thủ công chu kỳ gửi thư hàng đợi EmailOutbox
    /// </summary>
    [HttpPost("flush-outbox")]
    public async Task<IActionResult> FlushOutbox([FromServices] IEmailSenderService emailSender, [FromQuery] int batchSize = 20)
    {
        var sentCount = await emailSender.ProcessPendingOutboxAsync(batchSize);
        return Ok(new
        {
            success = true,
            sentCount,
            message = $"Đã xử lý {sentCount} email từ hàng đợi Outbox."
        });
    }

    /// <summary>
    /// Kích hoạt thủ công chu kỳ dọn dẹp hồ sơ quá hạn và thanh lọc thư outbox cũ
    /// </summary>
    [HttpPost("trigger-cleanup")]
    public async Task<IActionResult> TriggerCleanup([FromServices] IEmailSenderService emailSender, CancellationToken cancellationToken)
    {
        var result = await emailSender.CleanupExpiredRecordsAsync(cancellationToken);
        return Ok(new
        {
            success = true,
            expiredRegistrations = result.ExpiredRegistrations,
            cleanedOutbox = result.CleanedOutbox,
            message = $"Hoàn tất dọn dẹp: {result.ExpiredRegistrations} hồ sơ hết hạn và {result.CleanedOutbox} bản ghi outbox cũ."
        });
    }
}
