using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Common;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly UserService _userService;

    public UsersController(EAPlaymateGroupDbContext db, UserService userService)
    {
        _db = db;
        _userService = userService;
    }

    [HttpGet]
    [RequirePermission("Member.View")]
    public async Task<ActionResult<List<UserDto>>> GetUsers(
        [FromQuery] bool? isPlayer,
        [FromQuery] bool? isBoss,
        [FromQuery] bool activeOnly = true)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        if (isPlayer.HasValue)
        {
            query = query.Where(x => x.IsPlayer == isPlayer.Value);
        }

        if (isBoss.HasValue)
        {
            query = query.Where(x => x.IsBoss == isBoss.Value);
        }

        var userEntities = await query
            .OrderBy(x => x.Nickname)
            .ToListAsync();

        var users = userEntities.Select(UserMapper.ToDto).ToList();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("Member.View")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var userEntity = await _db.Users.AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        return userEntity is null ? NotFound() : Ok(UserMapper.ToDto(userEntity));
    }

    [HttpGet("players")]
    [RequirePermission("Member.View")]
    public async Task<ActionResult<List<UserDto>>> GetPlayers()
    {
        var userEntities = await _db.Users.AsNoTracking()
            .Where(x => x.IsActive && x.IsPlayer)
            .OrderBy(x => x.Nickname)
            .ToListAsync();

        var users = userEntities.Select(UserMapper.ToDto).ToList();

        return Ok(users);
    }

    [HttpGet("bosses")]
    [RequirePermission("Member.View")]
    public async Task<ActionResult<List<UserDto>>> GetBosses()
    {
        var userEntities = await _db.Users.AsNoTracking()
            .Where(x => x.IsActive && x.IsBoss)
            .OrderBy(x => x.Nickname)
            .ToListAsync();

        var users = userEntities.Select(UserMapper.ToDto).ToList();

        return Ok(users);
    }

    [HttpPost]
    [RequirePermission("Member.Create")]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequestDto request)
    {
        var result = await _userService.CreateUserAsync(request);
        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Member.Edit")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserRequestDto request)
    {
        var result = await _userService.UpdateUserAsync(id, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id:int}/deactivate")]
    [RequirePermission("Member.Edit")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var result = await _userService.DeactivateUserAsync(id);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id:int}/activate")]
    [RequirePermission("Member.Edit")]
    public async Task<IActionResult> ActivateUser(int id)
    {
        var result = await _userService.ActivateUserAsync(id);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id:int}/leave")]
    [RequirePermission("Member.Edit")]
    public async Task<IActionResult> LeaveUser(int id, LeaveUserRequestDto request)
    {
        var result = await _userService.LeaveUserAsync(id, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("Member.Delete")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        var isReferenced =
            await _db.Orders.AnyAsync(x => x.OwnerUserId == id) ||
            await _db.OrderMembers.AnyAsync(x => x.UserId == id) ||
            await _db.Payments.AnyAsync(x => x.UserId == id) ||
            await _db.GiftRecords.AnyAsync(x => x.BossUserId == id || x.RecipientUserId == id) ||
            await _db.DepartmentMembers.AnyAsync(x => x.UserId == id) ||
            await _db.AuditLogs.AnyAsync(x => x.UserId == id);

        if (isReferenced)
        {
            return Conflict(new { message = "此成員已有關聯資料，無法刪除；請改為停用。" });
        }

        var before = UserMapper.ToDto(user);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create("delete", "users", id, user.Uuid, before: before));
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private ActionResult ToActionResult(ServiceResult result)
    {
        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.ValidationErrors is not null)
        {
            return ApiErrors.Validation(result.ValidationErrors);
        }

        return ApiErrors.BadRequest(
            result.ErrorCode ?? "operation_failed",
            result.ErrorMessage ?? "Operation failed.");
    }

    private ActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        return ToActionResult(new ServiceResult
        {
            NotFound = result.NotFound,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            ValidationErrors = result.ValidationErrors
        });
    }
}
