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
        if (!await CanManageOrganizationsAsync())
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
        if (!await CanManageOrganizationsAsync())
        {
            return Forbid();
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "請輸入組織名稱。" });
        }

        var organization = new Organization
        {
            Name = name,
            IsActive = request.IsActive
        };
        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync();

        await SeedOrganizationDefaultsAsync(organization.Id);

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
        if (!await CanManageOrganizationsAsync())
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

    private async Task<bool> CanManageOrganizationsAsync()
    {
        if (HttpContext.Session.GetString(AuthService.SessionSystemRole) == "admin")
        {
            return true;
        }

        return !await _db.LoginUsers
            .IgnoreQueryFilters()
            .AnyAsync(x => x.IsActive);
    }

    private async Task SeedOrganizationDefaultsAsync(int organizationId)
    {
        var templateOrganizationId = await _db.Organizations
            .IgnoreQueryFilters()
            .Where(x => x.Id != organizationId)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (templateOrganizationId <= 0)
        {
            return;
        }

        var departments = await _db.Departments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.OrganizationId == templateOrganizationId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        foreach (var department in departments)
        {
            var exists = await _db.Departments
                .IgnoreQueryFilters()
                .AnyAsync(x => x.OrganizationId == organizationId && x.Name == department.Name);

            if (exists)
            {
                continue;
            }

            _db.Departments.Add(new Department
            {
                OrganizationId = organizationId,
                Name = department.Name,
                EnglishName = department.EnglishName,
                Description = department.Description,
                SortOrder = department.SortOrder,
                IsActive = department.IsActive
            });
        }

        var serviceItems = await _db.ServiceItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.OrganizationId == templateOrganizationId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        foreach (var item in serviceItems)
        {
            var exists = await _db.ServiceItems
                .IgnoreQueryFilters()
                .AnyAsync(x => x.OrganizationId == organizationId && x.SeedKey == item.SeedKey);

            if (exists)
            {
                continue;
            }

            _db.ServiceItems.Add(new ServiceItem
            {
                OrganizationId = organizationId,
                SeedKey = item.SeedKey,
                Category = item.Category,
                Subcategory = item.Subcategory,
                Name = item.Name,
                UnitType = item.UnitType,
                DefaultPrice = item.DefaultPrice,
                PriceNote = item.PriceNote,
                Remark = item.Remark,
                SortOrder = item.SortOrder,
                IsActive = item.IsActive
            });
        }

        await _db.SaveChangesAsync();
    }

    private static OrganizationDto ToDto(Organization organization) => new()
    {
        Id = organization.Id,
        Name = organization.Name,
        IsActive = organization.IsActive,
        CreatedAt = organization.CreatedAt
    };
}
