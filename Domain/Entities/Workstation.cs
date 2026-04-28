namespace CarAssemblyErp.Domain.Entities;

public class Workstation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
}
