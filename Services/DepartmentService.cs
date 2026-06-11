using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class DepartmentService
{
    private readonly EAPlaymateGroupDbContext _db;

    public DepartmentService(EAPlaymateGroupDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<DepartmentDto>> CreateAsync(CreateDepartmentRequestDto request)
    {
        var validation = ValidateDepartment(request.Name);
        if (!validation.Succeeded)
        {
            return ToGenericResult<DepartmentDto>(validation);
        }

        var department = new Department
        {
            Name = request.Name.Trim(),
            EnglishName = Clean(request.EnglishName),
            Description = Clean(request.Description),
            SortOrder = request.SortOrder
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync();

        var dto = DepartmentMapper.ToDto(department);
        _db.AuditLogs.Add(AuditLogWriter.Create("create", "departments", department.Id, department.Uuid, after: dto));
        await _db.SaveChangesAsync();
        return ServiceResult<DepartmentDto>.Success(dto);
    }

    public async Task<ServiceResult> UpdateAsync(int id, UpdateDepartmentRequestDto request)
    {
        var department = await _db.Departments
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (department is null)
        {
            return ServiceResult.Missing();
        }

        var validation = ValidateDepartment(request.Name);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var before = DepartmentMapper.ToDto(department);
        department.Name = request.Name.Trim();
        department.EnglishName = Clean(request.EnglishName);
        department.Description = Clean(request.Description);
        department.SortOrder = request.SortOrder;
        department.IsActive = request.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create("update", "departments", department.Id, department.Uuid, before, DepartmentMapper.ToDto(department)));
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<DepartmentMemberDto>> AddMemberAsync(int departmentId, AddDepartmentMemberRequestDto request)
    {
        var departmentExists = await _db.Departments.AnyAsync(x => x.Id == departmentId && x.IsActive);
        if (!departmentExists)
        {
            return ServiceResult<DepartmentMemberDto>.Missing();
        }

        var userExists = await _db.Users.AnyAsync(x => x.Id == request.UserId && x.IsActive);
        if (!userExists)
        {
            return ServiceResult<DepartmentMemberDto>.Validation(new Dictionary<string, string[]> { ["userId"] = ["請選擇有效成員。"] });
        }

        var existing = await _db.DepartmentMembers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == request.UserId && x.LeftAt == null);
        if (existing is not null)
        {
            existing.PositionTitle = Clean(request.PositionTitle);
            existing.IsManager = request.IsManager;
            await _db.SaveChangesAsync();
            return ServiceResult<DepartmentMemberDto>.Success(DepartmentMapper.ToMemberDto(existing));
        }

        var member = new DepartmentMember
        {
            DepartmentId = departmentId,
            UserId = request.UserId,
            PositionTitle = Clean(request.PositionTitle),
            IsManager = request.IsManager
        };

        _db.DepartmentMembers.Add(member);
        await _db.SaveChangesAsync();

        var saved = await _db.DepartmentMembers.Include(x => x.User).FirstAsync(x => x.Id == member.Id);
        _db.AuditLogs.Add(AuditLogWriter.Create("create", "department_members", saved.Id, after: DepartmentMapper.ToMemberDto(saved)));
        await _db.SaveChangesAsync();
        return ServiceResult<DepartmentMemberDto>.Success(DepartmentMapper.ToMemberDto(saved));
    }

    public async Task<ServiceResult> UpdateMemberAsync(int memberId, UpdateDepartmentMemberRequestDto request)
    {
        var member = await _db.DepartmentMembers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == memberId);
        if (member is null)
        {
            return ServiceResult.Missing();
        }

        var before = DepartmentMapper.ToMemberDto(member);
        member.PositionTitle = Clean(request.PositionTitle);
        member.IsManager = request.IsManager;
        member.LeftAt = request.LeftAt;
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create("update", "department_members", member.Id, before: before, after: DepartmentMapper.ToMemberDto(member)));
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveMemberAsync(int memberId)
    {
        var member = await _db.DepartmentMembers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == memberId);
        if (member is null)
        {
            return ServiceResult.Missing();
        }

        var before = DepartmentMapper.ToMemberDto(member);
        member.LeftAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create("leave", "department_members", member.Id, before: before, after: DepartmentMapper.ToMemberDto(member)));
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    private static ServiceResult ValidateDepartment(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? ServiceResult.Validation(new Dictionary<string, string[]> { ["name"] = ["請輸入部門名稱。"] })
            : ServiceResult.Success();
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ServiceResult<T> ToGenericResult<T>(ServiceResult result)
    {
        return new ServiceResult<T>
        {
            NotFound = result.NotFound,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            ValidationErrors = result.ValidationErrors
        };
    }
}
