using EAPlaymateGroup.Common;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
[PublicApi]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly LoginUserService _loginUserService;

    public AuthController(AuthService authService, LoginUserService loginUserService)
    {
        _authService = authService;
        _loginUserService = loginUserService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<AuthMeDto>> Me()
    {
        return Ok(new AuthMeDto
        {
            AuthRequired = await _authService.IsAuthRequiredAsync(),
            IsAuthenticated = HttpContext.Session.GetInt32(AuthService.SessionUserId).HasValue,
            User = await _authService.GetCurrentUserAsync(HttpContext)
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthMeDto>> Login(LoginRequestDto request)
    {
        var user = await _authService.LoginAsync(request);
        if (user is null)
        {
            return Unauthorized(new { message = "帳號或密碼錯誤，或此帳號已停用。" });
        }

        HttpContext.Session.SetInt32(AuthService.SessionUserId, user.Id);
        HttpContext.Session.SetInt32(AuthService.SessionOrganizationId, user.OrganizationId);
        HttpContext.Session.SetString(AuthService.SessionSystemRole, user.SystemRole);
        if (user.UserId.HasValue)
        {
            HttpContext.Session.SetInt32(AuthService.SessionMemberUserId, user.UserId.Value);
        }

        return Ok(new AuthMeDto
        {
            AuthRequired = true,
            IsAuthenticated = true,
            User = user
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangeMyPasswordRequestDto request)
    {
        var loginUserId = HttpContext.Session.GetInt32(AuthService.SessionUserId);
        if (!loginUserId.HasValue)
        {
            return Unauthorized(new { message = "請先登入。" });
        }

        var result = await _loginUserService.ChangeMyPasswordAsync(loginUserId.Value, request);
        if (result.Succeeded)
        {
            return NoContent();
        }

        if (result.NotFound)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "登入帳號不存在或已停用。" });
        }

        return result.ValidationErrors is not null
            ? Common.ApiErrors.Validation(result.ValidationErrors)
            : Common.ApiErrors.BadRequest(
                result.ErrorCode ?? "operation_failed",
                result.ErrorMessage ?? "Operation failed.");
    }
}
