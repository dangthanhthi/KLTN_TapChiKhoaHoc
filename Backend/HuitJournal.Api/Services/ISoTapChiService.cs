using HuitJournal.Api.DTOs;

namespace HuitJournal.Api.Services;

public interface ISoTapChiService
{
    Task<List<SoTapChiListItemDto>> GetPublishedIssuesAsync();
    Task<SoTapChiDetailDto?> GetIssueDetailAsync(int maSoTapChi);
    Task<List<BaiBaoPublicDto>> GetLatestArticlesAsync(int limit = 10);
}
