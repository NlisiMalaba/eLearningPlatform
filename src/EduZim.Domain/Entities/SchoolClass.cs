namespace EduZim.Domain.Entities;

public sealed class SchoolClass : TenantEntity
{
    public string Name { get; set; } = default!;
}
