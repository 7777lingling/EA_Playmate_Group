using EAPlaymateGroup.Common;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PermissionsController : ControllerBase
{
    private readonly PermissionService _permissionService;

    public PermissionsController(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<ActionResult<PermissionMatrixDto>> GetPermissions()
    {
        if (!await IsSystemAdminAsync())
        {
            return Forbid();
        }

        return Ok(await _permissionService.GetMatrixAsync());
    }

    [HttpPut("{role}")]
    public async Task<ActionResult<RolePermissionDto>> UpdatePermissions(
        string role,
        UpdateRolePermissionsRequestDto request)
    {
        if (!await IsSystemAdminAsync())
        {
            return Forbid();
        }

        var result = await _permissionService.UpdateRoleAsync(role, request);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.ValidationErrors is not null
            ? ApiErrors.Validation(result.ValidationErrors)
            : ApiErrors.BadRequest(
                result.ErrorCode ?? "operation_failed",
                result.ErrorMessage ?? "Operation failed.");
    }

    private async Task<bool> IsSystemAdminAsync()
    {
        var loginUserId = HttpContext.Session.GetInt32(AuthService.SessionUserId);
        return loginUserId.HasValue &&
               await _permissionService.IsSystemAdminAsync(loginUserId.Value);
    }
}
