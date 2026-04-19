using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.AddLanguage;

[Authorize]
public record AddLanguageCommand(
    string LanguageCode,
    LanguageType Type,
    LanguageLevel? Level) : IRequest<int>;

public class AddLanguageCommandHandler : IRequestHandler<AddLanguageCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public AddLanguageCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(AddLanguageCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        UserProfile profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        bool alreadyExists = await _context.UserLanguages
            .AnyAsync(l => l.UserProfileId == profile.Id
                        && l.LanguageCode == request.LanguageCode
                        && l.Type == request.Type,
                cancellationToken);

        if (alreadyExists)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.LanguageCode),
                    $"Language '{request.LanguageCode}' with type '{request.Type}' already added.")
            ]);
        }

        UserLanguage language = new()
        {
            UserProfileId = profile.Id,
            LanguageCode = request.LanguageCode,
            Type = request.Type,
            Level = request.Level,
        };

        _context.UserLanguages.Add(language);
        await _context.SaveChangesAsync(cancellationToken);

        return language.Id;
    }
}
