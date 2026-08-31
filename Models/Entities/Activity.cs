namespace EAPlaymateGroup.Models.Entities;

public sealed class Activity : IOrganizationScoped
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string DiscountType { get; set; } = "percent";
    public decimal DiscountValue { get; set; }
    public string ApplicableCategories { get; set; } = string.Empty;
    public bool IncludeFees { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
