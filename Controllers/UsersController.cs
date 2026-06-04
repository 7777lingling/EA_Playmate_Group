using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;

    public UsersController(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
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

        var users = await query
            .OrderBy(x => x.Nickname)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Nickname))
        {
            return BadRequest("Nickname is required.");
        }

        var user = new User
        {
            Nickname = request.Nickname.Trim(),
            DiscordId = string.IsNullOrWhiteSpace(request.DiscordId) ? null : request.DiscordId.Trim(),
            DiscordName = string.IsNullOrWhiteSpace(request.DiscordName) ? null : request.DiscordName.Trim(),
            BankAccount = string.IsNullOrWhiteSpace(request.BankAccount) ? null : request.BankAccount.Trim(),
            SystemRole = request.SystemRole,
            IsPlayer = request.IsPlayer,
            IsBoss = request.IsBoss
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = ToDto(user);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Nickname))
        {
            return BadRequest("Nickname is required.");
        }

        user.Nickname = request.Nickname.Trim();
        user.DiscordId = string.IsNullOrWhiteSpace(request.DiscordId) ? null : request.DiscordId.Trim();
        user.DiscordName = string.IsNullOrWhiteSpace(request.DiscordName) ? null : request.DiscordName.Trim();
        user.BankAccount = string.IsNullOrWhiteSpace(request.BankAccount) ? null : request.BankAccount.Trim();
        user.SystemRole = request.SystemRole;
        user.IsPlayer = request.IsPlayer;
        user.IsBoss = request.IsBoss;
        user.IsActive = request.IsActive;
        user.LeftAt = request.LeftAt;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Uuid = user.Uuid,
            Nickname = user.Nickname,
            DiscordId = user.DiscordId,
            DiscordName = user.DiscordName,
            BankAccount = user.BankAccount,
            SystemRole = user.SystemRole,
            IsPlayer = user.IsPlayer,
            IsBoss = user.IsBoss,
            IsActive = user.IsActive,
            LeftAt = user.LeftAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
