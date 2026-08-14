using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class ContentPack : TenantEntity
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ContentPackStatus Status { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public string SchoolName { get; set; } = default!;
    public string TeacherName { get; set; } = default!;
    public DateTime? RemovedAtUtc { get; set; }

    public ICollection<ContentPackItem> Items { get; set; } = new List<ContentPackItem>();
    public ICollection<ContentPackAccessRequest> AccessRequests { get; set; } = new List<ContentPackAccessRequest>();
    public ICollection<ContentPackRating> Ratings { get; set; } = new List<ContentPackRating>();
}
