using EAPlaymateGroup.Common;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UserPreferencesController : ControllerBase
{
    private readonly UserPreferenceService _preferences;

    public UserPreferencesController(UserPreferenceService preferences)
    {
        _preferences = preferences;
    }

    [HttpGet("me")]
    [RequirePermission("Profile.Manage")]
    public async Task<ActionResult<UserPreferenceDto>> GetMe()
    {
        var loginUserId = HttpContext.Session.GetInt32(AuthService.SessionUserId);
        if (!loginUserId.HasValue)
        {
            return Unauthorized();
        }

        var preference = await _preferences.GetAsync(loginUserId.Value);
        return preference is null ? NotFound() : Ok(preference);
    }

    [HttpPut("me")]
    [RequirePermission("Profile.Manage")]
    public async Task<ActionResult<UserPreferenceDto>> UpdateMe(UpdateUserPreferenceRequestDto request)
    {
        var loginUserId = HttpContext.Session.GetInt32(AuthService.SessionUserId);
        if (!loginUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _preferences.UpdateAsync(loginUserId.Value, request);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        if (result.ValidationErrors is not null)
        {
            return ApiErrors.Validation(result.ValidationErrors);
        }

        return result.NotFound
            ? NotFound()
            : ApiErrors.BadRequest(result.ErrorCode ?? "operation_failed", result.ErrorMessage ?? "Operation failed.");
    }
}
