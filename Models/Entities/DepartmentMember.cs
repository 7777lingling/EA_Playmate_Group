namespace EAPlaymateGroup.Models.Entities;

public sealed class DepartmentMember
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public int UserId { get; set; }
    public string? PositionTitle { get; set; }
    public bool IsManager { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Department? Department { get; set; }
    public User? User { get; set; }
}
