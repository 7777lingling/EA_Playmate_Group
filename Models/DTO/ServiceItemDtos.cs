namespace EAPlaymateGroup.Models.DTO;

public sealed class ServiceItemDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Subcategory { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UnitType { get; set; } = "custom";
    public decimal? DefaultPrice { get; set; }
    public string? PriceNote { get; set; }
    public string? Remark { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
