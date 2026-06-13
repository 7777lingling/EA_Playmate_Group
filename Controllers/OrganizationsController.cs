using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequirePermission("Organization.Manage")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;

    public OrganizationsController(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrganizationDto>>> GetOrganizations()
    {
        if (!IsSystemAdmin())
        {
            return Forbid();
        }

        return Ok(await _db.Organizations.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> CreateOrganization(SaveOrganizationRequestDto request)
    {
        if (!IsSystemAdmin())
        {
            return Forbid();
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "請輸入組織名稱。" });
        }

        if (await _db.Organizations.AnyAsync(x => x.Name == name))
        {
            return Conflict(new { message = "組織名稱已存在。" });
        }

        var organization = new Organization
        {
            Name = name,
            IsActive = request.IsActive
        };
        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create(
            "create",
            "organizations",
            organization.Id,
            after: ToDto(organization)));
        await _db.SaveChangesAsync();
        return Ok(ToDto(organization));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrganizationDto>> UpdateOrganization(
        int id,
        SaveOrganizationRequestDto request)
    {
        if (!IsSystemAdmin())
        {
            return Forbid();
        }

        var organization = await _db.Organizations.FirstOrDefaultAsync(x => x.Id == id);
        if (organization is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "請輸入組織名稱。" });
        }

        if (await _db.Organizations.AnyAsync(x => x.Id != id && x.Name == name))
        {
            return Conflict(new { message = "組織名稱已存在。" });
        }

        var before = ToDto(organization);
        organization.Name = name;
        organization.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create(
            "update",
            "organizations",
            organization.Id,
            before: before,
            after: ToDto(organization)));
        await _db.SaveChangesAsync();
        return Ok(ToDto(organization));
    }

    private bool IsSystemAdmin() =>
        HttpContext.Session.GetString(AuthService.SessionSystemRole) == "admin";

    private static OrganizationDto ToDto(Organization organization) => new()
    {
        Id = organization.Id,
        Name = organization.Name,
        IsActive = organization.IsActive,
        CreatedAt = organization.CreatedAt
    };
}
