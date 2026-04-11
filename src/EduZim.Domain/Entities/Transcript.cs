namespace EduZim.Domain.Entities;

public class Transcript : TenantEntity
{
    public Guid ContentItemId { get; set; }
    public string StorageKey { get; set; } = default!;
}
