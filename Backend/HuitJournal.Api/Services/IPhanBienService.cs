using HuitJournal.Api.DTOs;

namespace HuitJournal.Api.Services;

public interface IPhanBienService
{
    Task<(bool Success, string Message, int? MaPhanCong)> AssignReviewerAsync(int maNguoiThucHien, PhanCongRequestDto dto);
    Task<List<PhanCongListItemDto>> GetMyReviewAssignmentsAsync(int maReviewer);
    Task<(bool Success, string Message)> SubmitEvaluationAsync(int maReviewer, PhieuDanhGiaDto dto);
    Task<(bool Success, string Message)> MakeEditorialDecisionAsync(int maEditor, QuyetDinhBienTapDto dto);
    Task<(bool Success, string Message, string? PhysicalPath, string? FileName, string? ContentType)> GetManuscriptForReviewerAsync(int maPhanCong, int maReviewer, bool isEditorOrAdmin);
}
