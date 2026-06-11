namespace EAPlaymateGroup.Models.DTO;

public sealed class DepartmentDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EnglishName { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<DepartmentMemberDto> Members { get; set; } = [];
}

public sealed class DepartmentMemberDto
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public int UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string? PositionTitle { get; set; }
    public bool IsManager { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}

public sealed class CreateDepartmentRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? EnglishName { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public sealed class UpdateDepartmentRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? EnglishName { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AddDepartmentMemberRequestDto
{
    public int UserId { get; set; }
    public string? PositionTitle { get; set; }
    public bool IsManager { get; set; }
}

public sealed class UpdateDepartmentMemberRequestDto
{
    public string? PositionTitle { get; set; }
    public bool IsManager { get; set; }
    public DateTime? LeftAt { get; set; }
}
