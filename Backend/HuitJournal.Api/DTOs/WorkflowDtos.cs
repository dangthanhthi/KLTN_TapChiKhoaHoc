using System.ComponentModel.DataAnnotations;

namespace HuitJournal.Api.DTOs;

public class ContactRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(50)] public string? ArticleCode { get; set; }
    [Required, MaxLength(100)] public string Subject { get; set; } = "";
    [Required, MaxLength(5000)] public string Message { get; set; } = "";
}
public class ResetRequest { [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = ""; }
public class ResetConfirm
{
    public Guid Id { get; set; }
    [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; set; } = "";
    [Required, MinLength(6), MaxLength(72)] public string Password { get; set; } = "";
}
public class ReasonRequest { [Required, MinLength(10), MaxLength(2000)] public string Reason { get; set; } = ""; }
public class ActionReviewRequest
{
    public bool Approve { get; set; }
    [MaxLength(2000)] public string? Note { get; set; }
}
public class SubmissionDraftRequest
{
    public Guid Id { get; set; }
    [Required, MaxLength(100000)] public string Payload { get; set; } = "{}";
    public IFormFile? Manuscript { get; set; }
    public IFormFile? Bm02 { get; set; }
    public List<IFormFile> Supplements { get; set; } = new();
    [MaxLength(500)] public string? ExpectedVersion { get; set; }
    public bool ReplaceSupplements { get; set; }
    public bool ReplaceManuscript { get; set; }
    public bool ReplaceBm02 { get; set; }
}
public class ScreeningRequest
{
    [Range(0,100)] public decimal SimilarityPercent { get; set; }
    public bool Approve { get; set; }
    [Required, MinLength(10), MaxLength(2000)] public string Note { get; set; } = "";
    public IFormFile? Report { get; set; }
}
public class PublicationNoticeRequest
{
    [Required, RegularExpression("^(Correction|Retraction)$")] public string Kind { get; set; } = "Correction";
    [Required, MinLength(20), MaxLength(5000)] public string Text { get; set; } = "";
}
