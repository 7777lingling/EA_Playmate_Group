namespace EAPlaymateGroup.Models.DTO;

public sealed class OrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SaveOrganizationRequestDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
