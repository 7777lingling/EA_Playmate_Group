namespace EAPlaymateGroup.Models.Entities;

public sealed class FileAttachment : IOrganizationScoped
{
    public long Id { get; set; }
    public int OrganizationId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public Guid? TargetUuid { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? UploadedByLoginUserId { get; set; }
    public LoginUser? UploadedByLoginUser { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
