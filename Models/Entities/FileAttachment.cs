namespace EAPlaymateGroup.Models.Entities;

public sealed class FileAttachment : IOrganizationScoped
{
    public long Id { get; set; }
    public int OrganizationId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public Guid? TargetUuid { get; set; }
    public string? AttachmentKind { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public long FileSize { get; set; }
    public string? Sha256Hash { get; set; }
    public int? UploadedByLoginUserId { get; set; }
    public LoginUser? UploadedByLoginUser { get; set; }
    public string? Note { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByLoginUserId { get; set; }
    public LoginUser? DeletedByLoginUser { get; set; }
    public DateTime CreatedAt { get; set; }
}
