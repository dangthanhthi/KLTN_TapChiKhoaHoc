using HuitJournal.Api.DTOs;

namespace HuitJournal.Api.Services;

public interface IPhanBienService
{
    Task<(bool Success, string Message, int? MaPhanCong)> AssignReviewerAsync(int maNguoiThucHien, PhanCongRequestDto dto);
    Task<List<PhanCongListItemDto>> GetMyReviewAssignmentsAsync(int maReviewer);
    Task<(bool Success, string Message)> RespondToAssignmentAsync(int maPhanCong, int maReviewer, bool accept);
    Task<PhieuDanhGiaDetailDto?> GetMyEvaluationAsync(int maPhanCong, int maReviewer);
    Task<PhieuDanhGiaBanNhapDto?> GetMyEvaluationDraftAsync(int maPhanCong, int maReviewer);
    Task<List<PhieuDanhGiaBanNhapIndexDto>> GetMyEvaluationDraftsAsync(int maReviewer);
    Task<(bool Success, string Message, DateTimeOffset? SavedAtUtc)> SaveMyEvaluationDraftAsync(int maPhanCong, int maReviewer, PhieuDanhGiaBanNhapDto dto);
    Task<(bool Success, string Message)> DeleteMyEvaluationDraftAsync(int maPhanCong, int maReviewer);
    Task<(bool Success, string Message)> SubmitEvaluationAsync(int maReviewer, PhieuDanhGiaDto dto);
    Task<(bool Success, string Message)> MakeEditorialDecisionAsync(int maEditor, QuyetDinhBienTapDto dto);
    Task<(bool Success, string Message, string? PhysicalPath, string? FileName, string? ContentType)> GetManuscriptForReviewerAsync(int maPhanCong, int maReviewer, bool isEditorOrAdmin);
}
