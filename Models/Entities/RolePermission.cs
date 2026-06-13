namespace EAPlaymateGroup.Models.Entities;

public sealed class RolePermission
{
    public int Id { get; set; }
    public string SystemRole { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
    public DateTime UpdatedAt { get; set; }
}
