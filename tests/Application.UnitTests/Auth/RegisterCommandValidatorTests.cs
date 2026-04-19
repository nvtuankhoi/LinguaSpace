using LinguaSpace.Application.Auth.Commands.Register;

namespace LinguaSpace.Application.UnitTests.Auth;

/// <summary>
/// Tests for RegisterCommandValidator.
/// Unit tests — no DB, no DI. Direct validator instantiation.
///
/// Pattern:
/// 1. Create validator directly
/// 2. Call validator.TestValidate(command) — FluentValidation test extension
/// 3. Assert ShouldHaveValidationErrorFor / ShouldNotHaveValidationErrorFor
/// </summary>
public class RegisterCommandValidatorTests
{
    private RegisterCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RegisterCommandValidator();
    }

    // ─── Email rules ──────────────────────────────────────────────────────────

    [Test]
    public void ShouldRequireEmail()
    {
        RegisterCommand command = new(Email: string.Empty, Password: "ValidPass1!");

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Email));
    }

    [Test]
    public void ShouldRejectInvalidEmailFormat()
    {
        RegisterCommand command = new(Email: "not-an-email", Password: "ValidPass1!");

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Email));
    }

    [Test]
    public void ShouldRejectEmailExceedingMaxLength()
    {
        string longEmail = new string('a', 250) + "@test.com";

        RegisterCommand command = new(Email: longEmail, Password: "ValidPass1!");

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    // ─── Password rules ───────────────────────────────────────────────────────

    [Test]
    public void ShouldRequirePassword()
    {
        RegisterCommand command = new(Email: "valid@test.com", Password: string.Empty);

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Password));
    }

    [Test]
    public void ShouldRejectPasswordShorterThan8Chars()
    {
        RegisterCommand command = new(Email: "valid@test.com", Password: "Short1");

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Password));
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Test]
    public void ShouldPassWithValidEmailAndPassword()
    {
        RegisterCommand command = new(Email: "valid@test.com", Password: "ValidPass1!");

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
