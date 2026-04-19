using FluentValidation.Results;

namespace LinguaSpace.Application.Common.Models;

public class Result
{
    internal Result(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
    }

    public bool Succeeded { get; init; }

    public string[] Errors { get; init; }

    public static Result Success()
    {
        return new Result(true, Array.Empty<string>());
    }

    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result(false, errors);
    }

    /// <summary>
    /// Throws <see cref="LinguaSpace.Application.Common.Exceptions.ValidationException"/> if the result failed.
    /// Converts Identity errors (e.g. duplicate email, weak password) into validation failures.
    /// </summary>
    public void ThrowOnFailure()
    {
        if (!Succeeded)
        {
            IEnumerable<ValidationFailure> failures = Errors
                .Select(e => new ValidationFailure(string.Empty, e));

            throw new ValidationException(failures);
        }
    }
}
