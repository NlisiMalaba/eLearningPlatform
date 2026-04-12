using EduZim.Application.Assessments.Queries.GetStudentAssessmentResult;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetStudentAssessmentResultQueryValidatorTests
{
    private readonly GetStudentAssessmentResultQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        Guid tenantId = Guid.NewGuid();
        var query = new GetStudentAssessmentResultQuery(tenantId, Guid.NewGuid(), Guid.NewGuid());

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_tenant_fails()
    {
        var query = new GetStudentAssessmentResultQuery(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }
}
