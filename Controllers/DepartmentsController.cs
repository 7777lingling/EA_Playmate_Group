using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequirePermission("Organization.Manage")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly DepartmentService _departmentService;

    public DepartmentsController(EAPlaymateGroupDbContext db, DepartmentService departmentService)
    {
        _db = db;
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> GetDepartments([FromQuery] bool activeOnly = true)
    {
        var query = _db.Departments.AsNoTracking()
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var departments = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(departments.Select(DepartmentMapper.ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> GetDepartment(int id)
    {
        var department = await _db.Departments.AsNoTracking()
            .Include(x => x.Members).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        return department is null ? NotFound() : Ok(DepartmentMapper.ToDto(department));
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(CreateDepartmentRequestDto request)
    {
        var result = await _departmentService.CreateAsync(request);
        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(GetDepartments), new { id = result.Value!.Id }, result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateDepartment(int id, UpdateDepartmentRequestDto request)
    {
        var result = await _departmentService.UpdateAsync(id, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id:int}/members")]
    public async Task<ActionResult<DepartmentMemberDto>> AddDepartmentMember(int id, AddDepartmentMemberRequestDto request)
    {
        var result = await _departmentService.AddMemberAsync(id, request);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPut("members/{memberId:int}")]
    public async Task<IActionResult> UpdateDepartmentMember(int memberId, UpdateDepartmentMemberRequestDto request)
    {
        var result = await _departmentService.UpdateMemberAsync(memberId, request);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpGet("members/{memberId:int}")]
    public async Task<ActionResult<DepartmentMemberDto>> GetDepartmentMember(int memberId)
    {
        var member = await _db.DepartmentMembers.AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == memberId);

        if (member is null)
        {
            return NotFound();
        }

        return Ok(new DepartmentMemberDto
        {
            Id = member.Id,
            DepartmentId = member.DepartmentId,
            UserId = member.UserId,
            Nickname = member.User?.Nickname ?? string.Empty,
            PositionTitle = member.PositionTitle,
            IsManager = member.IsManager,
            JoinedAt = member.JoinedAt,
            LeftAt = member.LeftAt
        });
    }

    [HttpPost("members/{memberId:int}/remove")]
    public async Task<IActionResult> RemoveDepartmentMember(int memberId)
    {
        var result = await _departmentService.RemoveMemberAsync(memberId);
        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    [HttpDelete("members/{memberId:int}")]
    public async Task<IActionResult> DeleteDepartmentMember(int memberId)
    {
        var member = await _db.DepartmentMembers.FirstOrDefaultAsync(x => x.Id == memberId);
        if (member is null)
        {
            return NotFound();
        }

        var before = new
        {
            member.Id,
            member.DepartmentId,
            member.UserId,
            member.PositionTitle,
            member.IsManager,
            member.JoinedAt,
            member.LeftAt
        };
        _db.DepartmentMembers.Remove(member);
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create("delete", "department_members", memberId, before: before));
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _db.Departments.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
        if (department is null)
        {
            return NotFound();
        }

        var before = DepartmentMapper.ToDto(department);
        _db.Departments.Remove(department);
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(AuditLogWriter.Create("delete", "departments", id, department.Uuid, before: before));
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

        return ApiErrors.BadRequest(result.ErrorCode ?? "operation_failed", result.ErrorMessage ?? "Operation failed.");
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
