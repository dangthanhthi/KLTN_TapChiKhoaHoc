using System.Security.Claims;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using HuitJournal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Controllers;

[ApiController, Route("api/orcid")]
public sealed class OrcidController(OrcidOAuthService orcid, QLTapChiKhoaHocContext db, IConfiguration config, ILogger<OrcidController> logger) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("registration/start"), AllowAnonymous]
    public async Task<IActionResult> StartRegistration(CancellationToken ct)
    {
        try
        {
            var (state, authorizationUrl) = await orcid.StartAsync(null, ResolveReturnBase(), ct);
            return Ok(new { state, authorizationUrl });
        }
        catch (InvalidOperationException e) { return StatusCode(503, new { message = e.Message }); }
    }

    [HttpPost("link/start"), Authorize]
    public async Task<IActionResult> StartLink(CancellationToken ct)
    {
        try
        {
            var (state, authorizationUrl) = await orcid.StartAsync(UserId, ResolveReturnBase(), ct);
            return Ok(new { state, authorizationUrl });
        }
        catch (InvalidOperationException e) { return StatusCode(503, new { message = e.Message }); }
    }

    [HttpGet("registration/{state:guid}"), AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> RegistrationResult(Guid state, CancellationToken ct)
    {
        try
        {
            var (id, name) = await orcid.GetRegistrationResultAsync(state, ct);
            return Ok(new { orcid = id, name });
        }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    [HttpGet("callback"), AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] Guid? state, [FromQuery] string? error, CancellationToken ct)
    {
        var configuredWebBase = config["Orcid:PublicWebBaseUrl"]?.TrimEnd('/');
        var webBase = configuredWebBase;
        if (state.HasValue)
        {
            var startingFlow = await db.WorkflowRecords.AsNoTracking().SingleOrDefaultAsync(r => r.Id == state && (r.Kind == "OrcidOAuth" || r.Kind == "OrcidLink"), ct);
            if (startingFlow != null)
            {
                try
                {
                    using var payload = System.Text.Json.JsonDocument.Parse(startingFlow.Payload);
                    if (payload.RootElement.TryGetProperty("returnBase", out var returnBase)) webBase = returnBase.GetString()?.TrimEnd('/') ?? webBase;
                }
                catch (System.Text.Json.JsonException) { }
            }
        }
        if (string.IsNullOrWhiteSpace(webBase)) return StatusCode(503, "ORCID callback chưa được cấu hình.");
        WorkflowRecord? flow = null;
        if (state.HasValue)
            flow = await db.WorkflowRecords.SingleOrDefaultAsync(r => r.Id == state && (r.Kind == "OrcidOAuth" || r.Kind == "OrcidLink"), ct);
        var target = flow?.UserId.HasValue == true ? "profile.html" : "register.html";
        if (!string.IsNullOrEmpty(error) || string.IsNullOrWhiteSpace(code) || !state.HasValue)
            return Redirect($"{webBase}/{target}#orcid-error=cancelled");
        try
        {
            await orcid.CompleteAsync(code, state.Value, ct);
            return Redirect($"{webBase}/{target}#orcid-state={Uri.EscapeDataString(state.Value.ToString("D"))}");
        }
        catch (InvalidOperationException e)
        {
            logger.LogInformation("ORCID authorization was not completed: {Message}", e.Message);
            return Redirect($"{webBase}/{target}#orcid-error=link");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ORCID callback failed");
            return Redirect($"{webBase}/{target}#orcid-error=service");
        }
    }

    [HttpDelete("link"), Authorize]
    public async Task<IActionResult> Unlink(CancellationToken ct)
    {
        try { await orcid.UnlinkAsync(UserId, ct); return Ok(new { success = true }); }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
    }

    private string ResolveReturnBase()
    {
        var fallback = config["Orcid:PublicWebBaseUrl"]?.TrimEnd('/') ?? "";
        var origin = Request.Headers.Origin.ToString().TrimEnd('/');
        if (string.IsNullOrEmpty(origin)) return fallback;
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) throw new InvalidOperationException("Origin ORCID không hợp lệ.");
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction() &&
            uri.Scheme == "http" && (uri.Host == "localhost" || uri.Host == "127.0.0.1")) return origin;
        var allowed = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowed.Any(value => string.Equals(value.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase))) return origin;
        throw new InvalidOperationException("Website này chưa được cho phép quay lại sau khi liên kết ORCID.");
    }
}
