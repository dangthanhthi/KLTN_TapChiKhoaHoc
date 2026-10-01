using HuitJournal.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HuitJournal.Api.Services;

public static class CoauthorAccountLinker
{
    // Preserve authorship snapshots and never replace an existing account link.
    public static async Task<int> LinkAsync(QLTapChiKhoaHocContext context, int userId)
    {
        var email = await context.NguoiDungs.AsNoTracking()
            .Where(u => u.MaNguoiDung == userId && u.TrangThai)
            .Select(u => u.Email).SingleOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(email)) return 0;
        var normalized = email.Trim().ToLowerInvariant();
        return await context.DongTacGias
            .Where(d => d.MaNguoiDung == null && d.Email.Trim().ToLower() == normalized)
            .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.MaNguoiDung, (int?)userId));
    }
}
