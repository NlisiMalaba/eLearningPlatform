using EduZim.Application.Assessments.Queries.ListAssessments;
using EduZim.Application.Assessments.Queries.ListSchoolClasses;
using EduZim.Application.Content.Commands.PublishContent;
using EduZim.Application.Content.Commands.SetModuleContentItems;
using EduZim.Application.Content.Queries.ListContent;
using EduZim.Application.Content.Queries.ListModules;

namespace EduZim.Tests.Unit.Validators;

public sealed class TeacherContentCommandValidatorTests
{
    [Fact]
    public async Task List_content_requires_tenant()
    {
        ListContentQueryValidator validator = new();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new ListContentQuery(Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task List_modules_requires_tenant()
    {
        ListModulesQueryValidator validator = new();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new ListModulesQuery(Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Publish_requires_content_id()
    {
        PublishContentCommandValidator validator = new();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new PublishContentCommand(Guid.NewGuid(), Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Set_module_items_rejects_duplicates()
    {
        SetModuleContentItemsCommandValidator validator = new();
        Guid id = Guid.NewGuid();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new SetModuleContentItemsCommand(Guid.NewGuid(), Guid.NewGuid(), [id, id]));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task List_assessments_requires_tenant()
    {
        ListAssessmentsQueryValidator validator = new();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new ListAssessmentsQuery(Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task List_school_classes_requires_tenant()
    {
        ListSchoolClassesQueryValidator validator = new();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new ListSchoolClassesQuery(Guid.Empty));
        Assert.False(result.IsValid);
    }
}
