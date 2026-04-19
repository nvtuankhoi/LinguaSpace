using LinguaSpace.Application.Rooms.Commands.CreateRoom;
using LinguaSpace.Domain.Enums;

namespace LinguaSpace.Application.UnitTests.Rooms;

/// <summary>
/// Tests for CreateRoomCommandValidator.
/// </summary>
public class CreateRoomCommandValidatorTests
{
    private CreateRoomCommandValidator _validator = null!;

    private static CreateRoomCommand ValidCommand() =>
        new(Title: "English Conversation",
            Description: null,
            LanguageCode: "en",
            MaxParticipants: 10,
            RoomType: RoomType.TextOnly);

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateRoomCommandValidator();
    }

    [Test]
    public void ShouldPassWithValidCommand()
    {
        FluentValidation.Results.ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldRequireTitle()
    {
        CreateRoomCommand command = ValidCommand() with { Title = string.Empty };

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Title));
    }

    [Test]
    public void ShouldRejectTitleExceeding100Chars()
    {
        CreateRoomCommand command = ValidCommand() with { Title = new string('a', 101) };

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void ShouldRejectDescriptionExceeding500Chars()
    {
        CreateRoomCommand command = ValidCommand() with { Description = new string('a', 501) };

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void ShouldRequireLanguageCode()
    {
        CreateRoomCommand command = ValidCommand() with { LanguageCode = string.Empty };

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(command.LanguageCode));
    }

    [TestCase("x")]        // too short
    [TestCase("english")]  // too long / not ISO
    [TestCase("EN")]       // uppercase
    public void ShouldRejectInvalidLanguageCode(string languageCode)
    {
        CreateRoomCommand command = ValidCommand() with { LanguageCode = languageCode };

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [TestCase("en")]
    [TestCase("zh")]
    [TestCase("pt-BR")]
    public void ShouldAcceptValidLanguageCodes(string languageCode)
    {
        CreateRoomCommand command = ValidCommand() with { LanguageCode = languageCode };

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [TestCase(1)]   // below min
    [TestCase(51)]  // above max
    public void ShouldRejectInvalidMaxParticipants(int maxParticipants)
    {
        CreateRoomCommand command = ValidCommand() with { MaxParticipants = maxParticipants };

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [TestCase(2)]
    [TestCase(25)]
    [TestCase(50)]
    public void ShouldAcceptValidMaxParticipants(int maxParticipants)
    {
        CreateRoomCommand command = ValidCommand() with { MaxParticipants = maxParticipants };

        FluentValidation.Results.ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
