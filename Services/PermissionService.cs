using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class PermissionService
{
    private readonly EAPlaymateGroupDbContext _db;

    public PermissionService(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasPermissionAsync(int loginUserId, string permissionCode)
    {
        if (!PermissionCodes.IsValid(permissionCode))
        {
            return false;
        }

        var role = await _db.LoginUsers.AsNoTracking()
            .Where(x => x.Id == loginUserId && x.IsActive)
            .Select(x => x.SystemRole)
            .FirstOrDefaultAsync();

        if (role == "admin")
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return await _db.RolePermissions.AsNoTracking().AnyAsync(x =>
            x.SystemRole == role &&
            x.PermissionCode == permissionCode &&
            x.IsAllowed);
    }

    public async Task<bool> IsSystemAdminAsync(int loginUserId)
    {
        return await _db.LoginUsers.AsNoTracking().AnyAsync(x =>
            x.Id == loginUserId &&
            x.IsActive &&
            x.SystemRole == "admin");
    }

    public async Task<PermissionMatrixDto> GetMatrixAsync()
    {
        var rows = await _db.RolePermissions.AsNoTracking().ToListAsync();
        return new PermissionMatrixDto
        {
            PermissionCodes = PermissionCodes.All.ToList(),
            Roles = DomainValues.SystemRoles.Select(role => new RolePermissionDto
            {
                SystemRole = role,
                Permissions = role == "admin"
                    ? PermissionCodes.All.ToList()
                    : rows.Where(x => x.SystemRole == role && x.IsAllowed)
                        .Select(x => x.PermissionCode)
                        .OrderBy(x => x)
                        .ToList()
            }).ToList()
        };
    }

    public async Task<ServiceResult<RolePermissionDto>> UpdateRoleAsync(
        string role,
        UpdateRolePermissionsRequestDto request)
    {
        if (!DomainValues.IsSystemRole(role))
        {
            return ServiceResult<RolePermissionDto>.Validation(
                new Dictionary<string, string[]> { ["role"] = ["無效的系統角色。"] });
        }

        if (role == "admin")
        {
            return ServiceResult<RolePermissionDto>.Failure(
                "system_admin_locked",
                "系統管理員固定擁有全部權限，不可移除。");
        }

        var permissions = request.Permissions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var invalid = permissions.Where(x => !PermissionCodes.IsValid(x)).ToList();
        if (invalid.Count > 0)
        {
            return ServiceResult<RolePermissionDto>.Validation(
                new Dictionary<string, string[]> { ["permissions"] = [$"無效權限碼：{string.Join(", ", invalid)}"] });
        }

        var rows = await _db.RolePermissions.Where(x => x.SystemRole == role).ToListAsync();
        var beforePermissions = rows
            .Where(x => x.IsAllowed)
            .Select(x => x.PermissionCode)
            .OrderBy(x => x)
            .ToList();
        var now = DateTime.UtcNow;
        foreach (var code in PermissionCodes.All)
        {
            var row = rows.FirstOrDefault(x => x.PermissionCode == code);
            if (row is null)
            {
                row = new RolePermission
                {
                    SystemRole = role,
                    PermissionCode = code
                };
                _db.RolePermissions.Add(row);
            }

            row.IsAllowed = permissions.Contains(code, StringComparer.Ordinal);
            row.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create(
            "update",
            "role_permissions",
            before: new { role, permissions = beforePermissions },
            after: new { role, permissions = permissions.OrderBy(x => x).ToList() }));
        await _db.SaveChangesAsync();

        return ServiceResult<RolePermissionDto>.Success(new RolePermissionDto
        {
            SystemRole = role,
            Permissions = permissions.OrderBy(x => x).ToList()
        });
    }
}
