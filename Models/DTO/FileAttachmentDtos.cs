namespace EAPlaymateGroup.Models.DTO;

public sealed class FileAttachmentDto
{
    public long Id { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public Guid? TargetUuid { get; set; }
    public string? AttachmentKind { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public long FileSize { get; set; }
    public string? Sha256Hash { get; set; }
    public int? UploadedByLoginUserId { get; set; }
    public string? UploadedByDisplayName { get; set; }
    public string? Note { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByLoginUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
