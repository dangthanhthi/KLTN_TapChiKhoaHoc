using HuitJournal.Api.Infrastructure;
using HuitJournal.Api.Models;

namespace HuitJournal.Api.Services;

public static class WorkflowFileStorage
{
    public static async Task ValidateAsync(IFormFile file, bool data = false)
    {
        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = data ? new[] { ".pdf", ".doc", ".docx", ".csv", ".xlsx" } : new[] { ".pdf", ".doc", ".docx" };
        if (!allowed.Contains(ext) || file.Length < 1 || file.Length > 30 * 1024 * 1024 || file.FileName.Length > 255)
            throw new ArgumentException("Tệp phải thuộc định dạng được hỗ trợ, tên tối đa 255 ký tự và dung lượng tối đa 30 MB.");
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var count = await stream.ReadAsync(header);
        if ((ext == ".pdf" && (count < 5 || System.Text.Encoding.ASCII.GetString(header, 0, 5) != "%PDF-")) ||
            ((ext == ".docx" || ext == ".xlsx") && (count < 4 || header[0] != 0x50 || header[1] != 0x4b)) ||
            (ext == ".doc" && (count < 8 || !header.SequenceEqual(new byte[] { 0xd0,0xcf,0x11,0xe0,0xa1,0xb1,0x1a,0xe1 }))))
            throw new ArgumentException("Nội dung tệp không khớp định dạng khai báo.");
    }

    public static async Task<WorkflowFile> SaveAsync(string contentRoot, WorkflowRecord record, int owner, string kind, IFormFile file)
    {
        var relative = $"Workflow/{record.Id:N}/{Guid.NewGuid():N}{System.IO.Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var physical = System.IO.Path.Combine(UploadStoragePaths.GetRoot(contentRoot), relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(physical)!);
        try
        {
            await using var stream = File.Create(physical);
            await file.CopyToAsync(stream);
        }
        catch { if (File.Exists(physical)) File.Delete(physical); throw; }
        return new WorkflowFile { RecordId = record.Id, UserId = owner, ArticleId = record.ArticleId,
            Kind = kind, Name = System.IO.Path.GetFileName(file.FileName), Size = file.Length, Path = "/Uploads/" + relative };
    }
    public static void Delete(string root, WorkflowFile file)
    {
        var physical = UploadStoragePaths.ResolveExistingFile(root, file.Path);
        if (physical != null) File.Delete(physical);
    }
}
