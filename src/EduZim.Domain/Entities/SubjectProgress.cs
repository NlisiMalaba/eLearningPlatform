namespace EduZim.Domain.Entities;

/// <summary>Stored per-subject completion percentage for a student (requirement 4.8).</summary>
public class SubjectProgress : TenantEntity
{
    public Guid StudentId { get; set; }
    public string Subject { get; set; } = default!;
    public int ProgressPercent { get; set; }
}
