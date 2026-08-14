namespace EduZim.Domain.Entities;

public class ContentPackRating : TenantEntity
{
    public Guid ContentPackId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Review { get; set; }
}
