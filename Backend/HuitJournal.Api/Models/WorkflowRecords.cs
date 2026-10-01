using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuitJournal.Api.Models;

[Table("JournalWorkflowRecord")]
public class WorkflowRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public int? UserId { get; set; }
    public int? ArticleId { get; set; }
    [MaxLength(40)] public string Kind { get; set; } = "";
    [MaxLength(30)] public string State { get; set; } = "Pending";
    public string Payload { get; set; } = "{}";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = null!;
}

[Table("JournalWorkflowFile")]
public class WorkflowFile
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordId { get; set; }
    public WorkflowRecord Record { get; set; } = null!;
    public int UserId { get; set; }
    public int? ArticleId { get; set; }
    [MaxLength(40)] public string Kind { get; set; } = "";
    [MaxLength(255)] public string Name { get; set; } = "";
    [MaxLength(500)] public string Path { get; set; } = "";
    public long Size { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
