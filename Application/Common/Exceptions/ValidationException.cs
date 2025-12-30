using FluentValidation.Results;

namespace Application.Common.Exceptions;

public class ValidationException : Exception
{
    public short Code { get; private set; }

    public ValidationException()
        : base("Произошла одна или несколько ошибок при проверки")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(short code, string message)
        : base(message)
    {
        Code = code;
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e =>
                {
                    char prop = ' ';

                    if (e.PropertyName.Length > 0)
                    {
                        prop = char.ToLowerInvariant(e.PropertyName.First());
                        return $"{prop}{e.PropertyName[1..]}";
                    }

                    return string.Empty;
                },
                e => e.ErrorMessage
            )
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}