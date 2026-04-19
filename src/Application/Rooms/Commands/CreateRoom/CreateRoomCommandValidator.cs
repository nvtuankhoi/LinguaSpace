namespace LinguaSpace.Application.Rooms.Commands.CreateRoom;

public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Length(2, 10)
            .Matches(@"^[a-z]{2,3}(-[A-Z]{2,4})?$")
            .WithMessage("LanguageCode must be a valid ISO 639-1 code.");

        RuleFor(x => x.MaxParticipants)
            .InclusiveBetween(2, 50);

        RuleFor(x => x.RoomType)
            .IsInEnum();
    }
}
