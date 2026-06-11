namespace EAPlaymateGroup.Models.Entities;

public sealed class Department
{
    public int Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? EnglishName { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DepartmentMember> Members { get; set; } = [];
}
