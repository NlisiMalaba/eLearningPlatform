using FluentValidation.Results;

namespace EduZim.Application.Exceptions;

public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(BuildMessage(errors))
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this(
            failures
                .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_" : e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
    {
    }

    private static string BuildMessage(IDictionary<string, string[]> errors)
    {
        if (errors.Count == 0)
            return "One or more validation failures have occurred.";
        return string.Join("; ", errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}"));
    }
}
