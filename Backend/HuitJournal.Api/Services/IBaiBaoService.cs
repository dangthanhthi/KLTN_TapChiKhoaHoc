using HuitJournal.Api.DTOs;

namespace HuitJournal.Api.Services;

public interface IBaiBaoService
{
    Task<(bool Success, string Message, int? MaBaiBao, string? MaDinhDanh)> SubmitPaperAsync(int maNguoiDung, BaiBaoSubmitDto dto);
    Task<List<BaiBaoListItemDto>> GetMySubmissionsAsync(int maNguoiDung);
    Task<BaiBaoDetailDto?> GetSubmissionDetailAsync(int maBaiBao, int maNguoiDung, bool isEditorOrAdmin);
    Task<BaiBaoPublicDto?> GetPublicArticleAsync(int maBaiBao);
    Task<(bool Success, string Message)> ResubmitPaperAsync(int maBaiBao, int maNguoiDung, BaiBaoResubmitDto dto);
    Task<(bool Success, string Message, string? PhysicalPath, string? FileName, string? ContentType)> GetManuscriptForAuthorAsync(int maBaiBao, int maNguoiDung, bool isEditorOrAdmin);
    Task<(bool Success, string Message, string? PhysicalPath, string? FileName, string? ContentType)> GetPublicArticlePdfAsync(int maBaiBao);
    Task<(bool Success, string Message, string? DuongDan)> UploadAnonymousManuscriptAsync(int maBaiBao, IFormFile file, int? soVong, int maNguoiThucHien);
    Task<(bool Success, string Message, string? DuongDan)> UploadPublishedPdfAsync(int maBaiBao, IFormFile file, int maNguoiThucHien);
    Task<(bool Success, string Message)> AssignToIssueAsync(int maBaiBao, AssignIssueDto dto, int maNguoiThucHien);
}
