using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;

    public AuditLogsController(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? targetType,
        [FromQuery] int? targetId,
        [FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 500);

        var query = _db.AuditLogs.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.LoginUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            query = query.Where(x => x.TargetType == targetType);
        }

        if (targetId.HasValue)
        {
            query = query.Where(x => x.TargetId == targetId.Value);
        }

        var logs = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AuditLogDto>> GetAuditLog(long id)
    {
        var log = await _db.AuditLogs.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.LoginUser)
            .FirstOrDefaultAsync(x => x.Id == id);

        return log is null ? NotFound() : Ok(ToDto(log));
    }

    private static AuditLogDto ToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserNickname = log.User?.Nickname,
            LoginUserId = log.LoginUserId,
            LoginUserDisplayName = log.LoginUser?.DisplayName,
            LoginAccount = log.LoginUser?.LoginAccount,
            Action = log.Action,
            TargetType = log.TargetType,
            TargetId = log.TargetId,
            TargetUuid = log.TargetUuid,
            BeforeJson = log.BeforeJson,
            AfterJson = log.AfterJson,
            CreatedAt = log.CreatedAt
        };
    }
}
