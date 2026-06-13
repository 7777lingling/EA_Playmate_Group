using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LoginUsersController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly LoginUserService _loginUserService;

    public LoginUsersController(EAPlaymateGroupDbContext db, LoginUserService loginUserService)
    {
        _db = db;
        _loginUserService = loginUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<LoginUserDto>>> GetLoginUsers()
    {
        var users = await _db.LoginUsers.AsNoTracking()
            .OrderBy(x => x.LoginAccount)
            .ToListAsync();

        return Ok(users.Select(LoginUserMapper.ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoginUserDto>> GetLoginUser(int id)
    {
        var user = await _db.LoginUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return user is null ? NotFound() : Ok(LoginUserMapper.ToDto(user));
    }

    [HttpPost]
    public async Task<ActionResult<LoginUserDto>> CreateLoginUser(CreateLoginUserRequestDto request)
    {
        var result = await _loginUserService.CreateAsync(request);
        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(GetLoginUser), new { id = result.Value!.Id }, result.Value);
        }

        return result.ValidationErrors is not null
            ? ApiErrors.Validation(result.ValidationErrors)
            : ApiErrors.BadRequest(result.ErrorCode ?? "operation_failed", result.ErrorMessage ?? "Operation failed.");
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LoginUserDto>> UpdateLoginUser(int id, UpdateLoginUserRequestDto request)
    {
        var result = await _loginUserService.UpdateAsync(id, request);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        if (result.NotFound)
        {
            return NotFound();
        }

        return result.ValidationErrors is not null
            ? ApiErrors.Validation(result.ValidationErrors)
            : ApiErrors.BadRequest(result.ErrorCode ?? "operation_failed", result.ErrorMessage ?? "Operation failed.");
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateLoginUser(int id)
    {
        var result = await _loginUserService.SetActiveAsync(id, false);
        return result.NotFound ? NotFound() : NoContent();
    }

    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> ActivateLoginUser(int id)
    {
        var result = await _loginUserService.SetActiveAsync(id, true);
        return result.NotFound ? NotFound() : NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLoginUser(int id)
    {
        if (HttpContext.Session.GetInt32(AuthService.SessionUserId) == id)
        {
            return Conflict(new { message = "不可刪除目前登入中的帳號。" });
        }

        var user = await _db.LoginUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (await _db.AuditLogs.AnyAsync(x => x.LoginUserId == id))
        {
            return Conflict(new { message = "此登入者已有操作紀錄，無法刪除；請改為停用。" });
        }

        var before = LoginUserMapper.ToDto(user);
        _db.LoginUsers.Remove(user);
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create("delete", "login_users", id, user.Uuid, before: before));
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
