namespace LinguaSpace.Application.Users.Commands.AddLanguage;

public class AddLanguageCommandValidator : AbstractValidator<AddLanguageCommand>
{
    public AddLanguageCommandValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Length(2, 10)
            .Matches(@"^[a-z]{2,3}(-[A-Z]{2,4})?$")
            .WithMessage("LanguageCode must be a valid ISO 639-1 code (e.g., 'en', 'zh-TW').");

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Level)
            .IsInEnum()
            .When(x => x.Level.HasValue);
    }
}
