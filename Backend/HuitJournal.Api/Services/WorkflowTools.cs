using System.Net;
using System.Text.Json;
using HuitJournal.Api.Data;
using HuitJournal.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Services;

public static class WorkflowTools
{
    public const string ConsentVersion = "HUIT-2026-01";
    public static readonly TimeZoneInfo VietnamZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    public static DateTime VietnamNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamZone);
    public static EmailOutbox Mail(string kind, string email, string subject, string text) => new()
    {
        LoaiThu = kind, NguoiNhan = email.Trim().ToLowerInvariant(), TieuDe = subject.Length > 255 ? subject[..255] : subject,
        NoiDungText = text, NoiDungHtml = $"<p style=\"white-space:pre-wrap\">{WebUtility.HtmlEncode(text)}</p>",
        TrangThai = "Pending", TaoLucUtc = DateTime.UtcNow
    };
    public static T? Read<T>(WorkflowRecord? record) => record == null ? default : JsonSerializer.Deserialize<T>(record.Payload);
    public static async Task LockAsync(QLTapChiKhoaHocContext db, string key) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result = sp_getapplock @Resource={key}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @result < 0 THROW 50001, 'Workflow is busy. Retry later.', 1;");
}
