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
            .FirstOrDefaultAsync(x => x.Id == id);

        return log is null ? NotFound() : Ok(ToDto(log));
    }

    [HttpPost]
    public async Task<ActionResult<AuditLogDto>> CreateAuditLog(CreateAuditLogRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Action))
        {
            return BadRequest("Action is required.");
        }

        if (string.IsNullOrWhiteSpace(request.TargetType))
        {
            return BadRequest("TargetType is required.");
        }

        var log = new AuditLog
        {
            UserId = request.UserId,
            Action = request.Action.Trim(),
            TargetType = request.TargetType.Trim(),
            TargetId = request.TargetId,
            TargetUuid = request.TargetUuid,
            BeforeJson = request.BeforeJson,
            AfterJson = request.AfterJson
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();

        var savedLog = await _db.AuditLogs.AsNoTracking()
            .Include(x => x.User)
            .FirstAsync(x => x.Id == log.Id);

        return CreatedAtAction(nameof(GetAuditLog), new { id = log.Id }, ToDto(savedLog));
    }

    private static AuditLogDto ToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserNickname = log.User?.Nickname,
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
