using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;

namespace EAPlaymateGroup.Services;

public static class DepartmentMapper
{
    public static DepartmentDto ToDto(Department department)
    {
        return new DepartmentDto
        {
            Id = department.Id,
            Uuid = department.Uuid,
            Name = department.Name,
            EnglishName = department.EnglishName,
            Description = department.Description,
            SortOrder = department.SortOrder,
            IsActive = department.IsActive,
            Members = department.Members
                .Where(x => x.LeftAt == null)
                .OrderByDescending(x => x.IsManager)
                .ThenBy(x => x.User!.Nickname)
                .Select(ToMemberDto)
                .ToList()
        };
    }

    public static DepartmentMemberDto ToMemberDto(DepartmentMember member)
    {
        return new DepartmentMemberDto
        {
            Id = member.Id,
            DepartmentId = member.DepartmentId,
            UserId = member.UserId,
            Nickname = member.User?.Nickname ?? string.Empty,
            PositionTitle = member.PositionTitle,
            IsManager = member.IsManager,
            JoinedAt = member.JoinedAt,
            LeftAt = member.LeftAt
        };
    }
}
