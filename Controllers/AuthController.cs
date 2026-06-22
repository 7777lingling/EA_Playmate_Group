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
    private const string DiscordOAuthStateSessionKey = "discord_oauth_state";
    private const string DiscordOAuthModeSessionKey = "discord_oauth_mode";

    private readonly AuthService _authService;
    private readonly DiscordAuthService _discordAuthService;
    private readonly LoginUserService _loginUserService;

    public AuthController(
        AuthService authService,
        DiscordAuthService discordAuthService,
        LoginUserService loginUserService)
    {
        _authService = authService;
        _discordAuthService = discordAuthService;
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

        SignIn(user);
        await _authService.RecordAuthEventAsync(user.Id, "login");

        return Ok(new AuthMeDto
        {
            AuthRequired = true,
            IsAuthenticated = true,
            User = user
        });
    }

    [HttpGet("discord/login")]
    public IActionResult DiscordLogin()
    {
        if (!_discordAuthService.IsConfigured)
        {
            return Redirect("/?loginError=discord_config");
        }

        var state = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString(DiscordOAuthStateSessionKey, state);
        HttpContext.Session.SetString(DiscordOAuthModeSessionKey, "login");
        return Redirect(_discordAuthService.BuildAuthorizationUrl(BuildDiscordRedirectUri(), state));
    }

    [HttpGet("discord/link")]
    public IActionResult DiscordLink()
    {
        if (!_discordAuthService.IsConfigured)
        {
            return Redirect("/?discordLink=config");
        }

        if (!HttpContext.Session.GetInt32(AuthService.SessionUserId).HasValue)
        {
            return Unauthorized();
        }

        var state = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString(DiscordOAuthStateSessionKey, state);
        HttpContext.Session.SetString(DiscordOAuthModeSessionKey, "link");
        return Redirect(_discordAuthService.BuildAuthorizationUrl(BuildDiscordRedirectUri(), state, "consent"));
    }

    [HttpGet("/auth/discord/callback")]
    public async Task<IActionResult> DiscordCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error)
    {
        var mode = HttpContext.Session.GetString(DiscordOAuthModeSessionKey);
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Redirect(mode == "link"
                ? "/?discordLink=denied"
                : "/?loginError=discord_denied");
        }

        var expectedState = HttpContext.Session.GetString(DiscordOAuthStateSessionKey);
        HttpContext.Session.Remove(DiscordOAuthStateSessionKey);
        HttpContext.Session.Remove(DiscordOAuthModeSessionKey);
        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(state) ||
            string.IsNullOrWhiteSpace(expectedState) ||
            state != expectedState)
        {
            return Redirect(mode == "link"
                ? "/?discordLink=state"
                : "/?loginError=discord_state");
        }

        try
        {
            var profile = await _discordAuthService.GetUserProfileAsync(code, BuildDiscordRedirectUri());
            if (mode == "link")
            {
                var loginUserId = HttpContext.Session.GetInt32(AuthService.SessionUserId);
                if (!loginUserId.HasValue)
                {
                    return Redirect("/?discordLink=session");
                }

                var linkResult = await _authService.LinkDiscordAsync(loginUserId.Value, profile);
                return Redirect($"/?discordLink={linkResult}");
            }

            var user = await _authService.LoginWithDiscordAsync(profile);
            if (user is null)
            {
                return Redirect("/?loginError=discord_unbound");
            }

            SignIn(user);
            await _authService.RecordAuthEventAsync(user.Id, "login");
            return Redirect("/");
        }
        catch
        {
            return Redirect(mode == "link"
                ? "/?discordLink=failed"
                : "/?loginError=discord_failed");
        }
    }

    [HttpDelete("discord/link")]
    public async Task<IActionResult> DiscordUnlink()
    {
        var loginUserId = HttpContext.Session.GetInt32(AuthService.SessionUserId);
        if (!loginUserId.HasValue)
        {
            return Unauthorized();
        }

        return await _authService.UnlinkDiscordAsync(loginUserId.Value)
            ? NoContent()
            : NotFound();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var loginUserId = HttpContext.Session.GetInt32(AuthService.SessionUserId);
        if (loginUserId.HasValue)
        {
            await _authService.RecordAuthEventAsync(loginUserId.Value, "logout");
        }
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

    private void SignIn(LoginUserDto user)
    {
        HttpContext.Session.SetInt32(AuthService.SessionUserId, user.Id);
        HttpContext.Session.SetInt32(AuthService.SessionOrganizationId, user.OrganizationId);
        HttpContext.Session.SetString(AuthService.SessionSystemRole, user.SystemRole);
        if (user.UserId.HasValue)
        {
            HttpContext.Session.SetInt32(AuthService.SessionMemberUserId, user.UserId.Value);
        }
        else
        {
            HttpContext.Session.Remove(AuthService.SessionMemberUserId);
        }
    }

    private string BuildDiscordRedirectUri()
    {
        var configured = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Discord:RedirectUri"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return Url.ActionLink(
            nameof(DiscordCallback),
            "Auth",
            values: null,
            protocol: Request.Scheme,
            host: Request.Host.ToString())!;
    }
}
