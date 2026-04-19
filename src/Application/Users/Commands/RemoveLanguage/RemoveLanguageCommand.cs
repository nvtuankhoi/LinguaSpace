using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.RemoveLanguage;

[Authorize]
public record RemoveLanguageCommand(int LanguageId) : IRequest;

public class RemoveLanguageCommandHandler : IRequestHandler<RemoveLanguageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public RemoveLanguageCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveLanguageCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        // Load language with its profile to verify ownership
        UserLanguage language = await _context.UserLanguages
            .Include(l => l.UserProfile)
            .FirstOrDefaultAsync(l => l.Id == request.LanguageId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserLanguage), request.LanguageId.ToString());

        if (language.UserProfile.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        _context.UserLanguages.Remove(language);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
