namespace EAPlaymateGroup.Models.DTO;

public sealed class RolePermissionDto
{
    public string SystemRole { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}

public sealed class PermissionMatrixDto
{
    public List<string> PermissionCodes { get; set; } = [];
    public List<RolePermissionDto> Roles { get; set; } = [];
}

public sealed class UpdateRolePermissionsRequestDto
{
    public List<string> Permissions { get; set; } = [];
}
