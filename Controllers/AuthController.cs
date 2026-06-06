using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
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
}
